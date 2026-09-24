# Public website media

These optimized exports are checked in so a clean website build does not depend on local marketing renders. Only the seven explicitly named image/video files enter the public archive; this document does not.

- `tomato.webp`: project asset `assets/tomato-cute.png`, resized to 240 px.
- `timer-edit.webp`: real WPF control render `marketing/xiaohongshu/assets-v2/edit-25.png`, resized to 600 px.
- `timer-focus.webp`: real WPF control render `marketing/xiaohongshu/assets-v2/focus-1499.png`, resized to 320 px; original alpha preserved.
- `film-zh.webp`, `film-en.webp`: V2 story posters resized to 720 × 1280.
- `film-zh.mp4`: `dist/promo-zhuguo-v2/zhuguo-story-v2-1080x1920.mp4`.
- `film-en.mp4`: `dist/promo-zhuguo-v2-en/tomato-focus-story-en-1080x1920.mp4`.

Both films retain their full 35-second edit and original AAC audio. Web exports use FFmpeg `-vf scale=720:1280 -c:v libx264 -preset slow -crf 22 -pix_fmt yuv420p -c:a copy -movflags +faststart`. WebP exports use Pillow, Lanczos resizing, quality 91, method 6. Keep the two locales aligned when replacing a film.

The current website editions have been re-rendered with the English brand **Tommi** using `marketing/xiaohongshu/render_website_films.py zh|en`, based on the original story composition scripts. The website posters come from those updated frames. That renderer preserves and verifies the original AAC bitstream and fully decodes each film before replacing a public file. The older 1080p exports above are historical source editions and are not the current website branding.
