#!/bin/sh
set -eu
# Reload only for this certificate; other future services are untouched.
if [ "${RENEWED_LINEAGE:-}" = /etc/letsencrypt/live/example.com ]; then
    /usr/sbin/nginx -t
    /usr/bin/systemctl reload nginx.service
fi
