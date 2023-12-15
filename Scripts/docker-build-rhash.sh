#! /bin/sh
apk update
apk add gcc musl-dev

cd /app/RHash
wget -qO- https://github.com/rhash/RHash/archive/refs/tags/v1.4.4.tar.gz | tar -xz
cd RHash-1.4.4
./configure && make
cp librhash/librhash.so.1.4.4 ../librhash.so
