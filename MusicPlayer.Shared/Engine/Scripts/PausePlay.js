if (!window.AudioEngine.audio) throw new Error("AudioEngine is not initialized");

if (window.AudioEngine.audio.paused) window.AudioEngine.audio.play();
else {
    if (window.AudioEngine.audio.play() !== undefined)
        window.AudioEngine.audio.play().then(() => window.AudioEngine.audio.pause());
}