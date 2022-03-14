#Heavily WIP pkgbuild for esp
#Does not currently work
pkgname=('esp')
pkgver=0.1.0
pkgrel=1
arch=('any')
license=('GPL')
depends=('bash' 'sudo')
makedepends=('dotnet-sdk')
pkgdesc="Source-based package manager."
source=("$pkgname-$pkgver.tar.gz::https://github.com/Mrcarrot1/esp/archive/refs/tags/$pkgver.tar.gz")

build()
{
    make
}

package()
{
    cd $srcdir/$pkgname
}
