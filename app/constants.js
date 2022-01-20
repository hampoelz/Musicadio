const Constants = {
  settings: {
    appName: 'RH Music',
    appUrl: 'https://music.hampoelz.net', // without trailing slash!
    //appUrl: 'http://localhost:5555', // for testing
    enableDevMenu: false, // enable for dev-builds only!
    themeColor: '#0D47A1',
    userAgentPostfix: '[RH Music PWA Wrapper]'
  },
  mainWindow: {
    minwidth: 400,
    minheight: 860,
    width: 1320,
    height: 860
  }
};

module.exports = Constants;
