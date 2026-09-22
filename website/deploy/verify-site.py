#!/usr/bin/env python3
"""Read-only verification of the installed site through its loopback listener."""
import hashlib
import json
from pathlib import Path
import urllib.error
import urllib.request

root = Path('/srv/tomato-focus/current')
base = 'http://127.0.0.1:8080/'


def request(path, method='GET', headers=None):
    fields = {'Host': 'example.com', **(headers or {})}
    try:
        return urllib.request.urlopen(urllib.request.Request(base + path, method=method, headers=fields), timeout=10)
    except urllib.error.HTTPError as error:
        return error


verified = []
for line in (root / 'MANIFEST.sha256').read_text().splitlines():
    expected, name = line.split('  ', 1)
    with request(name) as response:
        assert response.status == 200, (name, response.status)
        assert hashlib.sha256(response.read()).hexdigest() == expected, name
        assert response.headers['X-Content-Type-Options'] == 'nosniff', name
        assert "frame-ancestors 'none'" in response.headers['Content-Security-Policy'], name
        if name.startswith('downloads/'):
            assert response.headers['Content-Disposition'] == 'attachment', name
    verified.append(name)
with request('') as response:
    assert response.read() == (root / 'index.html').read_bytes()
with request('healthz') as response:
    assert response.status == 200 and response.read() == b'ok\n'
with request('missing-file') as response:
    assert response.status == 404
with request('.git/config') as response:
    assert response.status == 403
with request('', method='POST') as response:
    assert response.status in (403, 405)
for path in ('deploy/production.conf', 'README.md', 'serve.cjs'):
    with request(path) as response:
        assert response.status == 404
installer = next(root.glob('downloads/*-Setup.exe')).relative_to(root).as_posix()
with request(installer, headers={'Range': 'bytes=0-31'}) as response:
    assert response.status == 206 and len(response.read()) == 32
print(json.dumps({'status': 'PASS', 'release': root.resolve().name, 'verified_files': verified,
                  'checks': ['file_sha256', 'security_headers', 'download_attachment', 'byte_ranges',
                             'healthz', 'no_private_files', 'dotfiles_denied', 'writes_denied']}, indent=2))
