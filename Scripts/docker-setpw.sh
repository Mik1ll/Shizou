#! /bin/sh
wget --no-check-certificate '--post-data="password"' '--header=Content-Type: application/json' 'https://localhost/api/Account/SetPassword'
