const fs = require('fs');
const path = require('path');
const electron = require('electron');
const { ipcRenderer } = electron;

window.ipcRenderer = ipcRenderer;


ipcRenderer.on('loadLogo-proxy', () => {
  var imgs = document.getElementsByTagName("img");

  for (var i = 0; i < imgs.length; i++) {
    imgs[i].src = path.join(__dirname, 'assets', 'Logo.svg');
  }
});

ipcRenderer.on('requestBlobUrl', (event, file) => {
  try {
    const data = fs.readFileSync(file.replace("file:///", ""))
    var blob = new Blob([new Uint8Array(data.buffer)]);

    var url = URL.createObjectURL(blob)
    ipcRenderer.send('getBlobUrl', url);
  } catch (err) {
    console.error(err)
  }
});