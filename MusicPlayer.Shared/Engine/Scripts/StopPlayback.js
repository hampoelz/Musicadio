if (!window.AudioEngine.audio) throw new Error("AudioEngine is not initialized");

window.AudioEngine.audio.src = "data:audio/wav;base64,UklGRiQAAABXQVZFZm10IBAAAAABAAEAVFYAAFRWAAABAAgAZGF0YQAAAAA=";

window.AudioEngine.setCover();

if (window.AudioEngine.audio.play() !== undefined)
    window.AudioEngine.audio.play().then(() => window.AudioEngine.audio.pause());