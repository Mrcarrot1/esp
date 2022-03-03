# esp: The World's Worst Linux Package Manager
esp is a poorly-designed package manager designed for AggravationOS. It is written in C# and has more dependencies than any package manager should.

To build esp:
* Ensure that you have the .NET Core SDK >= 6.0 installed.
* Navigate to the repository's root directory
* `make`
* `sudo make install`
* To run, you will need Microsoft Powershell Core accessible in the path as `pwsh` when run as root.
* Please do not use esp on any distro other than AggravationOS. While it may work, it has a high chance of bricking the entire system.
