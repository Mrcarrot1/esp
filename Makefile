all:
	dotnet publish -r linux-x64 -p:PublishSingleFile=true --self-contained=true -c Release
install:
	cp bin/Release/net6.0/linux-x64/publish/esp /usr/bin
	chmod +x /usr/bin/esp
	cp esp-update /usr/bin
	chmod +x /usr/bin/esp-update
#Install esp in a temporary location- meant to be used from within esp itself
install-esp:
	cp bin/Release/net6.0/linux-x64/publish/esp /usr/bin/esp_temp
	cp esp-update /usr/bin
	chmod +x /usr/bin/esp-update