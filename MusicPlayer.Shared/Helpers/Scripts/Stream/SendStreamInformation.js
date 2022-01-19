if (!window.StreamControl.control) throw new Error("StreamInstance is not initialized");

Object.keys(window.StreamControl.peers).forEach(from => {
    var channel = window.StreamControl.peers[from].dataChannel;

    if (channel.readyState === "open") {
        console.log("<== Send stream informations to client (" +
            (Object.keys(window.StreamControl.peers).indexOf(from) + 1) +
            ")");
        channel.send("$Information");
    }
});