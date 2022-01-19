using System.Globalization;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Uno.UI.Runtime.WebAssembly;
using Windows.UI.Xaml.Media;

namespace Musicadio.Shared.Html.Api.Components
{
    [HtmlElement("canvas")]
    public class SpectrumAnalyzer : Control
    {
        public SpectrumAnalyzer(MediaElement mediaElement)
        {
            MediaElement = mediaElement;

            SizeChanged += OnSizeChanged;
            RegisterPropertyChangedCallback(ForegroundProperty, ForegroundChanged);

            var media = $"document.getElementById('{mediaElement.GetHtmlId()}')";
            var javascript = $@"
                (function initialize() {{
                    const analyser = {media}.context.analyser;
                    const ctx = element.getContext('2d');

                    let bufferLength = analyser.frequencyBinCount;
                    let dataArray = new Uint8Array(bufferLength);

                    let barHeight;
                    let x = 0;

                    function renderFrame() {{
                        analyser.getByteFrequencyData(dataArray);

                        let width = element.width;
                        let height = element.height;

                        let background = element.style.backgroundColor;

                        let barColor = element.barColor;
                        let barWidth = element.barWidth;
                        let barDistance = element.barDistance;
                        let barRadius = element.barCornerRadius;

                        x = barDistance;

                        let grad = ctx.createLinearGradient(0, 0, 0, height);
                        grad.addColorStop(0.5, barColor);
                        grad.addColorStop(1, background);
                        ctx.fillStyle = grad;

                        ctx.clearRect(0, 0, width, height);

                        for (let i = 0; x + barDistance < width; i++) {{
                            barHeight = (dataArray[i] * 0.8);

                            ctx.roundRect(x, (height - barHeight), barWidth, barHeight, barRadius).fill();

                            x += barWidth + barDistance;
                        }}

                        requestAnimationFrame(renderFrame);
                    }}

                    CanvasRenderingContext2D.prototype.roundRect = function (x, y, w, h, r) {{
                        if (w < 2 * r) r = w / 2;
                        if (h < 2 * r) r = h / 2;
                        this.beginPath();
                        this.moveTo(x + r, y);
                        this.arcTo(x + w, y, x + w, y + h, r);
                        this.arcTo(x + w, y + h, x, y + h, r);
                        this.arcTo(x, y + h, x, y, r);
                        this.arcTo(x, y, x + w, y, r);
                        this.closePath();
                        return this;
                    }}

                    renderFrame();
                }})();";

            this.ExecuteJavascript(javascript);
        }

        public IMediaElement MediaElement { get; }

        private void OnSizeChanged(object sender, SizeChangedEventArgs args)
        {
            var size = args.NewSize;

            var newWidth = size.Width.ToString(CultureInfo.InvariantCulture);
            var newHeight = size.Height.ToString(CultureInfo.InvariantCulture);

            var javascript = $@"
                element.width = {newWidth} * devicePixelRatio;
                element.height = {newHeight} * devicePixelRatio;
                const ctx = element.getContext('2d');
                ctx.scale(devicePixelRatio, devicePixelRatio);";

            this.ExecuteJavascript(javascript);
        }

        private static void ForegroundChanged(DependencyObject dependencyobject, DependencyProperty dp)
        {
            var element = dependencyobject as ColorAnimation;

            if (!(element.Foreground is SolidColorBrush)) return;
            var brush = (SolidColorBrush)element.Foreground;

            var r = brush.Color.R;
            var g = brush.Color.G;
            var b = brush.Color.B;
            var a = brush.Color.A;
            var rgba = $"`rgba({r}, {g}, {b}, {a})`";

            element.ExecuteJavascript($"element.barColor = {rgba};");
        }

        #region Properties

        #region BarWidthProperty

        public static readonly DependencyProperty BarWidthProperty = DependencyProperty.Register(
            "BarWidth",
            typeof(int),
            typeof(SpectrumAnalyzer),
            new PropertyMetadata(12, BarWidthChanged));

        public int BarWidth
        {
            get => (int)GetValue(BarWidthProperty);
            set
            {
                if (value < 0) value = 0;
                SetValue(BarWidthProperty, value);
            }
        }

        private static void BarWidthChanged(DependencyObject dependencyobject, DependencyPropertyChangedEventArgs args)
        {
            var element = dependencyobject as SpectrumAnalyzer;
            var width = ((int)args.NewValue).ToString(CultureInfo.InvariantCulture);
            element?.ExecuteJavascript($"element.barWidth = {width};");
        }

        #endregion

        #region BarDistanceProperty

        public static readonly DependencyProperty BarDistanceProperty = DependencyProperty.Register(
            "BarDistance",
            typeof(int),
            typeof(SpectrumAnalyzer),
            new PropertyMetadata(10, BarDistanceChanged));

        public int BarDistance
        {
            get => (int)GetValue(BarDistanceProperty);
            set
            {
                if (value < 0) value = 0;
                SetValue(BarDistanceProperty, value);
            }
        }

        private static void BarDistanceChanged(DependencyObject dependencyobject, DependencyPropertyChangedEventArgs args)
        {
            var element = dependencyobject as SpectrumAnalyzer;
            var distance = ((int)args.NewValue).ToString(CultureInfo.InvariantCulture);
            element?.ExecuteJavascript($"element.barDistance = {distance};");
        }

        #endregion

        #region BarRadiusProperty

        public static readonly DependencyProperty BarRadiusProperty = DependencyProperty.Register(
            "BarRadius",
            typeof(int),
            typeof(SpectrumAnalyzer),
            new PropertyMetadata(10, BarRadiusChanged));

        public int BarRadius
        {
            get => (int)GetValue(BarRadiusProperty);
            set
            {
                if (value < 0) value = 0;
                SetValue(BarRadiusProperty, value);
            }
        }

        private static void BarRadiusChanged(DependencyObject dependencyobject, DependencyPropertyChangedEventArgs args)
        {
            var element = dependencyobject as SpectrumAnalyzer;
            var radius = ((int)args.NewValue).ToString(CultureInfo.InvariantCulture);
            element?.ExecuteJavascript($"element.barRadius = {radius};");
        }

        #endregion

        #endregion
    }
}