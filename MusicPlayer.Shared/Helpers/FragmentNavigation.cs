using System;
using System.Collections.Generic;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using MusicPlayer.Shared.Helpers;
using Uno.Foundation;

namespace MusicPlayer.Shared
{
    public static class FragmentNavigation
    {
        private static readonly Canvas Engine = (Canvas) Application.Current.Resources["Navigation"];

        static FragmentNavigation()
        {
            ((Grid) MainPage.GetCurrentPage().Content).Children.Add(Engine);

            Engine.RegisterHtmlCustomEventHandler("onFragmentChange",
                delegate { RaiseFragmentChanged(); });

            JavaScript.Invoke(
                $"window.onhashchange = () => document.getElementById('{Engine.GetHtmlId()}').dispatchEvent(new Event(\"onFragmentChange\"));");
        }

        public static Dictionary<string, string> CurrentFragmentOptions
        {
            get
            {
                var fragment = JavaScript.Invoke("window.location.hash;");
                var optionsString = RemoveLeadingHash(fragment);

                return Settings.GetOptions(optionsString);
            }
            set
            {
                var optionsString = Settings.GetOptionsString(value);
                var escaped = WebAssemblyRuntime.EscapeJs(optionsString);
                var command = $"window.location.hash = '{escaped}';";

                WebAssemblyRuntime.InvokeJS(command);
            }
        }

        private static string RemoveLeadingHash(string fragmentName)
        {
            while (fragmentName.StartsWith("#")) fragmentName = fragmentName.Substring(1);

            return fragmentName;
        }

        #region NotifyFragmentChanged

        public static event EventHandler<EventArgs> FragmentChanged;

        public static void RaiseFragmentChanged()
        {
            FragmentChanged?.Invoke(null, new EventArgs());
        }

        #endregion
    }
}