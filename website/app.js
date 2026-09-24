(() => {
  'use strict';
  const $ = selector => document.querySelector(selector);
  const video = $('#story-video');
  const filmCover = $('#film-cover-play');
  function resetFilmCover() { video.controls = false; filmCover.hidden = false; }
  resetFilmCover();
  video.addEventListener('play', () => { video.controls = true; filmCover.hidden = true; });
  video.addEventListener('ended', resetFilmCover);
  const tabs = [...document.querySelectorAll('[role="tab"]')];
  let locale = 'zh';
  let selectedTab = 0;
  let focusing = false;
  const descriptions = {
    zh: ['25 分钟，留给一件值得投入的事。', '5 分钟，起身走走，看看远方。', '15 分钟，慢慢放松，再重新出发。'],
    en: ['25 minutes for one thing worth your attention.', '5 minutes to stretch and look beyond the screen.', '15 minutes to unwind before you begin again.']
  };
  const metadata = {
    zh: ['朱果 Tommi — 少一点打扰，多一点投入。', '朱果，一颗住在你桌面上的番茄钟。透明悬浮、轻巧计时、番茄雨提醒。克制，但足够精致。'],
    en: ['Tommi — Less distraction. More in the moment.', 'A small, translucent Pomodoro timer for your desktop. Focus gently, and take a break with a little shower of tomatoes.']
  };
  function updateDemo() {
    $('#desktop-demo').classList.toggle('is-focusing', focusing);
    $('#demo-toggle').setAttribute('aria-pressed', String(focusing));
    $('#demo-toggle span').textContent = focusing
      ? (locale === 'zh' ? '专注模式示意 · 点击还原' : 'Focus mode preview · Reset')
      : (locale === 'zh' ? '体验一下：开始专注' : 'Try it: start focusing');
  }
  function chooseTab(index) {
    selectedTab = index;
    tabs.forEach((tab, i) => { tab.setAttribute('aria-selected', String(i === index)); tab.tabIndex = i === index ? 0 : -1; });
    $('.rhythm-time').replaceChildren(document.createTextNode(tabs[index].dataset.minutes.padStart(2, '0')));
    const seconds = document.createElement('span'); seconds.textContent = ':00'; $('.rhythm-time').append(seconds);
    $('#rhythm-panel').setAttribute('aria-labelledby', tabs[index].id);
    $('#rhythm-description').textContent = descriptions[locale][index];
  }
  function setLocale(next, persist = false) {
    if (!['zh', 'en'].includes(next)) return;
    locale = next;
    document.documentElement.lang = next === 'zh' ? 'zh-CN' : 'en';
    document.querySelectorAll('[data-zh]').forEach(el => { el.textContent = el.dataset[next]; });
    for (const attribute of ['alt', 'aria-label']) {
      document.querySelectorAll(`[data-zh-${attribute}]`).forEach(el => el.setAttribute(attribute, el.getAttribute(`data-${next}-${attribute}`)));
    }
    document.querySelectorAll('[data-locale]').forEach(button => button.setAttribute('aria-pressed', String(button.dataset.locale === next)));
    document.title = metadata[next][0];
    $('meta[name="description"]').content = metadata[next][1];
    const source = video.querySelector('source');
    const filmPath = `media/film-${next}.mp4`;
    if (source.getAttribute('src') !== filmPath) {
      video.pause(); source.src = filmPath; video.poster = `media/film-${next}.webp`; video.load(); resetFilmCover();
    }
    $('#film-direct').href = filmPath;
    chooseTab(selectedTab); updateDemo();
    if (persist) {
      try { localStorage.setItem('tomato-language', next); } catch { /* Storage is optional. */ }
      const url = new URL(location.href); url.searchParams.set('lang', next); history.replaceState(null, '', url);
    }
  }
  document.querySelectorAll('[data-locale]').forEach(button => button.addEventListener('click', () => setLocale(button.dataset.locale, true)));
  $('#demo-toggle').addEventListener('click', () => { focusing = !focusing; updateDemo(); });
  tabs.forEach((tab, index) => {
    tab.addEventListener('click', () => chooseTab(index));
    tab.addEventListener('keydown', event => {
      const next = { ArrowRight: (index + 1) % tabs.length, ArrowLeft: (index + tabs.length - 1) % tabs.length, Home: 0, End: tabs.length - 1 }[event.key];
      if (next !== undefined) { event.preventDefault(); chooseTab(next); tabs[next].focus(); }
    });
  });
  function playFilm() {
    video.scrollIntoView({ behavior: 'instant', block: 'center' });
    video.focus({ preventScroll: true });
    video.play().then(() => video.focus({ preventScroll: true })).catch(() => { video.controls = true; filmCover.hidden = true; video.focus({ preventScroll: true }); });
  }
  $('#watch-film').addEventListener('click', playFilm);
  filmCover.addEventListener('click', playFilm);
  document.addEventListener('visibilitychange', () => { if (document.hidden) video.pause(); });
  if ('IntersectionObserver' in window) new IntersectionObserver(entries => {
    if (!entries[0].isIntersecting) video.pause();
  }, { threshold: 0 }).observe(video);
  $('#year').textContent = new Date().getFullYear();
  const release = window.TOMATO_RELEASE;
  if (release) {
    $('#installer-link').href = release.installer; $('#portable-link').href = release.portable;
    $('#release-meta').textContent = `v${release.version} · Windows 10 / 11 · x64 · ${(release.installerBytes / 1048576).toFixed(1)} MB`;
  }
  let initial = new URLSearchParams(location.search).get('lang');
  if (!['zh', 'en'].includes(initial)) {
    try { initial = localStorage.getItem('tomato-language'); } catch { /* Default to Chinese. */ }
  }
  setLocale(['zh', 'en'].includes(initial) ? initial : 'zh');
})();
