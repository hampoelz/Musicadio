using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using MusicPlayer.Shared;
using MusicPlayer.Shared.Engine;
using MusicPlayer.Shared.Helpers;

namespace MusicPlayer
{
    public sealed partial class MainPage : Page
    {
        public static bool IsSongNavigationVisible;

        public MainPage()
        {
            this.InitializeComponent();

            Navigation.Margin = new Thickness(0, 0, 0, Navigation.Navigation.Height * -1.5);
        }

        public static MainPage GetCurrentPage()
        {
            return (MainPage)((Frame)Window.Current.Content).Content;
        }

        private async void MainPage_OnLoaded(object sender, RoutedEventArgs e)
        {
            Cover.SetCssClass(new[] { "rounded", "shadow-sm" });

            AudioEngine.StaticPropertyChanged += AudioEngine_OnStaticPropertyChanged;


            if (string.IsNullOrEmpty(Settings.GetOptionsString(FragmentNavigation.CurrentFragmentOptions))) return;

            await Task.Delay(1000);
            FragmentNavigation.RaiseFragmentChanged();
        }

        private void AudioEngine_OnStaticPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "Stream":
                    CreateRoom.Content = Stream.IsInStream ? "Leave Room" : "Create your own room";
                    break;
            }
        }

        private async void JoinRoom_OnClick(object sender, RoutedEventArgs e)
        {
            await Stream.Join(Guid.Parse(RoomId.Text));
        }

        private async void CreateRoom_OnClick(object sender, RoutedEventArgs e)
        {
            if (Stream.IsInStream) await Stream.Leave();
            else Stream.Create();
        }

        private void Play_OnClick(object sender, RoutedEventArgs e)
        {
            if (Uri.TryCreate(Url.Text, UriKind.Absolute, out var uriResult))
                //TODO: Add external services
                Url.Text = "";
            else AudioEngine.OpenFileDialog(false);
        }

        private void AddToPlaylist_OnClick(object sender, RoutedEventArgs e)
        {
            if (Uri.TryCreate(Url.Text, UriKind.Absolute, out var uriResult))
                //TODO: Add external services
                Url.Text = "";
            else AudioEngine.OpenFileDialog(true);
        }

        private async void ClearPlaylist_OnClick(object sender, RoutedEventArgs e)
        {
            await AudioEngine.Playlist.ClearAsync();
        }

        private async void Playlist_OnClick(object sender, RoutedEventArgs e)
        {
            await AudioEngine.Playlist.StartAsyncAt(0);
        }

        /* private async Task ShowSongNavigation()
        {
            if (IsSongNavigationVisible) return;
            IsSongNavigationVisible = true;

            var from = Navigation.Margin.Bottom;

            await Navigation.Slide(AnimationExtensions.MarginPosition.Bottom, from, 0, 1000);

            await Task.Delay(2);
            Navigation.Visibility = Visibility.Visible;
        }

        private async void HideSongNavigation()
        {
            if (!IsSongNavigationVisible) return;
            IsSongNavigationVisible = false;

            var from = Navigation.Margin.Bottom;

            await Navigation.Slide(AnimationExtensions.MarginPosition.Bottom, from, Navigation.Navigation.Height * -1.5, 1000);
        } */

        private /* async */ void ScrollViewer_OnViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
        {
            // if (MusicControl.Navigation.ActualHeight <= ContentScrollViewer.VerticalOffset) await ShowSongNavigation();
            // else HideSongNavigation();
        }

        #region StateChange

        public enum State
        {
            Undefined,
            Desktop,
            Tablet,
            Phone
        }

        public static event EventHandler<PropertyChangedEventArgs> StateChanged;

        private static void RaiseStateChanged([CallerMemberName] string name = null)
        {
            StateChanged?.Invoke(null, new PropertyChangedEventArgs(name));
        }

        private static State _state = State.Undefined;

        public static State CurrentState
        {
            get => _state;
            set
            {
                if (Equals(value, _state) || value == State.Undefined) return;

                _state = value;

                VisualStateManager.GoToState(GetCurrentPage(), value.ToString(), false);
                RaiseStateChanged(value.ToString());
            }
        }

        private void MainPage_OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            var oldState = CurrentState;

            if (e.NewSize.Width < 800) CurrentState = State.Phone;
            else if (e.NewSize.Width < 1300) CurrentState = State.Tablet;
            else CurrentState = State.Desktop;

            if (Equals(CurrentState, oldState)) return;

            switch (CurrentState)
            {
                case State.Desktop:
                    SongInformations.SetCssStyle("zIndex", "0");
                    SongInformations.UnsetCssClass("shadow");
                    SongInformations.SetCssClass(new[] { "shadow-lg", "rounded" });
                    ContentGrid.SetCssClass(new[] { "shadow", "rounded" });
                    break;
                case State.Tablet:
                case State.Phone:
                    SongInformations.SetCssStyle("zIndex", "1");
                    SongInformations.SetCssClass("shadow");
                    SongInformations.UnsetCssClass(new[] { "shadow-lg", "rounded" });
                    ContentGrid.UnsetCssClass(new[] { "shadow", "rounded" });
                    break;
                case State.Undefined:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        #endregion
    }
}
