using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Uno.Extensions;

namespace MusicPlayer.Shared.Helpers
{
    public static class Settings
    {
        public enum StorageType
        {
            IndexedDb,
            WebSql,
            LocalStorage,
            Cookies,
            JsonStorage
        }

        public static Dictionary<string, string> GetOptions(string optionsString)
        {
            if (string.IsNullOrEmpty(optionsString)) return new Dictionary<string, string>();

            var data = optionsString.Split(new[] {";"}, StringSplitOptions.RemoveEmptyEntries);

            var options = new Dictionary<string, string>();

            foreach (var option in data)
            {
                var index = option.IndexOf("=", StringComparison.Ordinal);

                if (index < 0) continue;

                var optionKey = option.Substring(0, index);
                var optionValue = option.Substring(index + 1);

                options.Add(optionKey, optionValue);
            }

            return options;
        }

        public static string GetOptionsString(Dictionary<string, string> options)
        {
            return options == null
                ? ""
                : options.Aggregate("", (current, option) => current + option.Key + "=" + option.Value + ";");
        }

        public class Storage
        {
            public Storage(StorageType type)
            {
                Type = type;
            }

            public StorageType Type { get; }

            public async Task<string[]> GetKeysAsync()
            {
                var keys = await JavaScript.RunScriptAsync("getKeys",
                    new[] {("$StorageOptions", Type.ToString().ToLower())});

                return keys.Equals("null") ? null : keys.Split(',');
            }

            public async Task<int> GetLengthAsync()
            {
                var length = await JavaScript.RunScriptAsync("getLength",
                    new[] {("$StorageOptions", Type.ToString().ToLower())});

                return int.Parse(length);
            }

            public async Task<Dictionary<string, string>> GetDataAsync()
            {
                var keys = await GetKeysAsync();

                var valueTasks = keys.Select(GetAsync);

                var values = await Task.WhenAll(valueTasks);

                return keys.ToList().ToDictionary(key => key, key => values.ToList()[Array.IndexOf(keys, key)]);
            }

            public async Task<string> GetAsync(string key)
            {
                var data = await JavaScript.RunScriptAsync("get", new[]
                {
                    ("$Key", key),
                    ("$StorageOptions", Type.ToString().ToLower())
                });

                return data.Equals("null") ? null : data;
            }

            public void Set(string key, string value)
            {
                JavaScript.RunScript("set",
                    new[]
                    {
                        ("$Key", key),
                        ("$Value", "\"" + value + "\""),
                        ("$StorageOptions", Type.ToString().ToLower())
                    });
            }

            public void Delete(string key)
            {
                JavaScript.RunScript("delete",
                    new[]
                    {
                        ("$Key", key),
                        ("$StorageOptions", Type.ToString().ToLower())
                    });
            }

            public void Clear()
            {
                JavaScript.RunScript("clear",
                    new[]
                    {
                        ("$StorageOptions", Type.ToString().ToLower())
                    });
            }
        }
    }
}