const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const { createRequire } = require('node:module');
const { chromium } = createRequire(path.resolve(__dirname, '../website/package.json'))('playwright');
const output = path.resolve(__dirname, '../artifacts');
const url = process.env.WEBSITE_URL || 'http://127.0.0.1:4173';
(async () => {
  fs.mkdirSync(output, { recursive: true });
  const browser = await chromium.launch({ channel: 'chrome', headless: true });
  const page = await browser.newPage({ viewport: { width: 1440, height: 1080 } });
  const errors = [], filmRequests = [];
  page.on('pageerror', error => errors.push(error.message));
  page.on('response', response => { if (response.status() >= 400) errors.push(`${response.status()} ${response.url()}`); });
  page.on('request', request => { if (request.url().endsWith('.mp4')) filmRequests.push(request.url()); });
  const noOverflow = async label => assert.equal(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth), true, label);
  try {
    await page.goto(url);
    await page.locator('.widget-edit').evaluate(img => img.decode());
    assert.equal(await page.locator('html').getAttribute('lang'), 'zh-CN');
    assert.equal(filmRequests.length, 0, 'video must not load on arrival');
    await page.screenshot({ path: `${output}/website-desktop-zh.png`, fullPage: true });
    await page.screenshot({ path: `${output}/website-hero-zh.png` });
    await page.locator('#demo-toggle').click();
    assert.equal(await page.locator('#demo-toggle').getAttribute('aria-pressed'), 'true');
    await page.locator('#demo-toggle').click();
    await page.locator('#tab-short').click();
    assert.equal(await page.locator('.rhythm-time').innerText(), '05:00');
    await page.locator('#tab-short').press('ArrowRight');
    assert.equal(await page.locator('.rhythm-time').innerText(), '15:00');
    await page.locator('#tab-long').press('Home');
    assert.equal(await page.locator('#tab-focus').getAttribute('aria-selected'), 'true');
    await page.locator('.faq-list summary').first().click();
    assert.equal(await page.locator('.faq-list details').first().getAttribute('open'), '');
    await page.locator('.faq-list summary').first().press('Enter');

    for (const lang of ['zh', 'en']) {
      await page.locator(`[data-locale="${lang}"]`).click();
      assert.equal(await page.locator('html').getAttribute('lang'), lang === 'zh' ? 'zh-CN' : 'en');
      assert.equal(await page.locator('#story-video source').getAttribute('src'), `media/film-${lang}.mp4`);
      assert.equal(await page.locator('#story-video').getAttribute('poster'), `media/film-${lang}.webp`);
      assert.equal(await page.locator('#film-direct').getAttribute('href'), `media/film-${lang}.mp4`);
      const badTranslations = await page.evaluate(locale => [...document.querySelectorAll('[data-zh]')].filter(el => !el.closest('#demo-toggle') && el.textContent !== el.dataset[locale]).map(el => el.outerHTML), lang);
      assert.deepEqual(badTranslations, []);
      assert.equal(await page.locator('#story-video').evaluate(v => v.paused), true);
      assert.ok(await page.locator('#film-cover-play').isVisible());
      await page.locator('#film-cover-play').click();
      await page.waitForFunction(() => { const v = document.querySelector('video'); return v.currentTime > .2 && !v.paused; });
      assert.ok(Math.abs(await page.locator('video').evaluate(v => v.duration) - 35) < .15);
      await page.locator('video').evaluate(v => { v.currentTime = 19; });
      await page.waitForFunction(() => !document.querySelector('video').seeking && document.querySelector('video').currentTime >= 19);
      await page.locator('video').evaluate(v => v.pause());
      assert.equal(await page.locator('video').evaluate(v => v.error), null);
      await page.locator('#film').screenshot({ path: `${output}/website-film-${lang}.png` });
      await page.locator('video').evaluate(v => { v.currentTime = 34.5; return v.play(); });
      await page.waitForFunction(() => document.querySelector('video').ended);
      await page.locator('#watch-film').click();
      await page.waitForFunction(() => document.querySelector('video').currentTime < 5 && !document.querySelector('video').paused);
      await page.locator('#hero-title').scrollIntoViewIfNeeded();
      await page.waitForFunction(() => document.querySelector('video').paused);
    }
    assert.match(await page.title(), /Less distraction/);
    assert.match(await page.locator('meta[name=description]').getAttribute('content'), /your desktop/);
    await page.goto(url);
    assert.equal(await page.locator('html').getAttribute('lang'), 'en', 'saved language survives navigation');
    await page.screenshot({ path: `${output}/website-desktop-en.png`, fullPage: true });
    await page.screenshot({ path: `${output}/website-hero-en.png` });
    await page.goto(`${url}/?lang=zh`);
    assert.equal(await page.locator('html').getAttribute('lang'), 'zh-CN', 'shared URL takes precedence');
    for (const selector of ['#installer-link', '#portable-link', '#checksum-link']) {
      const href = await page.locator(selector).getAttribute('href');
      const response = await page.request.get(new URL(href, url).href);
      assert.equal(response.status(), 200); assert.ok((await response.body()).length > 50);
    }
    for (const lang of ['zh', 'en']) {
      const film = `${url}/media/film-${lang}.mp4`;
      const head = await page.request.head(film);
      assert.equal(head.headers()['content-type'], 'video/mp4');
      for (const range of ['bytes=0-31', 'bytes=-32']) {
        const partial = await page.request.get(film, { headers: { Range: range } });
        assert.equal(partial.status(), 206); assert.equal((await partial.body()).length, 32);
      }
      assert.equal((await page.request.get(film, { headers: { Range: 'bytes=999999999-' } })).status(), 416);
      for (const width of [320, 390, 768, 1440]) {
        await page.setViewportSize({ width, height: width < 800 ? 844 : 1080 });
        await page.goto(`${url}/?lang=${lang}`);
        await noOverflow(`${lang} overflow at ${width}px`);
        if (width === 390) {
          await page.screenshot({ path: `${output}/website-mobile-${lang}.png`, fullPage: true });
          await page.screenshot({ path: `${output}/website-mobile-hero-${lang}.png` });
        }
      }
    }
    await page.emulateMedia({ reducedMotion: 'reduce' });
    assert.equal(await page.locator('.desktop-widget').evaluate(el => getComputedStyle(el).transitionDuration), '0s');
    await page.evaluate(() => document.documentElement.style.fontSize = '200%');
    await noOverflow('200% text enlargement');
    const noJs = await browser.newPage({ javaScriptEnabled: false });
    await noJs.goto(url);
    assert.ok(await noJs.locator('#installer-link').isVisible());
    assert.equal(await noJs.locator('video source').getAttribute('src'), 'media/film-zh.mp4');
    await noJs.close();
    for (const privatePath of ['/serve.cjs', '/deploy/security.conf', '/media/../README.md']) assert.equal((await page.request.get(url + privatePath)).status(), 404);
    assert.deepEqual(errors, []);
    console.log('PASS bilingual content/persistence, desktop demo, keyboard tabs, FAQ, both films playback/seek/replay/pause, range delivery, downloads, responsive layouts, reduced motion, text zoom, no-JS fallback and browser errors');
  } finally { await browser.close(); }
})().catch(error => { console.error(error); process.exitCode = 1; });
