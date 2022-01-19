using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using MusicPlayer.Shared.Helpers;

namespace MusicPlayer.Shared.Controls
{
    //TODO: TitleBar Rework
    public sealed partial class TitleBar : UserControl
    {
        public TitleBar()
        {
            this.InitializeComponent();
        }

        private void TitleBar_OnLoaded(object sender, RoutedEventArgs e)
        {
            var userAgent = JavaScript.Invoke("navigator.userAgent;");
            if (!userAgent.Contains(App.UserAgentPostfix)) return;

            TitleBarArea.Visibility = Visibility.Visible;

            JavaScript.Invoke($"document.getElementById('{WindowDragRegion.GetHtmlId()}')" +
                              ".style = '-webkit-app-region: drag';");
            JavaScript.Invoke($"document.getElementById('{WindowMinimizeButton.GetHtmlId()}')" +
                              ".addEventListener('click', () => window.ipcRenderer.send('app:minimize'));");
            JavaScript.Invoke($"document.getElementById('{WindowMinMaxButton.GetHtmlId()}')" +
                              ".addEventListener('click', () => window.ipcRenderer.send('app:min-max'));");
            JavaScript.Invoke($"document.getElementById('{WindowCloseButton.GetHtmlId()}')" +
                              ".addEventListener('click', () => window.ipcRenderer.send('app:quit'));");
        }

        private void WindowButtons_OnPointerEntered(object sender, PointerRoutedEventArgs e)
        {
            var border = (Border) sender;
            var bc = new BrushConverter();

            switch (border.Name)
            {
                case "WindowMinimizeButton":
                    border.Background = (Brush) bc.ConvertFrom("#1B5E20");
                    break;
                case "WindowMinMaxButton":
                    border.Background = (Brush) bc.ConvertFrom("#BF360C");
                    break;
                case "WindowCloseButton":
                    border.Background = (Brush) bc.ConvertFrom("#B71C1C");
                    break;
            }
        }

        private void WindowButtons_OnPointerExited(object sender, PointerRoutedEventArgs e)
        {
            var border = (Border) sender;
            var bc = new BrushConverter();

            switch (border.Name)
            {
                case "WindowMinimizeButton":
                    border.Background = (Brush) bc.ConvertFrom("#2E7D32");
                    break;
                case "WindowMinMaxButton":
                    border.Background = (Brush) bc.ConvertFrom("#D84315");
                    break;
                case "WindowCloseButton":
                    border.Background = (Brush) bc.ConvertFrom("#C62828");
                    break;
            }
        }

        private void WindowButtons_OnPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            var border = (Border) sender;
            var bc = new BrushConverter();

            switch (border.Name)
            {
                case "WindowMinimizeButton":
                    border.Background = (Brush) bc.ConvertFrom("#33691E");
                    break;
                case "WindowMinMaxButton":
                    border.Background = (Brush) bc.ConvertFrom("#DD2C00");
                    break;
                case "WindowCloseButton":
                    border.Background = (Brush) bc.ConvertFrom("#D50000");
                    break;
            }
        }
    }
}