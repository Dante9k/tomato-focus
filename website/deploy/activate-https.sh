#!/bin/bash
# Run only after domain resolution, ingress and launch prerequisites are confirmed.
# Requires an already-issued publicly trusted certificate for example.com.
set -euo pipefail
test "$(id -u)" = 0
command -v curl >/dev/null
certificate=/etc/letsencrypt/live/example.com/fullchain.pem
test -s "$certificate"
test -s /etc/letsencrypt/live/example.com/privkey.pem
openssl x509 -in "$certificate" -noout -checkhost example.com
openssl x509 -in "$certificate" -noout -checkend 604800
link=/etc/nginx/sites-available/tomato-focus
prior=$(readlink "$link")
case "$prior" in
    /etc/nginx/tomato-focus/staging.conf|/etc/nginx/tomato-focus/production.conf) ;;
    *) printf '%s\n' 'Unexpected site configuration; no changes made.' >&2; exit 1 ;;
esac
temporary="${link}.next.$$"
restore() {
    trap - ERR
    test ! -L "$temporary" || unlink "$temporary"
    ln -s "$prior" "$temporary"
    mv -Tf "$temporary" "$link"
    nginx -t && systemctl reload nginx.service
    printf '%s\n' 'HTTPS activation failed; the prior configuration was restored.' >&2
    exit 1
}
trap restore ERR
ln -s /etc/nginx/tomato-focus/production.conf "$temporary"
mv -Tf "$temporary" "$link"
nginx -t
systemctl reload nginx.service
curl --fail --silent --show-error --noproxy '*' --resolve example.com:443:127.0.0.1 https://example.com/healthz
install -m 0750 /usr/local/lib/tomato-focus/renew-hook.sh /etc/letsencrypt/renewal-hooks/deploy/tomato-focus
systemctl enable --now certbot.timer
trap - ERR
printf '%s\n' 'HTTPS enabled. Verify the public domain and run certbot renew --dry-run.'
