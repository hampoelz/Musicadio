using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using MusicPlayer.Shared.Helpers;
using Uno.Extensions;

namespace MusicPlayer.Shared.Engine
{
    public class Song
    {
        public string Title { get; set; }

        public string Artist { get; set; }
        public string Album { get; set; }
        public int Year { get; set; }

        public double Duration { get; set; }

        public Guid DataId { get; set; }

        public Uri Path { get; set; }

        public AudioEngine.SongProvider Provider { get; set; } = AudioEngine.SongProvider.Unknown;
    }

    public class AudioEngine
    {
        public enum SongProvider
        {
            Unknown,
            File,
            LiveStream
        }

        private const string AudioControl = "window.AudioEngine.audio";

        public static readonly Settings.Storage DataStorage = new Settings.Storage(Settings.StorageType.IndexedDb);
        public static readonly Settings.Storage InfoStorage = new Settings.Storage(Settings.StorageType.LocalStorage);

        private static readonly Canvas SpectrumAnalyzer = (Canvas) Application.Current.Resources["SpectrumAnalyzer"];
        private static readonly Image CoverElement = (Image) Application.Current.Resources["Cover"];

        public static readonly string[] SongExt = {".flac", ".m4a", ".mp3", ".ogg", ".opus", ".webm", ".wav"};

        // public static readonly string[] PlaylistExt = {".m3u", ".vlc", ".m3u8", ".xspf", ".b4s", ".jspf"};
        // public static readonly string[] UniversalExt = {".music", ".radio"};

        static AudioEngine()
        {
            var stream = (Canvas) Application.Current.Resources["Stream"];
            SpectrumAnalyzer.Children.Add(stream);

            var engine = SpectrumAnalyzer;

            engine.RegisterHtmlCustomEventHandler("onLoaded", OnLoaded);

            engine.RegisterHtmlCustomEventHandler("onPlay", OnPlay);
            engine.RegisterHtmlCustomEventHandler("onPause", OnPause);
            engine.RegisterHtmlCustomEventHandler("onEnded", OnEnded);
            engine.RegisterHtmlCustomEventHandler("onDownload", OnDownload);
            engine.RegisterHtmlCustomEventHandler("onTimeUpdate",
                delegate { RaiseStaticPropertyChanged("Position"); });
            engine.RegisterHtmlCustomEventHandler("onDurationChange",
                delegate { RaiseStaticPropertyChanged("Duration"); });
            engine.RegisterHtmlCustomEventHandler("onVolumeChange",
                delegate { RaiseStaticPropertyChanged("Volume"); });
            engine.RegisterHtmlCustomEventHandler("onColorChange",
                delegate { RaiseStaticPropertyChanged("Color"); });
            engine.RegisterHtmlCustomEventHandler("onCoverChange",
                delegate
                {
                    RaiseStaticPropertyChanged("Cover");
                    SendCover();
                });
            engine.RegisterHtmlCustomEventHandler("onFailedPlay", OnFailedPlay);

            engine.RegisterHtmlCustomEventHandler("handleFiles", HandleAddEvents.OnHandleFiles);

            var fileExtArray = SongExt /* .Concat(PlaylistExt).Concat(UniversalExt).ToArray() */;

            JavaScript.RunScript("InitializeAudioEngine", new[]
            {
                ("$AudioEngine", engine.GetHtmlId()),
                ("$ImageControl", CoverElement.GetHtmlId()),
                ("$FileExtension", string.Join(",", fileExtArray)),
                ("$StorageOptions", DataStorage.Type.ToString().ToLower()),
                ("$CurrentPlaybackGuid", Playback.DefaultUuid.ToString()),
                ("$UsePwaWrapper", App.UsePwaWrapper.ToString().ToLower())
            });
            JavaScript.RunScript("InitializeSpectrumAnalyzer");
            JavaScript.Invoke("window.AudioEngine.setCover();");

            JavaScript.Invoke($"document.getElementById('{engine.GetHtmlId()}').dispatchEvent(new Event('onLoaded'))");
        }

        #region Events

        private static async void OnLoaded(object sender, EventArgs e)
        {
            if (await Playlist.IsShuffleAsync()) RaiseStaticPropertyChanged("Shuffle");

            var playbackIndex = await Playback.GetIndexAsync();
            if (playbackIndex != -1) await Playlist.StartAsyncAt(playbackIndex);
            else await Playback.SetAsync(null);

            if (App.UsePwaWrapper) JavaScript.Invoke("window.ipcRenderer.send('engineLoaded');");
        }

        private static void OnPlay(object sender, EventArgs e)
        {
            IsEnded = false;
            IsPlaying = true;

            if (string.IsNullOrEmpty(JavaScript.Invoke($"{AudioControl}.src")))
            {
                Console.WriteLine();
                Console.WriteLine("Playing Stream");
                Console.WriteLine();
            }
        }

        private static void OnPause(object sender, EventArgs e)
        {
            IsPlaying = false;
        }

        private static async void OnEnded(object sender, EventArgs e)
        {
            IsEnded = true;
            IsPlaying = false;

            Playback.CurrentTime = 0;

            var playlistLength = await Playlist.GetLengthAsync();
            var playbackIndex = await Playback.GetIndexAsync();

            if (playbackIndex == -1 && Loop) Playback.PausePlay();
            else if (playbackIndex != playlistLength - 1 || Loop) await Playlist.SkipNextAsync();
        }

        private static void OnDownload(object sender, EventArgs e)
        {
            //TODO: [MSG] Downloading audio ...
        }

        private static class HandleAddEvents
        {
            public static int TmpIsShuffle = -1;
            public static int TmpPlaybackIndex = -1;

            private static int _tmpSongCounter;
            private static int _tmpNewPlaylistLength = -1;
            private static int _tmpOldPlaylistLength = -1;

            private static readonly Stopwatch StopWatch = new Stopwatch();

            public static async void OnHandleFiles(object sender, HtmlCustomEventArgs e)
            {
                //TODO: [MSG] Adding Songs ...

                var data = e.Detail;

                if (data.StartsWith("Initialize"))
                {
                    StopWatch.Start();
                    Console.WriteLine("Start loading songs ....");

                    // 1 2 3 4 5 6 7 8 9 10 11
                    // | | | | | | | | | |  |
                    // I n i t i a l i z e  :  traffic=##;....
                    var options = Settings.GetOptions(data.Remove(0, 11));

                    var songCounter = 0;
                    if (options.ContainsKey("traffic")) int.TryParse(options["traffic"], out songCounter);

                    _tmpSongCounter = songCounter;

                    var playlistLength = await Playlist.GetLengthAsync();

                    TmpIsShuffle = await Playlist.IsShuffleAsync() ? 1 : 0;
                    TmpPlaybackIndex = await Playback.GetIndexAsync();
                    _tmpOldPlaylistLength = playlistLength;
                    _tmpNewPlaylistLength = playlistLength;
                }
                else
                {
                    var timeout = new Stopwatch();
                    timeout.Start();

                    while (_tmpNewPlaylistLength == -1)
                    {
                        await Task.Delay(10);

                        if (timeout.ElapsedMilliseconds > 4000) return;
                    }

                    var songOptions = Settings.GetOptions(data);

                    if (!songOptions.ContainsKey("duration") || !songOptions.ContainsKey("provider") ||
                        !Guid.TryParse(songOptions["uuid"], out var uuid)) return;

                    if (uuid == Playback.DefaultUuid)
                    {
                        await Playback.StartAsync(Playback.Extract(data));
                    }
                    else
                    {
                        var playlistLength = _tmpNewPlaylistLength;
                        ++_tmpNewPlaylistLength;

                        if (TmpIsShuffle == 1)
                        {
                            var shuffleInfo = await InfoStorage.GetAsync("shuffle");
                            var shuffleSortInfo = shuffleInfo.Split(';').Select(int.Parse).ToList();

                            if (!shuffleSortInfo.Contains(playlistLength))
                            {
                                shuffleSortInfo.Add(playlistLength);

                                var shuffleOrder = await ShufflePlaylistAsync(shuffleSortInfo, TmpPlaybackIndex + 2);
                                InfoStorage.Set("shuffle", string.Join(";", shuffleOrder));
                            }
                        }

                        InfoStorage.Set(JavaScript.Base64Encode(playlistLength.ToString()), data);

                        if (!IsPlaying && (TmpPlaybackIndex == -1 ||
                                           _tmpNewPlaylistLength == _tmpOldPlaylistLength + 1 &&
                                           TmpPlaybackIndex == playlistLength - 1))
                        {
                            ++TmpPlaybackIndex;
                            await Playlist.StartAsyncAt(playlistLength);
                        }
                    }

                    if (_tmpOldPlaylistLength + _tmpSongCounter != _tmpNewPlaylistLength) return;

                    Console.WriteLine("All songs loaded in " + StopWatch.ElapsedMilliseconds + "ms");
                    StopWatch.Reset();

                    TmpIsShuffle = -1;
                    TmpPlaybackIndex = -1;
                    _tmpSongCounter = 0;
                    _tmpNewPlaylistLength = -1;
                    _tmpOldPlaylistLength = -1;
                }
            }
        }

        private static void OnFailedPlay(object sender, HtmlCustomEventArgs e)
        {
            switch (e.Detail)
            {
                case "notFound":
                    //TODO: [MSG] Song not found / can't play song
                    break;
                case "abort":
                    //TODO: [MSG] Audio load aborted
                    break;
                case "stalled":
                    //TODO: [MSG] Media data is not available
                    break;
                case "suspend":
                    //TODO: [MSG] Loading of the media is suspended
                    break;
                case "waiting":
                    //TODO: [MSG] Wait! The next frame must be buffered
                    break;
                case "error":
                    //TODO: [MSG] Error! Something went wrong
                    break;
            }
        }

        #endregion

        #region Functions

        public static void SendCover()
        {
            if (!Stream.IsStreamCreator) return;

            var packages = Playback.Cover.SplitInParts(65536).ToList();

            Stream.SendInformation(new Dictionary<string, string> {{"coverPackages", packages.Count.ToString()}});

            for (var i = 0; i < packages.Count; i++)
            {
                Console.WriteLine($"    Send Package {i + 1}/{packages.Count}");
                Stream.SendInformation(new Dictionary<string, string>
                    {{"coverPackage", i.ToString()}, {"cover", packages[i]}});
            }
        }

        public static void OpenFileDialog(bool addToPlaylist)
        {
            JavaScript.RunScript("FileSelector", new[] {("$MultiSelect", addToPlaylist.ToString().ToLower())});
        }

        #region PlaylistFunctions

        private static async Task<List<int>> ShufflePlaylistAsync(List<int> playlistOrder, int startIndex = 0)
        {
            if (startIndex < 0 || startIndex > playlistOrder.Count - 1) startIndex = 0;

            if (playlistOrder.Count - 1 <= startIndex) return playlistOrder;

            var ignore = new List<int>();

            for (var index = 0; index < startIndex; index++)
            {
                ignore.Add(playlistOrder[0]);
                playlistOrder.RemoveAt(0);
            }

            var rd = new Random();
            var shuffle = playlistOrder.ToArray();
            var n = shuffle.Length;

            while (n > 1)
            {
                n--;

                var k = rd.Next(n + 1);
                var v = shuffle[k];

                shuffle[k] = shuffle[n];
                shuffle[n] = v;
            }

            var output = ignore;
            output.AddRange(shuffle);

            var playbackIndex = await Playback.GetIndexAsync();

            if (playbackIndex == -1 || startIndex != 0) return output;

            output.Remove(playbackIndex);
            output.Insert(0, playbackIndex);

            return output;
        }

        #endregion

        #endregion

        #region INotifyPropertyChanged

        public static event EventHandler<PropertyChangedEventArgs> StaticPropertyChanged;

        public static void RaiseStaticPropertyChanged([CallerMemberName] string name = null)
        {
            StaticPropertyChanged?.Invoke(null, new PropertyChangedEventArgs(name));
        }

        #endregion

        #region Properties

        private static bool _isPlaying;
        private static bool _isEnded;
        private static bool _loop;

        public static bool IsPlaying
        {
            get => _isPlaying;
            private set
            {
                if (_isPlaying == value) return;

                _isPlaying = value;
                RaiseStaticPropertyChanged();
            }
        }

        public static bool IsEnded
        {
            get => _isEnded;
            private set
            {
                if (_isEnded == value) return;

                _isEnded = value;
                RaiseStaticPropertyChanged();
            }
        }

        public static bool Loop
        {
            get => _loop;
            set
            {
                if (_loop == value) return;

                _loop = value;
                RaiseStaticPropertyChanged();
            }
        }

        public static bool Mute
        {
            get => bool.Parse(JavaScript.Invoke($"{AudioControl}.muted;"));
            set
            {
                JavaScript.Invoke($"{AudioControl}.muted = {value.ToString().ToLower()};");
                RaiseStaticPropertyChanged();
            }
        }

        public static double Volume
        {
            get => double.Parse(JavaScript.Invoke($"{AudioControl}.volume;"), CultureInfo.InvariantCulture) * 100;
            set =>
                JavaScript.Invoke($"{AudioControl}.volume = {(value / 100).ToString(CultureInfo.InvariantCulture)};");
        }

        public static SolidColorBrush AccentColor
        {
            get
            {
                var r = int.Parse(JavaScript.Invoke("window.AudioEngine.color.r;"));
                var g = int.Parse(JavaScript.Invoke("window.AudioEngine.color.g;"));
                var b = int.Parse(JavaScript.Invoke("window.AudioEngine.color.b;"));

                return new SolidColorBrush(Color.FromArgb(255, (byte) r, (byte) g, (byte) b));
            }
            set
            {
                var color = value.Color;

                var r = color.R.ToString();
                var g = color.G.ToString();
                var b = color.B.ToString();

                JavaScript.Invoke($"window.AudioEngine.changeColor({{ r: {r}, g: {g}, b: {b} }});");
            }
        }

        public static class Playlist
        {
            public static async Task<List<Song>> GetAsync()
            {
                var infos = await InfoStorage.GetDataAsync();

                var sortInfo = new ConcurrentBag<(int index, Song song)>();

                var isShuffle = infos.ContainsKey("shuffle");
                var shuffleSortInfo = isShuffle ? infos["shuffle"].Split(';') : new string[0];

                Parallel.ForEach(infos, info =>
                {
                    var encodedIndex = info.Key;

                    if (!int.TryParse(JavaScript.Base64Decode(encodedIndex), out var songIndex)) return;
                    if (isShuffle) songIndex = Array.IndexOf(shuffleSortInfo, songIndex.ToString());
                    if (songIndex == -1) return;

                    sortInfo.Add((songIndex, Playback.Extract(info.Value)));
                });

                var songs = new List<Song>();

                foreach (var (_, song) in sortInfo.OrderBy(info => info.index)) songs.Add(song);

                return songs;
            }

            public static async Task<int> GetLengthAsync()
            {
                var length = await InfoStorage.GetLengthAsync();

                if (!string.IsNullOrEmpty(await InfoStorage.GetAsync("playback"))) --length;
                if (await IsShuffleAsync()) --length;

                return length;
            }

            public static async Task<Song> GetPlaybackAsyncAt(int index)
            {
                return await GetPlaybackAsyncAt(JavaScript.Base64Encode(index.ToString()));
            }

            public static async Task<Song> GetPlaybackAsyncAt(string encodedIndex)
            {
                if (!int.TryParse(JavaScript.Base64Decode(encodedIndex), out var songIndex)) return null;
                songIndex = await GetShuffledIndexAsync(songIndex);

                var songInfo = await InfoStorage.GetAsync(JavaScript.Base64Encode(songIndex.ToString()));

                return Playback.Extract(songInfo);
            }

            public static async Task StartAsyncAt(int index)
            {
                Console.WriteLine("start: " + index);

                var playback = await GetPlaybackAsyncAt(index);

                if (playback == null)
                    //TODO: [MSG] Song is null
                    return;

                JavaScript.RunScript("StartPlayback",
                    new[]
                    {
                        ("$StorageOptions", DataStorage.Type.ToString().ToLower()),
                        ("$DataId", playback.DataId.ToString())
                    });

                var data = new Dictionary<string, string> {{"index", index.ToString()}};

                await Playback.SetAsync(data);

                if (HandleAddEvents.TmpPlaybackIndex != -1 && HandleAddEvents.TmpPlaybackIndex != index)
                    HandleAddEvents.TmpPlaybackIndex = index;
            }

            public static async Task SkipPreviousAsync()
            {
                var playbackIndex = await Playback.GetIndexAsync();

                if (playbackIndex == -1)
                {
                    Playback.CurrentTime = 0;
                }
                else
                {
                    var playlistLength = await GetLengthAsync();

                    if (playbackIndex == 0) await StartAsyncAt(playlistLength - 1);
                    else await StartAsyncAt(playbackIndex - 1);
                }
            }

            public static async Task SkipNextAsync()
            {
                var playlistLength = await GetLengthAsync();

                if (playlistLength == 0) return;

                var playbackIndex = await Playback.GetIndexAsync();

                if (playbackIndex == -1 || playbackIndex == playlistLength - 1)
                    await StartAsyncAt(0);
                else
                    await StartAsyncAt(playbackIndex + 1);
            }

            public static async Task<bool> IsShuffleAsync()
            {
                var shuffleInfo = await InfoStorage.GetAsync("shuffle");
                return !string.IsNullOrEmpty(shuffleInfo);
            }

            public static async Task<int> GetShuffledIndexAsync(int index)
            {
                var shuffleInfo = await InfoStorage.GetAsync("shuffle");
                var isShuffle = !string.IsNullOrEmpty(shuffleInfo);

                if (!isShuffle) return index;

                var shuffleSortInfo = shuffleInfo.Split(';');

                return !int.TryParse(shuffleSortInfo[index], out var songIndex) ? index : songIndex;
            }

            public static async Task ToggleShuffleAsync(bool? shuffle = null)
            {
                if (shuffle == null) shuffle = !await IsShuffleAsync();

                if ((bool) shuffle)
                {
                    InfoStorage.Set("shuffle", "0");

                    var playlistLength = await GetLengthAsync();

                    var playlistOrder = new List<int>();

                    for (var index = 0; index < playlistLength; index++) playlistOrder.Add(index);

                    var shuffleOrder = await ShufflePlaylistAsync(playlistOrder);

                    InfoStorage.Set("shuffle", string.Join(";", shuffleOrder));

                    InfoStorage.Set("playback", "index=0;");
                }
                else if (!(bool) shuffle)
                {
                    var shuffleInfo = await InfoStorage.GetAsync("shuffle");

                    if (!string.IsNullOrEmpty(shuffleInfo))
                    {
                        var playbackIndex = await Playback.GetIndexAsync();

                        InfoStorage.Delete("shuffle");

                        var shuffleSortInfo = shuffleInfo.Split(';');
                        var songIndex = int.Parse(shuffleSortInfo[playbackIndex]);

                        InfoStorage.Set("playback", $"index={songIndex};");
                    }
                }

                if (HandleAddEvents.TmpIsShuffle != -1) HandleAddEvents.TmpIsShuffle = (bool) shuffle ? 1 : 0;

                RaiseStaticPropertyChanged("Shuffle");
            }

            public static async Task ClearAsync()
            {
                var playbackIndex = await Playback.GetIndexAsync();

                InfoStorage.Clear();
                DataStorage.Clear();

                if (playbackIndex != -1) await Playback.StopAsync();

                RaiseStaticPropertyChanged("Shuffle");
            }
        }

        public static class Playback
        {
            public static readonly Guid DefaultUuid; // = 00000000-0000-0000-0000-000000000000

            public static double Duration =>
                double.Parse(JavaScript.Invoke($"{AudioControl}.duration;"), CultureInfo.InvariantCulture);

            public static double CurrentTime
            {
                get => double.Parse(JavaScript.Invoke($"{AudioControl}.currentTime;"), CultureInfo.InvariantCulture);
                set => JavaScript.Invoke(
                    $"{AudioControl}.currentTime = {value.ToString(CultureInfo.InvariantCulture)};");
            }

            public static string Cover
            {
                get => JavaScript.Invoke("window.AudioEngine.cover;");
                set
                {
                    if (value != null && !string.IsNullOrEmpty(value)) value = $"'{value}'";
                    JavaScript.Invoke($"window.AudioEngine.setCover({value});");
                }
            }

            public static Song Extract(string songInfo)
            {
                var songOptions = Settings.GetOptions(songInfo);

                if (!songOptions.ContainsKey("duration") || !songOptions.ContainsKey("provider")) return null;

                var file = songOptions.ContainsKey("file")
                    ? Uri.UnescapeDataString(JavaScript.Base64Decode(songOptions["file"]))
                    : "Unknown File";

                var title = songOptions.ContainsKey("title")
                    ? Uri.UnescapeDataString(JavaScript.Base64Decode(songOptions["title"]))
                    : null;
                var path = songOptions.ContainsKey("path")
                    ? Uri.UnescapeDataString(JavaScript.Base64Decode(songOptions["path"]))
                    : null;
                var artist = songOptions.ContainsKey("artist")
                    ? Uri.UnescapeDataString(JavaScript.Base64Decode(songOptions["artist"]))
                    : null;
                var album = songOptions.ContainsKey("album")
                    ? Uri.UnescapeDataString(JavaScript.Base64Decode(songOptions["album"]))
                    : null;

                int.TryParse(songOptions.ContainsKey("year") ? songOptions["year"] : null, out var year);
                var duration = double.Parse(songOptions["duration"], CultureInfo.InvariantCulture);

                Uri.TryCreate(path, UriKind.Absolute, out var songUri);
                Enum.TryParse(songOptions["provider"], true, out SongProvider songProvider);

                if (songProvider != SongProvider.LiveStream && !songOptions.ContainsKey("uuid")) return null;

                Guid.TryParse(songOptions.ContainsKey("uuid") ? songOptions["uuid"] : "", out var uuid);

                return new Song
                {
                    Title = title ?? file,
                    Path = songUri,
                    Artist = artist,
                    Album = album,
                    Year = year,
                    Duration = duration,
                    DataId = uuid,
                    Provider = songProvider
                };
            }

            public static Dictionary<string, string> ToDictionary(Song playback)
            {
                if (playback == null) return new Dictionary<string, string>();

                var data = new Dictionary<string, string>
                {
                    {"uuid", playback.DataId.ToString()},
                    {"title", JavaScript.Base64Encode(Uri.EscapeDataString(playback.Title))},
                    {"duration", playback.Duration.ToString(CultureInfo.InvariantCulture)},
                    {"provider", playback.Provider.ToString()}
                };

                if (playback.Artist != null)
                    data.Add("artist", JavaScript.Base64Encode(Uri.EscapeDataString(playback.Artist)));
                if (playback.Album != null)
                    data.Add("album", JavaScript.Base64Encode(Uri.EscapeDataString(playback.Album)));
                if (playback.Year.ToString().Length == 4)
                    data.Add("year", JavaScript.Base64Encode(Uri.EscapeDataString(playback.Year.ToString())));
                if (playback.Path != null)
                    data.Add("path", JavaScript.Base64Encode(Uri.EscapeDataString(playback.Path.ToString())));

                return data;
            }

            public static async Task<Song> GetAsync()
            {
                var songInfo = await InfoStorage.GetAsync("playback");

                var options = Settings.GetOptions(songInfo);

                if (!options.ContainsKey("index")) return Extract(songInfo);
                if (!int.TryParse(options["index"], out var songIndex)) return null;
                songIndex = await Playlist.GetShuffledIndexAsync(songIndex);

                songInfo = await InfoStorage.GetAsync(JavaScript.Base64Encode(songIndex.ToString()));

                return Extract(songInfo);
            }

            public static async Task<int> GetIndexAsync()
            {
                var songInfo = await InfoStorage.GetAsync("playback");

                var options = Settings.GetOptions(songInfo);

                if (!options.ContainsKey("index")) return -1;

                return int.TryParse(options["index"], out var songIndex) ? songIndex : -1;
            }

            public static async Task SetAsync(Dictionary<string, string> data)
            {
                var songInfo = Settings.GetOptionsString(data);

                var reset = string.IsNullOrEmpty(songInfo) || data == null;

                if (reset)
                {
                    InfoStorage.Delete("playback");
                    Cover = null;
                }
                else
                {
                    InfoStorage.Set("playback", songInfo);
                }

                RaiseStaticPropertyChanged("Playback");

                await SendInfoAsync(data);
            }

            public static async Task SendInfoAsync(Dictionary<string, string> data = null)
            {
                if (!Stream.IsStreamCreator) return;

                if (data == null) data = ToDictionary(await GetAsync());

                if (data.Count == 0)
                {
                    data.Add("title", JavaScript.Base64Encode(Uri.EscapeDataString("The DJ is taking a break ...")));
                }
                else
                {
                    if (data.ContainsKey("index"))
                    {
                        var playback = await Playlist.GetPlaybackAsyncAt(int.Parse(data["index"]));

                        data = ToDictionary(playback);
                    }

                    if (data.ContainsKey("uuid")) data.Remove("uuid");
                    if (data.ContainsKey("path")) data.Remove("path");
                }

                if (data.ContainsKey("duration")) data["duration"] = "Infinity";
                else data.Add("duration", "Infinity");

                if (data.ContainsKey("provider")) data["provider"] = "LiveStream";
                else data.Add("provider", "LiveStream");

                Stream.SendInformation(data);
            }

            public static async Task StartAsync(Song playback)
            {
                if (playback == null)
                    //TODO: [MSG] Song is null
                    return;

                JavaScript.RunScript("StartPlayback",
                    new[]
                    {
                        ("$StorageOptions", DataStorage.Type.ToString().ToLower()),
                        ("$DataId", playback.DataId.ToString())
                    });

                await SetAsync(ToDictionary(playback));
            }

            public static void PausePlay()
            {
                JavaScript.RunScript("PausePlay");
            }

            public static async Task StopAsync()
            {
                JavaScript.RunScript("StopPlayback");

                await SetAsync(null);
            }
        }

        #endregion
    }
}