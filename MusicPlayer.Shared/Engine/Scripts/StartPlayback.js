if (!window.AudioEngine.audio) throw new Error("AudioEngine is not initialized");
let storage = new window.Sifrr.Storage("$StorageOptions");

storage.get("$DataId").then(data => {
    var key = Object.keys(data)[0];
    var file = data[key];

    if (file == undefined) {
        window.AudioEngine.audio.addEventListener("error",
            () => AudioEngine.dispatchEvent(new CustomEvent("onFailedPlay", { detail: "notFound" })));
        return;
    }

    window.AudioEngine.audio.pause();
    window.AudioEngine.audio.currentTime = 0;
    window.AudioEngine.audio.src = URL.createObjectURL(file);

    if (window.AudioEngine.audio.paused) window.AudioEngine.audio.play();

    jsmediatags.read(file,
        {
            onSuccess: (tags) => {
                if (tags.tags.picture) {
                    var cover = tags.tags.picture;
                    var base64String = "";

                    for (var char = 0; char < cover.data.length; char++) {
                        base64String += String.fromCharCode(cover.data[char]);
                    }

                    window.AudioEngine.setCover(window.btoa(base64String));
                } else window.AudioEngine.setCover();
            },
            onError: () => {
                window.AudioEngine.setCover();
            }
        });
});