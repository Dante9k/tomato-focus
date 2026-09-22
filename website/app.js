(() => {
  'use strict';
  const reducedMotion = matchMedia('(prefers-reduced-motion: reduce)');
  if ('IntersectionObserver' in window && !reducedMotion.matches) {
    document.documentElement.classList.add('js-motion');
    const observer = new IntersectionObserver(entries => entries.forEach(entry => {
      if (entry.isIntersecting) { entry.target.classList.add('visible'); observer.unobserve(entry.target); }
    }), { threshold: 0.12 });
    document.querySelectorAll('.reveal').forEach(el => {
      // Keep the primary download action visible even before an observer callback.
      if (el.classList.contains('download-inner')) el.classList.add('visible');
      else observer.observe(el);
    });
  }
  document.querySelector('#year').textContent = new Date().getFullYear();
  const tabs = [...document.querySelectorAll('[role="tab"]')];
  const descriptions = ['关掉干扰，只做一件事。', '起身走走，让眼睛看看远方。', '放松一下，为下一段专注充电。'];
  const chooseTab = tab => {
    tabs.forEach(item => { item.setAttribute('aria-selected', String(item === tab)); item.tabIndex = item === tab ? 0 : -1; });
    document.querySelector('.rhythm-time').innerHTML = `${tab.dataset.minutes.padStart(2, '0')}<span>:00</span>`;
    document.querySelector('#rhythm-description').textContent = descriptions[tabs.indexOf(tab)];
    document.querySelector('#rhythm-panel').setAttribute('aria-labelledby', tab.id);
  };
  tabs.forEach((tab, index) => {
    tab.addEventListener('click', () => chooseTab(tab));
    tab.addEventListener('keydown', event => {
      let next;
      if (event.key === 'ArrowRight') next = (index + 1) % tabs.length;
      if (event.key === 'ArrowLeft') next = (index + tabs.length - 1) % tabs.length;
      if (event.key === 'Home') next = 0;
      if (event.key === 'End') next = tabs.length - 1;
      if (next !== undefined) { event.preventDefault(); tabs[next].focus(); chooseTab(tabs[next]); }
    });
  });
  const dialog = document.querySelector('#film-dialog');
  const stage = dialog.querySelector('.film-stage');
  let frame = 0;
  let startedAt = 0;
  let previousChapter = -1;
  const chapters = [
    ['把世界的喧嚣，放在一边。', '现在，是你的时间。', '01 / 进入专注'],
    ['一个小小的开始，也能离目标更近。', '让热爱，慢慢发生。', '02 / 沉浸其中'],
    ['认真投入过，也值得好好放松。', '休息一下，让番茄飞。', '03 / 享受休息'],
    ['一颗番茄，一段完整的专注。', '朱果，陪你做好眼前的事。', '04 / 找到自己的节奏']
  ];
  function setChapter(index) {
    if (previousChapter === index) return;
    previousChapter = index;
    document.querySelector('#film-kicker').textContent = chapters[index][0];
    document.querySelector('#film-title').textContent = chapters[index][1];
    document.querySelector('#film-chapter').textContent = chapters[index][2];
    stage.classList.toggle('rest', index === 2);
    stage.classList.toggle('finished', index === 3);
    if (index === 2 && !reducedMotion.matches) {
      const particles = dialog.querySelector('.film-particles');
      for (let n = 0; n < 17; n++) {
        const tomato = document.createElement('img');
        tomato.src = 'assets/tomato.png'; tomato.alt = '';
        tomato.style.cssText = `--size:${35 + (n % 5) * 13}px;--x:${-stage.clientWidth * .45 + (n % 7) * stage.clientWidth * .11}px;--y:${stage.clientHeight * .5}px;--delay:${n * .19}s;--duration:${2 + (n % 3) * .3}s`;
        particles.append(tomato);
      }
    }
  }
  function tick(now) {
    const elapsed = Math.min((now - startedAt) / 1000, 18);
    setChapter(elapsed < 4.5 ? 0 : elapsed < 9 ? 1 : elapsed < 14 ? 2 : 3);
    const seconds = elapsed < 9 ? Math.max(0, Math.ceil(1500 * (1 - elapsed / 9))) : elapsed < 14 ? 0 : 1500;
    document.querySelector('#film-time').textContent = `${String(Math.floor(seconds / 60)).padStart(2, '0')}:${String(seconds % 60).padStart(2, '0')}`;
    if (elapsed < 18) frame = requestAnimationFrame(tick);
    else document.querySelector('#film-status').textContent = '宣传动画播放完毕。可以重新播放，或关闭后下载朱果。';
  }
  function play() {
    cancelAnimationFrame(frame);
    previousChapter = -1;
    dialog.classList.remove('playing');
    dialog.querySelector('.film-particles').replaceChildren();
    document.querySelector('#film-status').textContent = '';
    void dialog.offsetWidth;
    dialog.classList.add('playing');
    startedAt = performance.now();
    frame = requestAnimationFrame(tick);
  }
  document.querySelector('#watch-film').addEventListener('click', () => { dialog.showModal(); document.body.style.overflow = 'hidden'; play(); });
  document.querySelector('#close-film').addEventListener('click', () => dialog.close());
  document.querySelector('#replay-film').addEventListener('click', play);
  dialog.addEventListener('click', event => { if (event.target === dialog) { const box = dialog.getBoundingClientRect(); if (event.clientX < box.left || event.clientX > box.right || event.clientY < box.top || event.clientY > box.bottom) dialog.close(); } });
  dialog.addEventListener('close', () => { cancelAnimationFrame(frame); dialog.classList.remove('playing'); dialog.querySelector('.film-particles').replaceChildren(); document.body.style.overflow = ''; });
  const release = window.TOMATO_RELEASE;
  if (release) {
    document.querySelector('#installer-link').href = release.installer;
    document.querySelector('#portable-link').href = release.portable;
    document.querySelector('#release-meta').textContent = `v${release.version} · Windows 10 / 11 · x64 · ${(release.installerBytes / 1048576).toFixed(1)} MB`;
  }
})();
