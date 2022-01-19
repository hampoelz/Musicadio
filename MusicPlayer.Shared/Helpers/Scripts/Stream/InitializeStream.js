if (!window.AudioEngine.audio) throw new Error("AudioEngine is not initialized");

var StreamControl = document.getElementById("$StreamControl");

window.StreamControl = {};
window.StreamControl.control = StreamControl;