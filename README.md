# esp: The World's Worst Linux Package Manager
esp is a poorly-designed package manager designed for AggravationOS. It is written in C# and has more dependencies than any package manager should.

To build esp:
* Ensure that you have the .NET Core SDK >= 6.0 installed.
* Navigate to the repository's root directory
* `make`
* `sudo make install`
* To run, you will need `bash` and `sudo` installed.
* You may also need to install other utilities to build and install many packages. These may or may not be available from your distribution's package manager. Support for cross-distro dependency management is a planned feature.
