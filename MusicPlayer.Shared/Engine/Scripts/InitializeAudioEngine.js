var AudioEngine = document.getElementById("$AudioEngine");
let storage = new window.Sifrr.Storage("$StorageOptions");

var canvas = document.createElement("canvas");
canvas.id = "SpectrumAnalyzer";
// canvas.style.position = "fixed";
// canvas.style.height = "100%";
// canvas.style.width = "100%";
// canvas.style.top = "0";
// canvas.style.left = "0";

window.AudioEngine = {};
window.AudioEngine.audio = new Audio();
window.AudioEngine.audio.preload = "none";
window.AudioEngine.audio.autoplay = true;
window.AudioEngine.audio.crossOrigin = "anonymous";

window.AudioEngine.audio.addEventListener("play",
    () => AudioEngine.dispatchEvent(new Event("onPlay")));
window.AudioEngine.audio.addEventListener("pause",
    () => AudioEngine.dispatchEvent(new Event("onPause")));
window.AudioEngine.audio.addEventListener("ended",
    () => AudioEngine.dispatchEvent(new Event("onEnded")));
window.AudioEngine.audio.addEventListener("progress",
    () => AudioEngine.dispatchEvent(new Event("onDownload")));
window.AudioEngine.audio.addEventListener("timeupdate",
    () => AudioEngine.dispatchEvent(new Event("onTimeUpdate")));
window.AudioEngine.audio.addEventListener("durationchange",
    () => AudioEngine.dispatchEvent(new Event("onDurationChange")));
window.AudioEngine.audio.addEventListener("volumechange",
    () => AudioEngine.dispatchEvent(new Event("onVolumeChange")));

window.AudioEngine.audio.addEventListener("abort",
    () => AudioEngine.dispatchEvent(new CustomEvent("onFailedPlay", { detail: "abort" })));
window.AudioEngine.audio.addEventListener("stalled",
    () => AudioEngine.dispatchEvent(new CustomEvent("onFailedPlay", { detail: "stalled" })));
window.AudioEngine.audio.addEventListener("suspend",
    () => AudioEngine.dispatchEvent(new CustomEvent("onFailedPlay", { detail: "suspend" })));
window.AudioEngine.audio.addEventListener("waiting",
    () => AudioEngine.dispatchEvent(new CustomEvent("onFailedPlay", { detail: "waiting" })));
window.AudioEngine.audio.addEventListener("error",
    () => AudioEngine.dispatchEvent(new CustomEvent("onFailedPlay", { detail: "error" })));

// Problems with Pause/Play
window.AudioEngine.audio.addEventListener('canplaythrough', (event) => URL.revokeObjectURL(event.target.src));

var defaultColor = window.matchMedia && window.matchMedia("(prefers-color-scheme: dark)").matches ?
    { r: 25, g: 118, b: 210 } : { r: 100, g: 181, b: 246 };

window.AudioEngine.color = defaultColor;
window.AudioEngine.cover = "";

var input = document.createElement("input");
input.id = "FileSelector";
input.style.display = "none";
input.type = "file";
input.accept = "$FileExtension";

input.addEventListener("change", event => window.AudioEngine.handleFiles(event.target.files, event.target.multiple));

AudioEngine.appendChild(input);
AudioEngine.appendChild(canvas);

function sizeChange() {
    canvas.width = parseInt(getComputedStyle(AudioEngine).width, 10)
    canvas.height = parseInt(getComputedStyle(AudioEngine).height, 10)
}

sizeChange();
new ResizeObserver(sizeChange).observe(AudioEngine)

if ($UsePwaWrapper) window.ipcRenderer.on("handleFiles", (_, files) => window.AudioEngine.handleFiles(files, true));

window.AudioEngine.fileExtensions = "$FileExtension".split(",");

window.AudioEngine.handleFiles = (async (fileList, multiple) => {

    var files = Array.from(fileList);

    for (var i = 0, file; file = fileList[i]; i++) {

        var extension = "." + file.name.split(".").pop();

        if (!window.AudioEngine.fileExtensions.includes(extension)) {
            var index = files.indexOf(file);
            if (index > -1) {
                files.splice(index, 1);
            }
        }
    }

    AudioEngine.dispatchEvent(new CustomEvent("handleFiles", { detail: `Initialize:traffic=${files.length};` }));

    for (var i = 0, file; file = files[i]; i++) {
        var uuid = uuidv4();

        if (!multiple) uuid = "$CurrentPlaybackGuid";

        storage.set(uuid, file);

        var fileName = btoa(encodeURIComponent(file.name));
        var data = `file=${fileName};` + `uuid=${uuid};`;

        var tags;

        try {
            tags = await readMetadataAsync(file);
        } catch (exception) {
            tags = undefined;
            console.warn(exception);
        }

        if (tags?.tags.title) {
            var title = btoa(encodeURIComponent(tags.tags.title));
            data += `title=${title};`;
        }

        if (tags?.tags.artist) {
            var artist = btoa(encodeURIComponent(tags.tags.artist));
            data += `artist=${artist};`;
        }

        if (tags?.tags.album) {
            var album = btoa(encodeURIComponent(tags.tags.album));
            data += `album=${album};`;
        }

        if (tags?.tags.year) {
            var year = tags.tags.year;
            data += `year=${year};`;
        }

        var duration = await getDurationAsync(file);
        data += `duration=${duration};`;

        if ($UsePwaWrapper) {
            var path = btoa(encodeURIComponent(file.path));
            data += `path=${path};`;
        }

        data += `provider=File;`;

        AudioEngine.dispatchEvent(new CustomEvent("handleFiles", { detail: data }));

        if (!multiple) break;
    }
});

window.AudioEngine.changeColor = (color => {
    window.AudioEngine.color.animation = true;
    var oldColor = window.AudioEngine.color;

    if (!color) color = defaultColor;

    var interval = 10;
    var steps = 500 / interval;
    var step_u = 1.0 / steps;
    var u = 0.0;

    var theInterval = setInterval(() => {
            if (u >= 1.0) {
                clearInterval(theInterval);
            }

            var r = Math.round(lerp(oldColor.r, color.r, u));
            var g = Math.round(lerp(oldColor.g, color.g, u));
            var b = Math.round(lerp(oldColor.b, color.b, u));
            var newColor = { r: r, g: g, b: b };

            window.AudioEngine.color = newColor;
            AudioEngine.dispatchEvent(new Event("onColorChange"));

            u += step_u;
        },
        interval);
});

var ImageControl = document.getElementById("$ImageControl").getElementsByTagName("img")[0];
window.AudioEngine.setCover = (base64String => {

    if (!base64String) {
        window.AudioEngine.cover =
            "data:image/svg+xml;base64,PHN2ZyB4bWxucz0iaHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmciIHhtbG5zOnhsaW5rPSJodHRwOi8vd3d3LnczLm9yZy8xOTk5L3hsaW5rIiB3aWR0aD0iMTA1MCIgaGVpZ2h0PSIxMDUwIiBmaWxsPSIjZmZmZmZmIiB2aWV3Qm94PSIwIDAgMTI5NSAxMjk1IiB4bWxuczp2PSJodHRwczovL3ZlY3RhLmlvL25hbm8iPjxkZWZzPjxsaW5lYXJHcmFkaWVudCBpZD0iQSIgZ3JhZGllbnRVbml0cz0idXNlclNwYWNlT25Vc2UiPjxzdG9wIG9mZnNldD0iMCIvPjxzdG9wIHN0b3Atb3BhY2l0eT0iMCIgb2Zmc2V0PSIxIi8+PC9saW5lYXJHcmFkaWVudD48bGluZWFyR3JhZGllbnQgaWQ9IkIiIHgxPSI3MDAiIHgyPSI3NzcuNzA5IiB5MT0iMzgwLjE4MiIgeTI9IjMwMi40NzQiIHhsaW5rOmhyZWY9IiNBIi8+PGxpbmVhckdyYWRpZW50IGlkPSJDIiB4MT0iOC4wNjMiIHgyPSI5LjM1OSIgeTE9IjQuNjE5IiB5Mj0iNS45MTYiIHhsaW5rOmhyZWY9IiNBIi8+PGxpbmVhckdyYWRpZW50IGlkPSJEIiB4MT0iMTMuMzEyIiB4Mj0iMTEuNTgzIiB5MT0iMTQuMDE4IiB5Mj0iMTIuMjg5IiB4bGluazpocmVmPSIjQSIvPjxsaW5lYXJHcmFkaWVudCBpZD0iRSIgeDE9IjkxOC4zOSIgeDI9IjEwMTAuODU1IiB5MT0iMTk4Ljg5NyIgeTI9IjI5MS4zMjciIHhsaW5rOmhyZWY9IiNBIi8+PGZpbHRlciBpZD0iRiIgeD0iLS4xMjYiIHk9Ii0uMDc4IiB3aWR0aD0iMS4yNTIiIGhlaWdodD0iMS4xNTUiIGNvbG9yLWludGVycG9sYXRpb24tZmlsdGVycz0ic1JHQiI+PGZlR2F1c3NpYW5CbHVyIHN0ZERldmlhdGlvbj0iLjEyMSIvPjwvZmlsdGVyPjxmaWx0ZXIgIHg9Ii0uMDc3IiB5PSItLjA0OSIgd2lkdGg9IjEuMTU0IiBoZWlnaHQ9IjEuMDk4IiBjb2xvci1pbnRlcnBvbGF0aW9uLWZpbHRlcnM9InNSR0IiPjxmZUdhdXNzaWFuQmx1ciBzdGREZXZpYXRpb249IjI0LjAyOCIvPjwvZmlsdGVyPjxwYXRoIGlkPSJIIiBkPSJNMTA4OCAxNzFMODc4LjgyIDM4MC4xOGg5MS42NzJjNzQuMzg3IDAgMTM1LjI0LTYzLjMzNyAxMzUuMjQtMTQwLjc0IDAtMjQuOTAzLTYuNzA2LTQ4LjA5NS0xNy43My02OC40NDR6Ii8+PHBhdGggaWQ9IkkiIGQ9Ik04MzUuMjQgOTguNzA1Yy03NC4zODcgMC0xMzUuMjQgNjMuMzM3LTEzNS4yNCAxNDAuNzR2NS40OThsMTM1LjI0IDEzNS4yNGg0My41NjhsMjA5LjE4LTIwOS4xOGMtMjMuMjQtNDIuODk2LTY3LjA1OC03Mi4yOTMtMTE3LjUtNzIuMjkzeiIvPjwvZGVmcz48ZyB0cmFuc2Zvcm09Im1hdHJpeCguNzMxNzUgMCAwIC43MzAyMyAxMzUuMjcgMTEyLjk2KSI+PHBhdGggZD0iTTU2NS44NiA4MDIuNGMtMjkuMzI1LS4xNjQtNTkuNzkzIDQuNzM0LTkwLjk4NSAxNi4wOC05MC42MTYgMzMuNzc1LTE2MC4zOCAxMTcuNTQtMTc2LjYgMjE2LjA1LTMxLjEwNyAxOTIuOCAxMjUuODYgMzU3LjUgMzEwLjQ3IDMyNy4yNCAxMzIuNTQtMjEuODEzIDIyNi41LTE0OC40NiAyMjYuNS0yODguNDh2LTk3LjAzMmwtMTM1LjI0LTEzNS4yNGMtMzkuNzMtMjMuNzQ4LTg1LjI2NS0zOC4zNC0xMzQuMTQtMzguNjJ6IiBmaWxsPSIjMTk3NmQyIi8+PHBhdGggZD0iTTgzNS4yNCA0MjMuNzVMNzAwIDU1OVY4NDFsMTM1LjI0IDEzNS4yNHoiIGZpbGw9IiMzMDNmOWYiLz48cGF0aCB0cmFuc2Zvcm09Im1hdHJpeCg3MC4zNjkgMCAwIDcwLjM2OSAxMzIuNjUgOTguNzA1KSIgZD0iTTcuOTQzIDEwLjY0MmMtLjM5Ni40NDctLjU0Ni44ODYtLjU0NyAxLjQ3NyAwIDEuMjU3IDEuMDIgMi4yNzUgMi4yNzUgMi4yNzUuMS0uMDAxLS4wMDMtLjAzLjAwOC0uMDk2LTEuODgzLS41LTIuNTY2LTIuMDItMS42NzgtMy42MjJ6IiBmaWxsPSIjMDAwIiBmaWx0ZXI9InVybCgjRikiLz48ZyB0cmFuc2Zvcm09Im1hdHJpeCg1Mi43NzcgMCAwIDUyLjc3NyAxMzIuNjUgOTguNzA1KSI+PHBhdGggZD0iTTEwLjc1IDE0LjAxOGwyLjU2My0yLjU2MlY2LjExMkwxMC43NSAzLjU1eiIgZmlsbD0idXJsKCNEKSIvPjxwYXRoIHRyYW5zZm9ybT0ic2NhbGUoMS4zMzMzKSIgZD0iTTkuOTg0IDQuNjJMOC4wNjMgNi41NHY0LjAwOGwxLjkyMiAxLjkyMnoiIGZpbGw9InVybCgjQykiLz48L2c+PHBhdGggZD0iTTgzNS4yMjMgNzAzLjI5bC0xMzYuOTM2IDEzNi45NGMtMjguOTE2IDI5LjgyOC00NS4xMSA2OS43MjUtNDUuMTY2IDExMS4yNjguMDIyIDg4LjQyIDcxLjY5NiAxNjAuMDk0IDE2MC4xMTYgMTYwLjExNiA2LjM0NC0uMDggMTIuNjc3LS41NCAxOC45NjYtMS4zNzJsLjgzLS4wOWMxLjQwOC0xMS44NzQgMi4xNTUtMjMuOTA2IDIuMTg3LTM2LjA0M2gtLjAwNWwuMDEzLS44NFY3OTMuMDM2eiIgZmlsbD0iIzY0YjVmNiIvPjx1c2UgeGxpbms6aHJlZj0iI0giIGZpbGw9IiMwZDQ3YTEiLz48dXNlIHhsaW5rOmhyZWY9IiNIIiBmaWxsPSJ1cmwoI0UpIi8+PHVzZSB4bGluazpocmVmPSIjSSIgZmlsbD0iIzIxOTZmMyIvPjx1c2UgeGxpbms6aHJlZj0iI0kiIGZpbGw9InVybCgjQikiLz48cGF0aCBkPSJNNzAwIDI0NC45NFY1NTlsMTM1LjI0LTEzNS4yNHYtNDMuNTY4eiIgZmlsbD0iIzY0YjVmNiIvPjwvZz48L3N2Zz4=";
        ImageControl.src = window.AudioEngine.cover;
        window.AudioEngine.changeColor();

        AudioEngine.dispatchEvent(new Event("onCoverChange"));
    } else {
        var base64 = "data:image/jpeg;base64," + base64String;

        Vibrant.from(base64)
            .quality(1)
            .clearFilters()
            .build()
            .getPalette()
            .then((palette) => {
                var LightVibrant = {
                    r: Math.round(palette.LightVibrant.r),
                    g: Math.round(palette.LightVibrant.g),
                    b: Math.round(palette.LightVibrant.b)
                };
                var DarkVibrant = {
                    r: Math.round(palette.DarkVibrant.r),
                    g: Math.round(palette.DarkVibrant.g),
                    b: Math.round(palette.DarkVibrant.b)
                };

                var color = LightVibrant;

                if (window.matchMedia && window.matchMedia("(prefers-color-scheme: dark)").matches)
                    color = DarkVibrant;

                window.AudioEngine.cover = base64String;
                ImageControl.src = base64;

                window.AudioEngine.changeColor(color);

                AudioEngine.dispatchEvent(new Event("onCoverChange"));
            });
    }
});

function lerp(a, b, u) {
    return (1 - u) * a + u * b;
}

async function getDurationAsync(file) {
    return new Promise((resolve, reject) => {
        var audio = new Audio();

        audio.onloadedmetadata = (event => {
            var duration = event.target.duration;
            URL.revokeObjectURL(event.target.src);
            resolve(duration);
        });

        audio.onerror = (() => reject);
        audio.onabort = (() => reject);
        audio.onstalled = (() => reject);
        audio.onsuspend = (() => reject);

        audio.src = URL.createObjectURL(file);
    });
}

async function readMetadataAsync(file) {
    return new Promise((resolve, reject) => {
        jsmediatags.read(file,
            {
                onSuccess: resolve,
                onError: reject
            });
    });
}

async function readBase64DataAsync(file) {
    return new Promise((resolve, reject) => {
        var reader = new FileReader();

        reader.onloadend = (event => resolve(event.target.result));
        reader.onerror = (() => reject);
        reader.onabort = (() => reject);

        reader.readAsDataURL(file);
    });
}