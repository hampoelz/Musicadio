using System;
using System.ComponentModel;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Media;
using MusicPlayer.Shared.Engine;
using MusicPlayer.Shared.Helpers;

namespace MusicPlayer.Shared.Controls
{
    public sealed partial class MusicControl
    {
        public MusicControl()
        {
            this.InitializeComponent();

            var canvas = (Canvas) Application.Current.Resources["SpectrumAnalyzer"];
            if (canvas.Parent == null)
            {
                SpectrumAnalyzer.Child = canvas;
            }
            else
            {
                Navigation.Height = 120;
                Visualizer.Height = 120;

                Navigation.VerticalAlignment = VerticalAlignment.Bottom;
                Visualizer.VerticalAlignment = VerticalAlignment.Bottom;

                Navigation.Margin = new Thickness(0);
                NavigationBackground.CornerRadius = 0;

                Visualizer.ElevatedContent = null;
            }
        }

        private void MusicControl_OnLoaded(object sender, RoutedEventArgs e)
        {
            AudioEngine.StaticPropertyChanged += AudioEngine_OnStaticPropertyChanged;
            MainPage.StateChanged += MainPage_OnStateChanged;

            if (Visualizer.ElevatedContent != null) return;
            Navigation.SetCssClass("shadow");
            Visualizer.SetCssClass("shadow");
        }

        private void MainPage_OnStateChanged(object sender, PropertyChangedEventArgs e)
        {
            VisualStateManager.GoToState(this, e.PropertyName, false);

            SongSlider.Visibility = Visibility.Visible;
            SecondaryTools.Visibility = Visibility.Visible;

            switch (e.PropertyName)
            {
                case "Desktop": // Height = 330;
                    if (Visualizer.ElevatedContent != null)
                    {
                        Navigation.SetCssClass(new[] {"rounded", "shadow"});
                        Visualizer.SetCssClass(new[] {"shadow"});

                        Visualizer.Height = 330;
                        Navigation.Height = 150;
                    }

                    break;
                case "Tablet":
                case "Phone": // Height = 240;
                    if (Visualizer.ElevatedContent != null)
                    {
                        Navigation.UnsetCssClass(new[] {"rounded", "shadow"});
                        Visualizer.UnsetCssClass("shadow");

                        Visualizer.Height = 240;
                        Navigation.Height = 240;
                    }
                    else if (e.PropertyName == "Phone")
                    {
                        SongSlider.Visibility = Visibility.Collapsed;
                        SecondaryTools.Visibility = Visibility.Collapsed;
                    }

                    break;
            }
        }

        private async void AudioEngine_OnStaticPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "Playback":
                {
                    var currentPlayBack = await AudioEngine.Playback.GetAsync();

                    SongTitle.Text = currentPlayBack == null
                        ? "Let's start playing"
                        : currentPlayBack.Title;

                    break;
                }
                case "Color":
                    Visualizer.Background = AudioEngine.AccentColor;
                    break;
                case "IsPlaying":
                    PlaySymbol.Visibility = AudioEngine.IsPlaying ? Visibility.Collapsed : Visibility.Visible;
                    PauseSymbol.Visibility = AudioEngine.IsPlaying ? Visibility.Visible : Visibility.Collapsed;
                    break;
                case "Mute":
                    MuteToggleButton.BorderBrush = AudioEngine.Mute
                        ? new SolidColorBrush(App.IsDarkMode
                            ? App.IsDarkMode ? Colors.White : Colors.Black
                            : Colors.Black)
                        : new SolidColorBrush(Colors.Transparent);
                    break;
                case "Shuffle":
                    ShuffleToggleButton.BorderBrush = await AudioEngine.Playlist.IsShuffleAsync()
                        ? new SolidColorBrush(App.IsDarkMode ? Colors.White : Colors.Black)
                        : new SolidColorBrush(Colors.Transparent);
                    break;
                case "Loop":
                    RepeatAllToggleButton.BorderBrush = AudioEngine.Loop
                        ? new SolidColorBrush(App.IsDarkMode ? Colors.White : Colors.Black)
                        : new SolidColorBrush(Colors.Transparent);
                    break;
                case "Duration":
                {
                    var currentPlayBack = await AudioEngine.Playback.GetAsync();
                    var playbackDuration = AudioEngine.Playback.Duration;

                    var isEnabled = currentPlayBack != null &&
                                    currentPlayBack.Provider != AudioEngine.SongProvider.LiveStream &&
                                    !double.IsNaN(playbackDuration) && !double.IsInfinity(playbackDuration);

                    SongSeekBar.IsEnabled = isEnabled;
                    SongSeekBar.Maximum = isEnabled ? playbackDuration : 0;
                    SongDuration.Text = isEnabled
                        ? TimeSpan.FromSeconds(playbackDuration).ToString(@"mm\:ss")
                        : "00:00";

                    break;
                }
                case "Position":
                    SongSeekBar.Value = SongSeekBar.IsEnabled ? AudioEngine.Playback.CurrentTime : 0;
                    break;
                case "Volume":
                    VolumeSeekBar.Value = Convert.ToInt32(AudioEngine.Volume);
                    break;
                case "Stream":
                {
                    if (Stream.IsInStream && !Stream.IsStreamCreator)
                    {
                        LeaveRoom.Visibility = Visibility.Visible;
                        MainTools.Visibility = Visibility.Collapsed;
                    }
                    else
                    {
                        LeaveRoom.Visibility = Visibility.Collapsed;
                        MainTools.Visibility = Visibility.Visible;
                    }

                    break;
                }
            }
        }

        private void SeekBar_OnValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            var oldTime = TimeSpan.FromSeconds(e.OldValue).TotalMilliseconds;
            var newTime = TimeSpan.FromSeconds(e.NewValue).TotalMilliseconds;
            var span = Math.Abs(newTime - oldTime);

            SongPosition.Text = TimeSpan.FromSeconds(e.NewValue).ToString(@"mm\:ss");

            if (span < 500) return;

            AudioEngine.Playback.CurrentTime = e.NewValue;
        }

        private async void Shuffle_OnClick(object sender, RoutedEventArgs e)
        {
            await AudioEngine.Playlist.ToggleShuffleAsync();
        }

        private async void Previous_OnClick(object sender, RoutedEventArgs e)
        {
            await AudioEngine.Playlist.SkipPreviousAsync();
        }

        private async void PausePlay_OnClick(object sender, RoutedEventArgs e)
        {
            var currentPlayBack = await AudioEngine.Playback.GetAsync();
            var playlistLength = await AudioEngine.Playlist.GetLengthAsync();

            switch (currentPlayBack)
            {
                case null when playlistLength == 0:
                    AudioEngine.OpenFileDialog(false);
                    break;
                case null:
                    await AudioEngine.Playlist.StartAsyncAt(0);
                    break;
                default:
                    AudioEngine.Playback.PausePlay();
                    break;
            }
        }

        private async void Next_OnClick(object sender, RoutedEventArgs e)
        {
            await AudioEngine.Playlist.SkipNextAsync();
        }

        private void RepeatAll_OnClick(object sender, RoutedEventArgs e)
        {
            AudioEngine.Loop = !AudioEngine.Loop;
        }

        private void Favorite_OnClick(object sender, RoutedEventArgs e)
        {
            // TODO: Favorite function
            throw new NotImplementedException();
        }

        private void Volume_OnValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (e.NewValue > Convert.ToDouble(100 / 3 * 2))
            {
                VolumeSeekBarHigh.Visibility = Visibility.Visible;
                VolumeSeekBarMedium.Visibility = Visibility.Collapsed;
                VolumeSeekBarLow.Visibility = Visibility.Collapsed;
                VolumeSeekBarMute.Visibility = Visibility.Collapsed;
            }
            else if (e.NewValue > Convert.ToDouble(100 / 3))
            {
                VolumeSeekBarHigh.Visibility = Visibility.Collapsed;
                VolumeSeekBarMedium.Visibility = Visibility.Visible;
                VolumeSeekBarLow.Visibility = Visibility.Collapsed;
                VolumeSeekBarMute.Visibility = Visibility.Collapsed;
            }
            else if (e.NewValue > 0)
            {
                VolumeSeekBarHigh.Visibility = Visibility.Collapsed;
                VolumeSeekBarMedium.Visibility = Visibility.Collapsed;
                VolumeSeekBarLow.Visibility = Visibility.Visible;
                VolumeSeekBarMute.Visibility = Visibility.Collapsed;
            }
            else if (e.NewValue.Equals(0))
            {
                VolumeSeekBarHigh.Visibility = Visibility.Collapsed;
                VolumeSeekBarMedium.Visibility = Visibility.Collapsed;
                VolumeSeekBarLow.Visibility = Visibility.Collapsed;
                VolumeSeekBarMute.Visibility = Visibility.Visible;
            }

            AudioEngine.Volume = e.NewValue;
        }

        private void Mute_OnClick(object sender, RoutedEventArgs e)
        {
            AudioEngine.Mute = !AudioEngine.Mute;
        }

        private async void LeaveRoom_OnClick(object sender, RoutedEventArgs e)
        {
            await Stream.Leave();
        }
    }
}