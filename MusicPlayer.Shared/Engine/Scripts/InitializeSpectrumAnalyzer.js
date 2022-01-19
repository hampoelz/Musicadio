if (!window.AudioEngine.audio) throw new Error('AudioEngine is not initialized');

var alreadyLoaded = true;

if (!window.audioContext) {
    window.audioContext = new (window.AudioContext || window.webkitAudioContext);

    document.addEventListener('click', InitializeAudioContext);

    function InitializeAudioContext() {
        window.audioContext.resume();
        document.removeEventListener('click', InitializeAudioContext);
    }
}
if (!window.source) window.source = window.audioContext.createMediaElementSource(window.AudioEngine.audio);

if (!window.analyser) {
    alreadyLoaded = false;
    window.analyser = window.audioContext.createAnalyser();
    window.source.connect(window.analyser);
    window.analyser.connect(window.audioContext.destination);
}

var element = document.getElementById('SpectrumAnalyzer');

var barWidth = 12;
var barDistance = 8;
var barRadius = 10;

(function initialize() {
    var ctx = element.getContext('2d');

    var bufferLength = window.analyser.frequencyBinCount;
    var dataArray = new Uint8Array(bufferLength);

    var barHeight;

    var x = 0;

    function renderFrame() {
        window.analyser.getByteFrequencyData(dataArray);

        x = barDistance;

        var width = element.width;
        var height = element.height;

        var color = window.AudioEngine.color;
        var background = `rgb(${color.r},${color.g},${color.b})`;
        var barColor = window.matchMedia && window.matchMedia("(prefers-color-scheme: dark)").matches ?
            `rgba(255,255,255,0.5)` : `rgba(0,0,0,0.5)`;

        var grad = ctx.createLinearGradient(0, 0, 0, height);
        grad.addColorStop(0.5, barColor);
        grad.addColorStop(1, background);
        ctx.fillStyle = grad;

        ctx.clearRect(0, 0, width, height);

        for (var i = 0; x + barDistance < width; i++) {
            barHeight = (dataArray[i] * 0.8);

            ctx.roundRect(x, (height - barHeight), barWidth, barHeight, barRadius).fill();

            x += barWidth + barDistance;
        }

        requestAnimationFrame(renderFrame);
    }

    CanvasRenderingContext2D.prototype.roundRect = function (x, y, w, h, r) {
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
    }

    renderFrame()
})();