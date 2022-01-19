if (!window.AudioEngine.audio) throw new Error("AudioEngine is not initialized");
if (!window.audioContext || !window.source) throw new Error("AudioContext is not initialized");
if (!window.StreamControl.control) throw new Error("StreamInstance is not initialized");

console.log("<== Connecting to server ...");
window.StreamControl.control.dispatchEvent(new CustomEvent("onInformation", { detail: "server.connecting" }));
window.StreamControl.socket = io.connect("https://rh-utensils-socket.herokuapp.com/", { 'reconnection': true });

window.StreamControl.stream = "$StreamId";
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
        window.StreamControl.control.dispatchEvent(new CustomEvent("onInformation", { detail: "server.connected" }));
        console.log("<=> Successfully connected to server");

        console.log("Season id:  " + id);
        console.log("Stream id:    " + window.StreamControl.stream);

        console.log("<== Connecting to stream ...");
        window.StreamControl.control.dispatchEvent(new CustomEvent("onInformation", { detail: "stream.connecting" }));
        window.StreamControl.socket.emit("logon", { from: id, to: window.StreamControl.stream });

        window.StreamControl.connectionTimeout = setTimeout(() => {
                console.error("Timeout: Can't connect to stream");
                window.StreamControl.control.dispatchEvent(new CustomEvent("onError", { detail: "stream.timeout" }));
            },
            4000);
    });

window.StreamControl.socket.on("connect_error",
    () => {
        console.error("Connection to server failed");
        window.StreamControl.control.dispatchEvent(new CustomEvent("onError", { detail: "server.failed" }));
    });

window.StreamControl.socket.on("info",
    message => {
        if (message == "Not Found") {
            console.log("==> Stream doesn't exist");
            window.StreamControl.control.dispatchEvent(new CustomEvent("onError", { detail: "stream.notFound" }));
        } else {
            console.log("==> " + message);
        }
    });

window.StreamControl.socket.on("disconnected",
    from => {
        if (from === window.StreamControl.stream) {
            console.log("==> Host has left the stream");
            window.StreamControl.control.dispatchEvent(new CustomEvent("onInformation",
                { detail: "stream.disconnected" }));
        }
    });

var peerConnection = new RTCPeerConnection(cfg);

var sdpConstraints = { optional: [] };

peerConnection.ondatachannel = event => {
    var dataChannel = event.channel || event;

    dataChannel.onopen = () => {
        console.log("<=> Data channel has been opened");

        if (window.StreamControl.connectionTimeout) {
            console.log("==> Successfully connected to stream");
            window.StreamControl.control.dispatchEvent(new CustomEvent("onInformation",
                { detail: "stream.connected" }));
        }
        clearTimeout(window.StreamControl.connectionTimeout);
        window.StreamControl.connectionTimeout = undefined;
    };

    dataChannel.onmessage = event => {
        console.log("==> Received stream informations from host");
        window.StreamControl.control.dispatchEvent(new CustomEvent("onMessage", { detail: event.data }));
    };

    dataChannel.onclose = () => {
        console.log(">=< Data channel has been closed");
        window.StreamControl.control.dispatchEvent(new CustomEvent("onInformation", { detail: "stream.disconnected" }));
    };

    dataChannel.onerror = event => {
        console.error("Failed to open data channel: " + event.error);
    };
};

peerConnection.onicecandidate = event => {
    if (event.candidate == null) {
        console.log("<== Send answer to host");
        window.StreamControl.socket.emit("message",
            {
                from: window.StreamControl.uuid,
                to: window.StreamControl.stream,
                data: { type: "answer", answer: peerConnection.localDescription }
            });
    }
};

peerConnection.onaddstream = event => {
    console.log("==> Received stream from host");
    window.StreamControl.control.dispatchEvent(new CustomEvent("onInformation", { detail: "stream.stream" }));

    if (!window.AudioEngine.audio.paused) window.AudioEngine.audio.pause();

    window.StreamControl.source = window.audioContext.createMediaStreamSource(event.stream);
    window.StreamControl.source.connect(window.analyser);

    //Mute Analyzer Element
    window.analyser.disconnect(window.audioContext.destination);

    window.AudioEngine.audio.srcObject = event.stream;
    window.AudioEngine.audio.play();
};

window.StreamControl.socket.on("message",
    message => {
        if (message.data.type === "offer") {
            console.log("==> Received offer from host");
            var offer = message.data.offer;
            var offerDesc = new RTCSessionDescription(offer);

            peerConnection.setRemoteDescription(offerDesc);
            peerConnection.createAnswer(answerDesc => {
                    peerConnection.setLocalDescription(answerDesc);
                },
                error => {
                    console.error("Failed to create answer: " + error);
                    window.StreamControl.control.dispatchEvent(new CustomEvent("onInformation",
                        { detail: "stream.disconnected" }));
                },
                sdpConstraints);
        }
    });