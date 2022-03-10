var UnoAppManifest = {

    splashScreenImage: "Assets/SplashScreen.png",
    splashScreenColor:
        window.matchMedia &&
            window.matchMedia("(prefers-color-scheme: dark)").matches
            ? "#212121"
            : "#FFFFFF",
    displayName: "Musicadio"

}
