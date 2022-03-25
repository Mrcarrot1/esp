# esp: Quick and Easy LinuxⓇ Packages from Source
esp is a package manager designed to build packages from source. It is based on spm, an unreleased package manager I designed to be intentionally terrible, but I realized that it was actually pretty cool, so here we are.

To build esp:
* Ensure that you have the .NET Core 6.0 SDK installed.
* Navigate to the repository's root directory
* `make`
* `sudo make install`
* To run, you will need `bash` and `sudo` installed.
* It is also recommended, once you have a functioning install of esp, to run `esp install esp-latest.esp`. This will allow esp to install updates to itself.
* You may also need to install other utilities to build and install many packages. These may or may not be available from your distribution's package manager. Support for cross-distro dependency management is a planned feature.
* If you have an existing esp installation, you should be able to update it with esp itself. If not(your build of esp is broken in some way), performing the above installation steps may help.

Instructions for creating packages and other documentation may be found on the [wiki](https://github.com/Mrcarrot1/esp/wiki).

This branch is the development repository for esp and the source of the rolling-beta version of esp. Point releases are made from the stable branch.