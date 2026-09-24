#!/usr/bin/env python3
"""Publish a verified static bundle, with an atomic symlink switch and rollback.

Usage: deploy-release.py ARCHIVE RELEASE_ID EXPECTED_ARCHIVE_SHA256
Only /srv/tomato-focus and the site's operations log are written.
"""
import datetime
import hashlib
import json
import os
from pathlib import Path, PurePosixPath
import re
import stat
import sys
import urllib.error
import urllib.request
import uuid
import zipfile

ROOT = Path('/srv/tomato-focus')
LOG = Path('/var/lib/tomato-focus/operations/releases.jsonl')


def switch_link(name, target):
    temporary = ROOT / ('.' + name + '-' + uuid.uuid4().hex)
    temporary.symlink_to(target)
    os.replace(temporary, ROOT / name)


def main():
    archive, release, expected_hash = sys.argv[1:]
    if os.geteuid() != 0:
        raise RuntimeError('Run as root; the serving worker must never write releases.')
    # A root shell may use umask 027; static directories must remain traversable.
    os.umask(0o022)
    if not re.fullmatch(r'[a-zA-Z0-9][a-zA-Z0-9._-]{0,79}', release):
        raise ValueError('Invalid release ID')
    if not re.fullmatch(r'[a-fA-F0-9]{64}', expected_hash):
        raise ValueError('Invalid expected archive checksum')
    archive = Path(archive)
    if hashlib.sha256(archive.read_bytes()).hexdigest() != expected_hash.lower():
        raise ValueError('Archive checksum mismatch')
    if ROOT.is_symlink() or (ROOT / 'releases').is_symlink():
        raise ValueError('Managed directories must not be symlinks')
    ROOT.mkdir(mode=0o755, exist_ok=True)
    ROOT.chmod(0o755)
    (ROOT / 'releases').mkdir(mode=0o755, exist_ok=True)
    (ROOT / 'releases').chmod(0o755)
    target = ROOT / 'releases' / release
    current = ROOT / 'current'
    if target.exists():
        raise FileExistsError('Immutable release already exists; choose a new release ID')
    if current.exists() and not current.is_symlink():
        raise ValueError('Refusing to replace a non-symlink current path')
    previous = current.resolve(strict=True) if current.is_symlink() else None
    if previous is not None and previous.parent != ROOT / 'releases':
        raise ValueError('Existing release is outside the managed releases directory')
    with zipfile.ZipFile(archive) as package:
        names = package.namelist()
        if len(names) != len(set(names)) or len(names) > 30:
            raise ValueError('Duplicate or excessive archive entries')
        if sum(entry.file_size for entry in package.infolist()) > 50 * 1024 * 1024:
            raise ValueError('Archive exceeds the expected static-site size')
        for entry in package.infolist():
            name = entry.filename
            path = PurePosixPath(name)
            if path.is_absolute() or '..' in path.parts or '\\' in name or stat.S_ISLNK(entry.external_attr >> 16):
                raise ValueError('Unsafe archive path')
            allowed = name in ('index.html', 'style.css', 'app.js', 'release.js', 'MANIFEST.sha256', 'assets/tomato.png', 'assets/favicon.ico', 'downloads/SHA256SUMS.txt')
            allowed = allowed or bool(re.fullmatch(r'downloads/TomatoFocus-\d+\.\d+\.\d+-(?:Setup\.exe|win-x64\.zip)', name))
            allowed = allowed or name in ('media/tomato.webp', 'media/timer-edit.webp', 'media/timer-focus.webp', 'media/film-zh.webp', 'media/film-en.webp', 'media/film-zh.mp4', 'media/film-en.mp4')
            if not allowed:
                raise ValueError('Unexpected public file: ' + name)
        manifest = {}
        for line in package.read('MANIFEST.sha256').decode('ascii').splitlines():
            digest, name = line.split('  ', 1)
            if name in manifest or not re.fullmatch(r'[a-f0-9]{64}', digest):
                raise ValueError('Invalid release manifest')
            manifest[name] = digest
        if set(manifest) != set(names) - {'MANIFEST.sha256'}:
            raise ValueError('Manifest and archive differ')
        for name, digest in manifest.items():
            if hashlib.sha256(package.read(name)).hexdigest() != digest:
                raise ValueError('File checksum mismatch: ' + name)
        # Validation completes before any release data or links are changed.
        target.mkdir(mode=0o755)
        for name in names:
            destination = target / name
            destination.parent.mkdir(mode=0o755, parents=True, exist_ok=True)
            destination.write_bytes(package.read(name))
            destination.chmod(0o644)
    switch_link('current', target)
    try:
        # A running local server serves the exact new bytes after an atomic link swap.
        request = urllib.request.Request('http://127.0.0.1:8080/', headers={'Host': 'example.com'})
        try:
            with urllib.request.urlopen(request, timeout=5) as response:
                if hashlib.sha256(response.read()).hexdigest() != manifest['index.html']:
                    raise RuntimeError('Homepage verification failed')
        except urllib.error.URLError as error:
            if previous is not None or not isinstance(error.reason, ConnectionRefusedError):
                raise
            # First installation is verified after Nginx has been configured and started.
    except Exception:
        if previous is not None:
            switch_link('current', previous)
        else:
            current.unlink()
        raise
    if previous is not None:
        switch_link('previous', previous)
    record = {'time_utc': datetime.datetime.now(datetime.timezone.utc).isoformat(), 'release': release,
              'archive_sha256': expected_hash.lower(), 'previous': str(previous) if previous else None}
    with LOG.open('a') as log:
        log.write(json.dumps(record) + '\n')
    print(json.dumps(record))


if __name__ == '__main__':
    main()
