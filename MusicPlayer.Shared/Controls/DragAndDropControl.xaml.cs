using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;
using MusicPlayer.Shared.Helpers;
using Uno.Extensions;

namespace MusicPlayer.Shared.Controls
{
    public sealed partial class DragAndDropControl
    {
        private int _counter;
        private bool _isDragOver;

        public DragAndDropControl()
        {
            this.InitializeComponent();
        }

        private static bool PlayFile
        {
            set => JavaScript.Invoke($"window.DragAndDrop.playFile = {value.ToString().ToLower()};");
        }

        protected override bool IsSimpleLayout => base.IsSimpleLayout;

        protected override bool CanCreateTemplateWithoutParent => base.CanCreateTemplateWithoutParent;

        public override object Content { get => base.Content; set => base.Content = value; }
        public override UIElement ContentTemplateRoot { get => base.ContentTemplateRoot; protected set => base.ContentTemplateRoot = value; }

        private void DragAndDropControl_OnLoaded(object sender, RoutedEventArgs e)
        {
            MainPage.GetCurrentPage().RegisterHtmlCustomEventHandler("onFileDragOver", OnFileDragOver);
            MainPage.GetCurrentPage().RegisterHtmlCustomEventHandler("onFileDragLeave", OnFileDragLeave);
            MainPage.GetCurrentPage().RegisterHtmlCustomEventHandler("onFileDrop", OnFileDrop);
            MainPage.GetCurrentPage().RegisterHtmlCustomEventHandler("onEnterPlayFileArea", OnEnterPlayFileArea);
            MainPage.GetCurrentPage().RegisterHtmlCustomEventHandler("onEnterAddFileArea", OnEnterAddFileArea);

            JavaScript.RunScript("InitializeDragAndDropControl", new[]
            {
                ("$DragAndDropArea", MainPage.GetCurrentPage().GetHtmlId()),
                ("$PlayFileArea", DropFileToPlay.GetHtmlId()),
                ("$AddFileArea", DropFileToAdd.GetHtmlId()),
                ("$UsePwaWrapper", App.UsePwaWrapper.ToString().ToLower())
            });

            MainPage.StateChanged += (o, args) => VisualStateManager.GoToState(this, args.PropertyName, false);

            DragAndDropArea.SetCssStyle("zIndex", "10");
        }

        private void OnFileDragOver(object sender, HtmlCustomEventArgs e)
        {
            _counter++;

            if (_isDragOver) return;

            _isDragOver = true;

            DragAndDropArea.Visibility = Visibility.Visible;

            if (Convert.ToInt32(e.Detail) > 1)
            {
                DropFileToPlay.Visibility = Visibility.Collapsed;
                DropFileToAdd.Visibility = Visibility.Collapsed;
                DropFilesToAdd.Visibility = Visibility.Visible;
            }
            else
            {
                DropFileToPlay.Visibility = Visibility.Visible;
                DropFileToAdd.Visibility = Visibility.Visible;
                DropFilesToAdd.Visibility = Visibility.Collapsed;
            }
        }

        private async void OnFileDragLeave(object sender, EventArgs e)
        {
            _counter--;

            if (_counter <= 1) return;

            _isDragOver = false;

            var timeout = new Stopwatch();
            timeout.Start();

            while (true)
            {
                await Task.Delay(10);

                if (_isDragOver) return;

                if (timeout.ElapsedMilliseconds > 100) break;
            }

            _counter = 0;

            DragAndDropArea.Visibility = Visibility.Collapsed;

            DropFileToAdd.BorderBrush = (Brush) new BrushConverter().ConvertFromString("#82B1FF");
            DropFileToAdd.BorderBrush = (Brush) new BrushConverter().ConvertFromString("#82B1FF");

            PlayFile = false;
        }

        private void OnFileDrop(object sender, EventArgs e)
        {
            _isDragOver = false;
            _counter = 0;

            DragAndDropArea.Visibility = Visibility.Collapsed;

            DropFileToAdd.BorderBrush = (Brush) new BrushConverter().ConvertFromString("#82B1FF");
            DropFileToAdd.BorderBrush = (Brush) new BrushConverter().ConvertFromString("#82B1FF");

            PlayFile = false;
        }

        private void OnEnterPlayFileArea(object sender, EventArgs e)
        {
            PlayFile = true;

            DropFileToPlay.BorderBrush = (Brush) new BrushConverter().ConvertFromString("#FFFFFF");
            DropFileToAdd.BorderBrush = (Brush) new BrushConverter().ConvertFromString("#82B1FF");
        }

        private void OnEnterAddFileArea(object sender, EventArgs e)
        {
            PlayFile = false;

            DropFileToAdd.BorderBrush = (Brush) new BrushConverter().ConvertFromString("#FFFFFF");
            DropFileToPlay.BorderBrush = (Brush) new BrushConverter().ConvertFromString("#82B1FF");
        }
    }
}