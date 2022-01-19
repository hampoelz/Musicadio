using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Documents;
using MusicPlayer.Shared.Engine;
using Uno.Extensions;

namespace MusicPlayer.Shared.Helpers
{
    //TODO: Datenschutzerklärung
    public static class Stream
    {
        private static readonly Canvas Engine = (Canvas) Application.Current.Resources["Stream"];
        private static bool _isStreamCreator;
        private static bool _isInStream;

        static Stream()
        {
            Engine.RegisterHtmlCustomEventHandler("onInformation", OnInformation);
            Engine.RegisterHtmlCustomEventHandler("onError", OnError);
            Engine.RegisterHtmlCustomEventHandler("onMessage", OnMessage);

            JavaScript.RunScript("InitializeStream", new[] {("$StreamControl", Engine.GetHtmlId())});

            FragmentNavigation.FragmentChanged += FragmentNavigationOnFragmentChanged;
        }

        public static bool IsStreamCreator
        {
            get => _isStreamCreator;
            private set
            {
                _isStreamCreator = value;
                IsInStream = value;
            }
        }

        public static bool IsInStream
        {
            get => _isInStream;
            private set
            {
                if (value == _isInStream) return;

                _isInStream = value;
                AudioEngine.RaiseStaticPropertyChanged("Stream");
            }
        }

        private static async void FragmentNavigationOnFragmentChanged(object sender, EventArgs e)
        {
            var options = FragmentNavigation.CurrentFragmentOptions;

            if (!IsStreamCreator && options.ContainsKey("stream") && Guid.TryParse(options["stream"], out var stream))
            {
                var info = new TextBlock();
                info.Inlines.Add(new LineBreak());
                info.Inlines.Add(new Run {Text = "Hey! If you join this stream, the stream creator is the "});
                info.Inlines.Add(new Run {Text = "DJ", FontStyle = FontStyle.Italic});
                info.Inlines.Add(new Run {Text = ", so "});
                info.Inlines.Add(new Run {Text = "he controls the music playback", FontWeight = FontWeights.Bold});
                info.Inlines.Add(new Run {Text = " and you can listen to his songs live and completely free. "});
                //info.Inlines.Add(new LineBreak());
                info.Inlines.Add(new Run
                {
                    Text = "There may be costs for the required internet connection!", FontWeight = FontWeights.Light
                });
                info.Inlines.Add(new LineBreak());
                info.Inlines.Add(new LineBreak());
                info.Inlines.Add(new Run
                    {Text = "Do you want to join and enjoy music?", FontWeight = FontWeights.Bold});
                info.TextWrapping = TextWrapping.WrapWholeWords;

                var joinDialog = new ContentDialog
                {
                    Title = "You got a link for joining a streaming stream.",
                    Content = info,
                    CloseButtonText = "Cancel",
                    PrimaryButtonText = "Join",
                    CornerRadius = 5,
                    MaxWidth = 500
                };

                var result = await joinDialog.ShowAsync();

                if (result == ContentDialogResult.Primary) await Join(stream);
            }
        }

        public static void Create(Dictionary<string, string> data = null)
        {
            if (_isInStream)
                //TODO: [MSG] Already in Stream
                return;

            if (data == null) data = new Dictionary<string, string>();

            var optionsString = Settings.GetOptionsString(data);

            JavaScript.RunScript("CreateStream", new[] {("$Information", optionsString)});
        }

        public static void SendInformation(Dictionary<string, string> data)
        {
            if (!IsStreamCreator) return;

            var optionsString = Settings.GetOptionsString(data);

            JavaScript.RunScript("SendStreamInformation", new[] {("$Information", optionsString)});
        }

        public static async Task Join(Guid uuid)
        {
            if (_isInStream) await Leave();

            JavaScript.RunScript("JoinStream", new[] {("$StreamId", uuid.ToString())});
        }

        public static async Task Leave(bool force = false)
        {
            if (!_isInStream)
                //TODO: [MSG] Already in Stream
                return;

            if (!IsStreamCreator) await AudioEngine.Playback.SetAsync(null);
            else IsStreamCreator = false;

            IsInStream = false;

            JavaScript.RunScript("LeaveStream", new[] {("$ForceDisconnect", force.ToString().ToLower())});

            var fragmentOptions = FragmentNavigation.CurrentFragmentOptions;
            if (fragmentOptions.ContainsKey("stream")) fragmentOptions.Remove("stream");
            FragmentNavigation.CurrentFragmentOptions = fragmentOptions;
        }

        private static async void OnInformation(object sender, HtmlCustomEventArgs e)
        {
            var message = e.Detail.Split(':');

            var options = Settings.GetOptions(message.Length > 1 ? message[1] : "");

            switch (message[0])
            {
                case "server.connecting":
                    //TODO: [MSG] Connecting to Server
                    break;
                case "server.connected":
                    if (options.ContainsKey("uuid"))
                    {
                        IsStreamCreator = true;

                        var uuid = options["uuid"];

                        var fragmentOptions = FragmentNavigation.CurrentFragmentOptions;
                        if (fragmentOptions.ContainsKey("stream")) fragmentOptions["stream"] = uuid;
                        else fragmentOptions.Add("stream", uuid);
                        FragmentNavigation.CurrentFragmentOptions = fragmentOptions;
                    }

                    break;
                case "client.connecting":
                    //TODO: [MSG] Client tries to Connect
                    break;
                case "client.connected":
                {
                    AudioEngine.SendCover();
                    await AudioEngine.Playback.SendInfoAsync();
                    //TODO: Connected client counter
                    break;
                }
                case "client.disconnected":
                    //TODO: [MSG] Client disconnected
                    break;
                case "client.timeout":
                    //TODO: [MSG] Client connection timeout
                    break;
                case "stream.connecting":
                    //TODO: [MSG] Connecting to Stream
                    break;
                case "stream.connected":
                    IsInStream = true;
                    //TODO: [MSG] Connected to Stream
                    break;
                case "stream.disconnected":
                    await Leave();
                    //TODO: [MSG] Lost connection to Stream
                    break;
                case "stream.stream":
                    break;
            }
        }

        private static async void OnMessage(object sender, HtmlCustomEventArgs e)
        {
            var options = Settings.GetOptions(e.Detail);

            if (options.ContainsKey("title"))
            {
                var data = new Dictionary<string, string>
                    {{"title", options["title"]}, {"duration", options["duration"]}, {"provider", options["provider"]}};

                if (options.ContainsKey("artist")) data.Add("artist", options["artist"]);
                if (options.ContainsKey("album")) data.Add("album", options["album"]);
                if (options.ContainsKey("year")) data.Add("year", options["year"]);

                await AudioEngine.Playback.SetAsync(data);
            }

            if (options.ContainsKey("coverPackages") || options.ContainsKey("cover"))
            {
                var data = new Dictionary<string, string>();

                if (options.ContainsKey("coverPackages")) data.Add("coverPackages", options["coverPackages"]);
                if (options.ContainsKey("coverPackage")) data.Add("coverPackage", options["coverPackage"]);
                if (options.ContainsKey("cover")) data.Add("cover", options["cover"]);

                var cover = CoverReceiver.GetCover(data);

                if (cover != null) AudioEngine.Playback.Cover = cover;
            }
        }

        private static async void OnError(object sender, HtmlCustomEventArgs e)
        {
            switch (e.Detail)
            {
                case "server.failed":
                    IsStreamCreator = false;
                    //TODO: [MSG] Connection to Server failed
                    break;
                case "client.failed":
                    //TODO: [MSG] Connection to Client failed
                    break;
                case "stream.timeout":
                    await Leave();
                    //TODO: [MSG] Stream connection timeout
                    break;
                case "stream.notFound":
                    await Leave();
                    //TODO: [MSG] Stream not found
                    break;
            }
        }

        private static class CoverReceiver
        {
            private static int _tmpCoverPackageCounter;
            private static string[] _tmpCover;

            public static string GetCover(IReadOnlyDictionary<string, string> data)
            {
                if (data.ContainsKey("coverPackages"))
                {
                    _tmpCover = new string[int.Parse(data["coverPackages"])];
                    _tmpCoverPackageCounter = 0;
                }

                if (!data.ContainsKey("cover")) return null;

                var package = int.Parse(data["coverPackage"]);

                Console.WriteLine($"    Received package {package + 1}/{_tmpCover.Length}");

                _tmpCover[package] = data["cover"];
                ++_tmpCoverPackageCounter;

                if (_tmpCoverPackageCounter != _tmpCover.Length) return null;

                Console.WriteLine("    Combine packages");
                return string.Join(string.Empty, _tmpCover);
            }
        }
    }
}