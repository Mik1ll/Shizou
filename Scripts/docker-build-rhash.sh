#! /bin/sh
wget --no-check-certificate '--post-data="password"' '--header=Content-Type: application/json' 'https://localhost/api/Account/SetPassword'
apk update
apk add gcc musl-dev


cd /app/RHash
wget -qO- https://github.com/rhash/RHash/archive/refs/tags/v1.4.4.tar.gz | tar -xz
cd RHash-1.4.4
./configure && make
cp librhash/librhash.so.1.4.4 ../librhash.so
