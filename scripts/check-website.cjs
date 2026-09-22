const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const { createRequire } = require('node:module');
const websiteRequire = createRequire(path.resolve(__dirname, '../website/package.json'));
const { chromium } = websiteRequire('playwright');
(async () => {
  fs.mkdirSync('artifacts', { recursive: true });
  const browser = await chromium.launch({ channel: 'chrome', headless: true });
  const page = await browser.newPage({ viewport: { width: 1440, height: 1080 }, deviceScaleFactor: 1 });
  const errors = [];
  page.on('pageerror', error => errors.push(error.message));
  page.on('response', response => { if (response.status() >= 400) errors.push(`${response.status()} ${response.url()}`); });
  const url = 'http://127.0.0.1:4173';
  try {
    await page.goto(url);
    await page.locator('.hero-product img').evaluate(img => img.decode());
    for (const element of await page.locator('.reveal').all()) await element.scrollIntoViewIfNeeded();
    await page.evaluate(() => scrollTo({ top: 0, behavior: 'instant' }));
    await page.waitForTimeout(800);
    await page.screenshot({ path: 'artifacts/website-desktop.png', fullPage: true });
    assert.equal(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth), true, 'desktop overflow');
    await page.locator('#tab-short').click();
    assert.match(await page.locator('.rhythm-time').innerText(), /05:00/);
    await page.locator('#tab-short').press('ArrowRight');
    assert.match(await page.locator('.rhythm-time').innerText(), /15:00/);
    await page.locator('#watch-film').click();
    assert.equal(await page.locator('#film-dialog').evaluate(d => d.open), true);
    await page.waitForTimeout(9500);
    assert.match(await page.locator('#film-chapter').innerText(), /03/);
    assert.ok(await page.locator('.film-particles img').count() > 0);
    await page.screenshot({ path: 'artifacts/website-film.png' });
    await page.waitForTimeout(9000);
    assert.match(await page.locator('#film-status').innerText(), /播放完毕/);
    await page.locator('#replay-film').click();
    await page.waitForTimeout(100);
    assert.match(await page.locator('#film-chapter').innerText(), /01/);
    await page.keyboard.press('Escape');
    assert.equal(await page.locator('#film-dialog').evaluate(d => d.open), false);
    assert.equal(await page.evaluate(() => document.body.style.overflow), '');
    for (const selector of ['#installer-link', '#portable-link', '#checksum-link']) {
      const href = await page.locator(selector).getAttribute('href');
      const response = await page.request.get(`${url}/${href}`);
      assert.equal(response.status(), 200);
      assert.ok((await response.body()).length > 50);
    }
    for (const width of [390, 320, 768]) {
      await page.setViewportSize({ width, height: 844 });
      await page.goto(url);
      assert.equal(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth), true, `overflow at ${width}px`);
      if (width === 390) {
        for (const element of await page.locator('.reveal').all()) await element.scrollIntoViewIfNeeded();
        await page.evaluate(() => scrollTo({ top: 0, behavior: 'instant' }));
        await page.waitForTimeout(800);
        await page.screenshot({ path: 'artifacts/website-mobile.png', fullPage: true });
      }
    }
    await page.emulateMedia({ reducedMotion: 'reduce' });
    await page.goto(url);
    assert.equal(await page.locator('.hero-product').evaluate(el => getComputedStyle(el).animationName), 'none');
    await page.setViewportSize({ width: 1440, height: 1080 });
    await page.evaluate(() => document.documentElement.style.fontSize = '200%');
    assert.equal(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth), true, 'text zoom overflow');
    assert.deepEqual(errors, []);
    console.log('PASS responsive layouts, animation chapters/replay/Escape, keyboard tabs, real downloads, reduced motion, text enlargement, no browser errors');
  } finally { await browser.close(); }
})().catch(error => { console.error(error); process.exitCode = 1; });
