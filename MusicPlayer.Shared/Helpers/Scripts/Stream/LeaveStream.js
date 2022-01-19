if (!window.AudioEngine.audio) throw new Error("AudioEngine is not initialized");
if (!window.audioContext || !window.source) throw new Error("AudioContext is not initialized");
if (!window.StreamControl.control) throw new Error("StreamInstance is not initialized");

if (window.StreamControl.uuid || $ForceDisconnect) {
    if (window.StreamControl.stream) {
        clearTimeout(window.StreamControl.connectionTimeout);
        window.StreamControl.connectionTimeout = undefined;

        console.log(">=< Disconnected from server");
        window.StreamControl.socket.disconnect();

        window.StreamControl.stream = undefined;
        window.StreamControl.uuid = undefined;

        window.StreamControl.source.disconnect(window.analyser);
        window.StreamControl.source = undefined;

        //Unmute Analyzer Element
        window.analyser.connect(window.audioContext.destination);

        window.AudioEngine.audio.srcObject = null;

        if (window.AudioEngine.audio.play() !== undefined)
            window.AudioEngine.audio.play().then(() => window.AudioEngine.audio.pause());
    } else if (window.StreamControl.peers) {
        Object.keys(window.StreamControl.peers).forEach(from => {
            if (window.StreamControl.peers[from].stream) {
                window.StreamControl.peers[from].peerConnection.removeStream(window.StreamControl.peers[from].stream);
                window.StreamControl.peers[from].stream = undefined;
            }
        });

        console.log("<== Left stream");
        console.log(">=< Disconnected from server");
        window.StreamControl.socket.disconnect();

        window.StreamControl.socket = undefined;
        window.StreamControl.peers = undefined;
        window.StreamControl.uuid = undefined;

        window.analyser.disconnect(window.remoteDestination);
        window.remoteDestination = undefined;
    }
}