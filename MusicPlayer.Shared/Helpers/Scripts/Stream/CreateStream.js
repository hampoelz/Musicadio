if (!window.AudioEngine.audio) throw new Error("AudioEngine is not initialized");
if (!window.audioContext || !window.source) throw new Error("AudioContext is not initialized");
if (!window.StreamControl.control) throw new Error("StreamInstance is not initialized");

console.log("<== Connecting to server ...");
window.StreamControl.control.dispatchEvent(new CustomEvent("onInformation", { detail: "server.connecting" }));
window.StreamControl.socket = io.connect("https://rh-utensils-socket.herokuapp.com/", { 'reconnection': true });

window.StreamControl.peers = {};
window.StreamControl.uuid;

//TODO: Host own TURN Server
var cfg = {
    'iceServers': [
        {
            'urls': "stun:stun.l.google.com:19302"
        },
        {
            url: "turn:uncreative.hampoelz.net?transport=udp",
            credential: "musicplayer",
            username: "zErcBpHfgVQA5Bza6"
        },
        {
            url: "turn:uncreative.hampoelz.net?transport=tcp",
            credential: "musicplayer",
            username: "zErcBpHfgVQA5Bza6"
        }
    ]
};

window.StreamControl.socket.on("season-id",
    id => {
        window.StreamControl.uuid = id;
        window.StreamControl.control.dispatchEvent(new CustomEvent("onInformation",
            { detail: "server.connected:uuid=" + id }));
        console.log("<=> Successfully connected to server");
        console.log("Season uuid: " + id);
    });

window.StreamControl.socket.on("connect_error",
    () => {
        console.error("Connection to server failed");
        window.StreamControl.control.dispatchEvent(new CustomEvent("onError", { detail: "server.failed" }));
    });

window.StreamControl.socket.on("disconnected",
    from => {
        if (from != window.StreamControl.uuid && window.StreamControl.peers[from]) {
            if (window.StreamControl.peers[from].stream) {
                window.StreamControl.peers[from].peerConnection.removeStream(window.StreamControl.peers[from].stream);
                window.StreamControl.peers[from].stream = undefined;
            }
            console.log("==> Client (" +
                (Object.keys(window.StreamControl.peers).indexOf(from) + 1) +
                ") has left the stream");
            window.StreamControl.control.dispatchEvent(
                new CustomEvent("onInformation", { detail: "client.disconnected" }));
            delete window.StreamControl.peers[from];
        }
    });

window.remoteDestination = window.audioContext.createMediaStreamDestination();
window.analyser.connect(window.remoteDestination);

window.StreamControl.socket.on("logon",
    message => {
        console.log("==> Client tries to connect ...");
        window.StreamControl.control.dispatchEvent(new CustomEvent("onInformation", { detail: "client.connecting" }));

        var from = message.from;

        window.StreamControl.connectionTimeout = setTimeout(() => {
                console.error("Timeout: Client (" +
                    (Object.keys(window.StreamControl.peers).indexOf(from) + 1) +
                    ") timed out");
                window.StreamControl.control.dispatchEvent(new CustomEvent("onInformation",
                    { detail: "client.timeout" }));
                if (window.StreamControl.peers[from]) delete window.StreamControl.peers[from];
            },
            4000);

        window.StreamControl.peers[from] = {
            peerConnection: new RTCPeerConnection(cfg),
            dataChannel: undefined,
            stream: undefined
        };

        var sdpConstraints = {
            optional: []
        };

        window.StreamControl.peers[from].peerConnection.onicecandidate = event => {
            if (event.candidate == null) {
                console.log("<== Send offer to client (" +
                    (Object.keys(window.StreamControl.peers).indexOf(from) + 1) +
                    ")");
                window.StreamControl.socket.emit("message",
                    {
                        from: window.StreamControl.uuid,
                        to: from,
                        data: { type: "offer", offer: window.StreamControl.peers[from].peerConnection.localDescription }
                    });
            }
        };

        window.StreamControl.peers[from].dataChannel =
            window.StreamControl.peers[from].peerConnection.createDataChannel("informations", { reliable: true });

        window.StreamControl.peers[from].dataChannel.onopen = () => {
            console.log("<=> Data channel has been opened");
            window.StreamControl.control.dispatchEvent(new CustomEvent("onInformation",
                { detail: "client.connected" }));

            console.log("==> Client (" +
                (Object.keys(window.StreamControl.peers).indexOf(from) + 1) +
                ") connected successfully");
            clearTimeout(window.StreamControl.connectionTimeout);
            window.StreamControl.connectionTimeout = undefined;

            var constraints = { mandatory: {}, optional: [] };

            console.log("<== Send stream to client (" +
                (Object.keys(window.StreamControl.peers).indexOf(from) + 1) +
                ")");
            window.StreamControl.peers[from].peerConnection.addStream(window.remoteDestination.stream, constraints);
            window.StreamControl.peers[from].stream = window.remoteDestination.stream;

            window.StreamControl.peers[from].peerConnection.createOffer(desc => {
                    window.StreamControl.peers[from].peerConnection.setLocalDescription(desc,
                        function() {},
                        function() {});
                },
                error => {
                    if (window.StreamControl.peers[from]) {
                        if (window.StreamControl.peers[from].stream) {
                            window.StreamControl.peers[from].peerConnection.removeStream(
                                window.StreamControl.peers[from].stream);
                            window.StreamControl.peers[from].stream = undefined;
                        }

                        console.error("Failed to create offer: " + error);
                        console.log(">=< Disconnect client (" +
                            (Object.keys(window.StreamControl.peers).indexOf(from) + 1) +
                            ")");
                        window.StreamControl.control.dispatchEvent(
                            new CustomEvent("onError", { detail: "client.failed" }));
                        delete window.StreamControl.peers[from];
                    }
                },
                sdpConstraints);

            console.log("<== Send stream informations to client (" +
                (Object.keys(window.StreamControl.peers).indexOf(from) + 1) +
                ")");
            window.StreamControl.peers[from].dataChannel.send("$Information");
        };

        window.StreamControl.peers[from].dataChannel.onclose = () => {
            console.log(">=< Data channel has been closed");

            if (window.StreamControl.peers[from]) {
                if (window.StreamControl.peers[from].stream) {
                    window.StreamControl.peers[from].peerConnection.removeStream(
                        window.StreamControl.peers[from].stream);
                    window.StreamControl.peers[from].stream = undefined;
                }

                delete window.StreamControl.peers[from];
                window.StreamControl.control.dispatchEvent(new CustomEvent("onInformation",
                    { detail: "client.disconnected" }));
                console.log(">=< Disconnect client (" +
                    (Object.keys(window.StreamControl.peers).indexOf(from) + 1) +
                    ")");
            }
        };

        window.StreamControl.peers[from].dataChannel.onerror = event => {
            console.error("Failed to open data channel: " + event.error);
        };

        window.StreamControl.peers[from].peerConnection.createOffer(desc => {
                window.StreamControl.peers[from].peerConnection.setLocalDescription(desc, () => {}, () => {});
            },
            error => {
                console.error("Failed to create offer: " + error);
                window.StreamControl.control.dispatchEvent(new CustomEvent("onError", { detail: "client.failed" }));
                if (window.StreamControl.peers[from]) delete window.StreamControl.peers[from];
            },
            sdpConstraints);
    });

window.StreamControl.socket.on("message",
    message => {
        var from = message.from;

        if (message.data.type === "answer") {
            console.log("==> Received answer from client (" +
                (Object.keys(window.StreamControl.peers).indexOf(from) + 1) +
                ")");
            var answer = message.data.answer;
            var answerDesc = new RTCSessionDescription(answer);

            window.StreamControl.peers[from].peerConnection.setRemoteDescription(answerDesc);
        }
    });