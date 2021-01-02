openssl req -x509 -newkey rsa:4096  -subj "/CN=sirsonic.com" -keyout monitorbuddy_key.pem -out monitorbuddy_cert.pem -days 365 -nodes -sha256
openssl pkcs12 -name "CN=sirsonic.com" -export -in monitorbuddy_cert.pem -inkey monitorbuddy_key.pem -out monitorbuddy.pfx
