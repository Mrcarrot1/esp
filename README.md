# esp: Quick and Easy LinuxⓇ Packages from Source
esp is a package manager designed to build packages from source. It is based on spm, an unreleased package manager I designed to be intentionally terrible, but I realized that it was actually pretty cool, so here we are.

To build esp:
* Ensure that you have the .NET Core SDK >= 6.0 installed.
* Navigate to the repository's root directory
* `make`
* `sudo make install`
* To run, you will need `bash` and `sudo` installed.
* You may also need to install other utilities to build and install many packages. These may or may not be available from your distribution's package manager. Support for cross-distro dependency management is a planned feature.
* If you have an existing esp installation, you should be able to update it with esp itself.

# Package Format
The esp package format is fairly simple and is based on [KON](https://github.com/Mrcarrot1/KarrotObjectNotation).

An example esp package(taken from esp-latest.esp):
```
//esp 0.1.0
//Information for esp package manager.
ESP_PACKAGE
{
    name = esp
    description = esp package manager.
    type = Git //Can be Git, Tarball, or Other
    version = 0.1.0 //Can be rolling, rolling-<prerelease label>, or a semantic version string
    cloneURL = ssh://git@github.com:Mrcarrot1/esp.git //For a git package, the link to the repository to clone.
    updateURL = https://raw.githubusercontent.com/Mrcarrot1/esp/main/esp-latest.esp //The location from which an updated version of this file can be downloaded. 
    INSTALL_COMMANDS //This is a list of bash commands used to install the package. Each command is run with a working directory of ~/.cache/esp/<package> and does not run in the same session as previously run commands.
    [
        make -j $THREADS
        sudo make install-esp
        echo -e 'An updated version of esp has been installed to a temporary location.\nPlease run esp-update as root to install it.'
    ]
    UNINSTALL_COMMANDS
    [
        esp no-yes Are you sure? Continuing will remove esp. //esp built-in command interpreted by the application-used for integration with install process
        echo -e 'esp is not able to uninstall itself.\n Run esp-uninstall as root to remove it.'
    ]
}
```
The beginning lines should contain comments that let a human reader know what the file contains. The first line should have the package name and version, while the second should state that the file is an esp package.
