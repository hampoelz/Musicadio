if (!window.AudioEngine.audio) throw new Error("AudioEngine is not initialized");
var DragAndDrop = document.getElementById("$DragAndDropArea");

window.DragAndDrop = {};
window.DragAndDrop.playFile = false;

DragAndDrop.addEventListener("dragover",
    event => {
        event.stopPropagation();
        event.preventDefault();

        if (event.dataTransfer.types == "Files")
            DragAndDrop.dispatchEvent(new CustomEvent("onFileDragOver", { detail: event.dataTransfer.items.length }));
    });

DragAndDrop.addEventListener("dragleave",
    event => {
        event.stopPropagation();
        event.preventDefault();

        DragAndDrop.dispatchEvent(new CustomEvent("onFileDragLeave"));
    });

DragAndDrop.addEventListener("drop",
    event => {
        event.stopPropagation();
        event.preventDefault();

        if (event.dataTransfer.types != "Files") return;

        window.AudioEngine.handleFiles(event.dataTransfer.files, !window.DragAndDrop.playFile);

        DragAndDrop.dispatchEvent(new CustomEvent("onFileDrop"));
    });

document.getElementById("$PlayFileArea").addEventListener("dragenter",
    () => DragAndDrop.dispatchEvent(new CustomEvent("onEnterPlayFileArea")));
document.getElementById("$AddFileArea")
    .addEventListener("dragenter", () => DragAndDrop.dispatchEvent(new CustomEvent("onEnterAddFileArea")));