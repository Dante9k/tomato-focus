// Local preview only; deploy the allowlisted website ZIP to the production web server.
const http = require('node:http');
const fs = require('node:fs');
const path = require('node:path');
const root = __dirname;
const mime = { '.html': 'text/html; charset=utf-8', '.css': 'text/css; charset=utf-8', '.js': 'text/javascript; charset=utf-8', '.png': 'image/png', '.webp': 'image/webp', '.mp4': 'video/mp4', '.ico': 'image/x-icon', '.zip': 'application/zip', '.exe': 'application/octet-stream', '.txt': 'text/plain; charset=utf-8' };
const allowed = /^(index\.html|style\.css|app\.js|release\.js|assets\/(?:tomato\.png|favicon\.ico)|media\/(?:tomato\.webp|timer-(?:edit|focus)\.webp|film-(?:zh|en)\.(?:webp|mp4))|downloads\/(?:SHA256SUMS\.txt|Tommi-\d+\.\d+\.\d+-(?:Setup\.exe|win-x64\.zip)))$/;
http.createServer((req, res) => {
  let relative;
  try { relative = decodeURIComponent(new URL(req.url, 'http://localhost').pathname).slice(1) || 'index.html'; } catch { res.writeHead(400).end(); return; }
  if (!['GET', 'HEAD'].includes(req.method)) { res.writeHead(405, { Allow: 'GET, HEAD' }).end(); return; }
  if (!allowed.test(relative)) { res.writeHead(404).end('Not found'); return; }
  const file = path.join(root, relative);
  fs.stat(file, (error, stat) => {
    if (error || !stat.isFile()) { res.writeHead(404).end('Not found'); return; }
    const headers = { 'Content-Type': mime[path.extname(file)] || 'application/octet-stream', 'Cache-Control': 'no-store', 'X-Content-Type-Options': 'nosniff', 'Accept-Ranges': 'bytes' };
    let start = 0, end = stat.size - 1, status = 200;
    if (req.headers.range && req.method === 'GET') {
      const match = /^bytes=(\d*)-(\d*)$/.exec(req.headers.range);
      if (match && (match[1] || match[2])) {
        start = match[1] ? Number(match[1]) : Math.max(0, stat.size - Number(match[2]));
        end = match[1] && match[2] ? Math.min(Number(match[2]), stat.size - 1) : stat.size - 1;
      } else start = stat.size;
      if (!Number.isSafeInteger(start) || !Number.isSafeInteger(end) || start >= stat.size || start > end) {
        res.writeHead(416, { ...headers, 'Content-Range': `bytes */${stat.size}` }).end(); return;
      }
      status = 206; headers['Content-Range'] = `bytes ${start}-${end}/${stat.size}`;
    }
    headers['Content-Length'] = Math.max(0, end - start + 1);
    res.writeHead(status, headers);
    if (req.method === 'HEAD' || !stat.size) res.end(); else fs.createReadStream(file, { start, end }).on('error', () => res.destroy()).pipe(res);
  });
}).listen(Number(process.env.PORT) || 4173, '127.0.0.1', () => console.log(`朱果预览 http://127.0.0.1:${Number(process.env.PORT) || 4173}`));
