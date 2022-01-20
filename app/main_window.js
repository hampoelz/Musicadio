const path = require('path');
const electron = require('electron');
const { BrowserWindow, shell, session } = electron;
const c = require('./constants');

class MainWindow extends BrowserWindow {
  constructor(mainUrl, iconPath, show) {
    const options = {
      minWidth: c.mainWindow.minwidth,
      minHeight: c.mainWindow.minheight,
      width: c.mainWindow.width,
      height: c.mainWindow.height,
      title: c.settings.appName,
      backgroundColor: c.settings.themeColor,
      icon: iconPath,
      devTools: c.settings.enableDevMenu,
      frame: false,
      show: (show === false ? false : true),
      webPreferences: {
        nodeIntegration: false,
        preload: path.resolve(__dirname, '../src', 'ipcPreloader.js'),
      },
    };

    // initalize BrowserWindow
    super(options);
    this.mainUrl = mainUrl;

    //  -- Event listeners: --
    // Open new windows in default Browser
    this.webContents.on('new-window', function (e, url) {
      e.preventDefault();
      shell.openExternal(url);
    });

    this.loadHome();

    if (c.settings.enableDevMenu) this.webContents.openDevTools();
  }

  // add custom user agent postifx (e.g. for google analytics)
  loadCustomUrl(url) {
    this.loadURL(url, {
      userAgent: (session.defaultSession.getUserAgent()
        + ' ' + c.settings.userAgentPostfix),
    });
  }

  loadRelativeUrl(url) {
    this.loadCustomUrl(this.mainUrl + url);
  }

  loadHome() {
    this.loadRelativeUrl('');
  }

}

module.exports = MainWindow;
