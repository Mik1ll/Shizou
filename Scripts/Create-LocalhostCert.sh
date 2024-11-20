#!/usr/bin/env bash
openssl req -x509 -out shizou.crt -keyout shizou.key \
  -newkey rsa:2048 -nodes -sha256 \
  -subj '/CN=shizou' -extensions EXT -config <( \
   printf "[dn]\nCN=shizou\n[req]\ndistinguished_name = dn\n[EXT]\nsubjectAltName=DNS:localhost,IP:127.0.0.1,IP:::1\nkeyUsage=digitalSignature\nextendedKeyUsage=serverAuth")
