const path = require('path');
const fs = require('fs');
const electron = require('electron');
const MainWindow = require('./app/main_window');
const c = require('./app/constants');

const { app, ipcMain } = electron;

let mainWindow;
let offlineWindow;
let appIconPath;

const singleInstanceLock = app.requestSingleInstanceLock();

if (!singleInstanceLock) app.quit();
else {
  app.on('second-instance', (event, args) => {
    var window = (offlineWindow == null ? mainWindow : offlineWindow);
    if (window) {
      if (window.isMinimized()) window.restore();
      window.focus();
    }

    sendArgs(args);
  })

  app.on('ready', () => {
    appIconPath = path.join(__dirname, 'src', 'assets', 'Logo.ico');

    loadAppWindows();
  });
}

function loadAppWindows() {
  let appPath = c.settings.appUrl;

  mainWindow = new MainWindow(appPath, appIconPath);
  mainWindow.setMenu(null);

  mainWindow.on('closed', () => app.quit());

  mainWindow.webContents.on('did-fail-load', () => {
    offlineWindow = new MainWindow(`file://${__dirname}/src/offline.html`, appIconPath, true);
    offlineWindow.setResizable(false);

    mainWindow.hide();
  });

}

function sendArgs(args) {
  args.forEach(file => {
    if (fs.existsSync(file) && file != process.execPath) mainWindow.webContents.send('openFile', file);
  });
}

ipcMain.on('loadLogo', () => offlineWindow.webContents.send('loadLogo-proxy'));

ipcMain.on('engineLoaded', () => sendArgs(process.argv));

ipcMain.on('requestBlobUrl', (event, file) => {
  mainWindow.webContents.send('requestBlobUrl', file);
});

ipcMain.on('getBlobUrl', (event, file) => {
  mainWindow.webContents.send('getBlobUrl', file);
});

ipcMain.on('app:refresh', () => {
  // hide offline window if applicable
  if (offlineWindow && offlineWindow.isVisible()) offlineWindow.hide();

  offlineWindow = null;

  if (mainWindow) {
    // mainWindow is hidden, refresh and show it directly
    mainWindow.loadHome();
    mainWindow.show();
  } else {
    // instantiate mainWindow additionally
    loadAppWindows();
  }
});

// register window buttons click event
ipcMain.on('app:minimize', () => (offlineWindow == null ? mainWindow : offlineWindow).minimize());
ipcMain.on('app:min-max', () => {
  var window = (offlineWindow == null ? mainWindow : offlineWindow);
  if (window.isMaximized()) window.unmaximize();
  else window.maximize();
});
ipcMain.on('app:quit', () => app.exit(0));