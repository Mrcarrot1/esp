# esp: Quick and Easy LinuxⓇ Packages from Source
esp is a package manager designed to build packages from source. It is based on spm, an unreleased package manager designed to be intentionally terrible, but I realized that it was actually pretty cool, so here we are.

To build esp:
* Ensure that you have the .NET Core SDK >= 6.0 installed.
* Navigate to the repository's root directory
* `make`
* `sudo make install`
* To run, you will need `bash` and `sudo` installed.
* You may also need to install other utilities to build and install many packages. These may or may not be available from your distribution's package manager. Support for cross-distro dependency management is a planned feature.
