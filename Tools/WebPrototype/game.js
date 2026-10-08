// The Data Heist - browser build of Level 1 (renderer, input, audio, screens).
(function () {
  'use strict';
  const A = window.DH_ASSETS;
  const { Sim, CHARACTERS } = window.DataHeist;
  const LEVEL = A.level;
  const VW = 640, VH = 360, PPU = 32;
  const CHAR_IDS = ['kai', 'nova', 'jax', 'lin'];

  // ------------------------------------------------------------------ save
  const SAVE_KEY = 'dataheist.save.v1';
  const save = Object.assign({ bytes: 0, unlocked: ['kai'], selected: 'kai', cleared: {}, best: {}, music: 0.6, sfx: 0.8 }, readSave());
  function readSave() { try { return JSON.parse(localStorage.getItem(SAVE_KEY) || '{}'); } catch (e) { return {}; } }
  function writeSave() { try { localStorage.setItem(SAVE_KEY, JSON.stringify(save)); } catch (e) { /* storage blocked */ } }

  // ------------------------------------------------------------------ canvas
  const cv = document.getElementById('game');
  const ctx = cv.getContext('2d');
  cv.width = VW; cv.height = VH;
  ctx.imageSmoothingEnabled = false;
  const atlas = new Image(); atlas.src = A.atlas;
  const PAR = {};
  for (const k in A.parallax) { PAR[k] = new Image(); PAR[k].src = A.parallax[k]; }

  function fitCanvas() {
    const wrap = document.getElementById('stage');
    const w = wrap.clientWidth, h = Math.max(200, window.innerHeight - 140);
    let s = Math.min(w / VW, h / VH);
    if (s >= 1) s = Math.floor(s * 2) / 2;
    const fw = Math.floor(VW * s), fh = Math.floor(VH * s);
    cv.style.width = fw + 'px'; cv.style.height = fh + 'px';
    const fr = document.getElementById('frame'); fr.style.width = fw + 'px'; fr.style.height = fh + 'px';
  }
  window.addEventListener('resize', fitCanvas);

  function spr(name, x, y, opts) {
    const f = A.frames[name];
    if (!f) return;
    opts = opts || {};
    const w = f[2] * (opts.sx || 1), h = f[3] * (opts.sy || 1);
    let dx = Math.round(x - w * (opts.ax === undefined ? 0.5 : opts.ax));
    let dy = Math.round(y - h * (opts.ay === undefined ? 1 : opts.ay));
    if (opts.alpha !== undefined) ctx.globalAlpha = opts.alpha;
    if (opts.flip) {
      ctx.save(); ctx.translate(dx + w, dy); ctx.scale(-1, 1);
      ctx.drawImage(atlas, f[0], f[1], f[2], f[3], 0, 0, w, h); ctx.restore();
    } else ctx.drawImage(atlas, f[0], f[1], f[2], f[3], dx, dy, w, h);
    if (opts.alpha !== undefined) ctx.globalAlpha = 1;
  }

  // tinted sprite via offscreen cache
  const tintCache = {};
  function sprTint(name, color, x, y, opts) {
    const key = name + color;
    let c = tintCache[key];
    const f = A.frames[name];
    if (!f) return;
    if (!c) {
      c = document.createElement('canvas'); c.width = f[2]; c.height = f[3];
      const g = c.getContext('2d');
      g.drawImage(atlas, f[0], f[1], f[2], f[3], 0, 0, f[2], f[3]);
      g.globalCompositeOperation = 'source-atop'; g.fillStyle = color; g.fillRect(0, 0, f[2], f[3]);
      tintCache[key] = c;
    }
    opts = opts || {};
    const w = f[2] * (opts.s || 1), h = f[3] * (opts.s || 1);
    ctx.globalAlpha = opts.alpha === undefined ? 1 : opts.alpha;
    if (opts.add) ctx.globalCompositeOperation = 'lighter';
    ctx.drawImage(c, Math.round(x - w * (opts.ax === undefined ? .5 : opts.ax)), Math.round(y - h * (opts.ay === undefined ? .5 : opts.ay)), w, h);
    ctx.globalAlpha = 1; ctx.globalCompositeOperation = 'source-over';
  }

  // ------------------------------------------------------------------ audio
  const AU = { ctx: null, buf: {}, music: null, musicGain: null, sfxGain: null, ready: false, current: null };
  function initAudio() {
    if (AU.ctx) return;
    try {
      AU.ctx = new (window.AudioContext || window.webkitAudioContext)();
    } catch (e) { return; }
    AU.musicGain = AU.ctx.createGain(); AU.musicGain.gain.value = save.music * 0.55; AU.musicGain.connect(AU.ctx.destination);
    AU.sfxGain = AU.ctx.createGain(); AU.sfxGain.gain.value = save.sfx * 0.7; AU.sfxGain.connect(AU.ctx.destination);
    const jobs = Object.keys(A.audio).map(k => {
      const b64 = A.audio[k].split(',')[1];
      const bin = atob(b64); const arr = new Uint8Array(bin.length);
      for (let i = 0; i < bin.length; i++) arr[i] = bin.charCodeAt(i);
      return AU.ctx.decodeAudioData(arr.buffer).then(b => { AU.buf[k] = b; }).catch(() => {});
    });
    Promise.all(jobs).then(() => { AU.ready = true; if (AU.want) playMusic(AU.want, true); });
  }
  function sfx(name, vol, rate) {
    if (!AU.ctx || !AU.buf['sfx_' + name]) return;
    const s = AU.ctx.createBufferSource(); s.buffer = AU.buf['sfx_' + name];
    if (rate) s.playbackRate.value = rate;
    const g = AU.ctx.createGain(); g.gain.value = vol === undefined ? 1 : vol;
    s.connect(g); g.connect(AU.sfxGain); s.start();
  }
  function playMusic(name, force) {
    AU.want = name;
    if (!AU.ctx || !AU.ready) return;
    if (AU.current === name && !force) return;
    if (AU.music) { try { AU.music.stop(); } catch (e) {} }
    const s = AU.ctx.createBufferSource(); s.buffer = AU.buf[name]; s.loop = true;
    s.connect(AU.musicGain); s.start(); AU.music = s; AU.current = name;
  }
  function musicRate(r) { if (AU.music) AU.music.playbackRate.setTargetAtTime(r, AU.ctx.currentTime, 0.08); }

  // ------------------------------------------------------------------ input
  const keys = {};
  const pressed = {};
  const BIND = {
    jump: ['Space', 'ArrowUp', 'KeyK'], slide: ['KeyS', 'ArrowDown'], attack: ['KeyJ', 'KeyF', 'Mouse0'],
    ability: ['KeyW', 'KeyL', 'ShiftLeft'], pause: ['Escape', 'KeyP'],
  };
  function actionOf(code) { for (const a in BIND) if (BIND[a].includes(code)) return a; return null; }
  function down(code) {
    const a = actionOf(code); if (!a) return false;
    if (!keys[a]) pressed[a] = true; keys[a] = true; return true;
  }
  function up(code) { const a = actionOf(code); if (a) keys[a] = false; }
  window.addEventListener('keydown', e => {
    initAudio();
    if (screen === 'play' && down(e.code)) e.preventDefault();
    else if (screen !== 'play') menuKey(e);
  });
  window.addEventListener('keyup', e => up(e.code));
  cv.addEventListener('mousedown', e => { initAudio(); if (screen === 'play') { down('Mouse' + e.button); e.preventDefault(); } });
  window.addEventListener('mouseup', e => up('Mouse' + e.button));
  cv.addEventListener('contextmenu', e => e.preventDefault());
  // touch buttons
  document.querySelectorAll('[data-touch]').forEach(b => {
    const a = b.dataset.touch;
    const on = e => { e.preventDefault(); initAudio(); if (!keys[a]) pressed[a] = true; keys[a] = true; b.classList.add('on'); };
    const off = e => { e.preventDefault(); keys[a] = false; b.classList.remove('on'); };
    b.addEventListener('pointerdown', on); b.addEventListener('pointerup', off);
    b.addEventListener('pointerleave', off); b.addEventListener('pointercancel', off);
  });
  function consume(a) { const v = !!pressed[a]; pressed[a] = false; return v; }

  // ------------------------------------------------------------------ game state
  let screen = 'title';     // title | select | play | paused | results | over
  let sim = null, charId = save.selected;
  let cam = { x: 0, y: 2 }, shake = 0, hitStop = 0, countdown = 0, cdLast = -1;
  let particles = [], floaters = [], flashT = 0, slashT = -1, empRing = -1, ghosts = [];
  let lastGroundY = 0, runDist = 0, wallTime = 0, promptShown = {}, alarmOn = false, alarmT = 0;
  let realSlow = 0;

  function startLevel() {
    sim = new Sim(LEVEL, charId);
    particles = []; floaters = []; ghosts = []; promptShown = {};
    cam.x = sim.p.x - 5.5; cam.y = sim.p.y + 2.6; lastGroundY = sim.p.y;
    countdown = 3.2; cdLast = -1; runDist = 0; realSlow = 0; alarmOn = false;
    for (const k in keys) keys[k] = false;
    setScreen('play');
    playMusic('music_level01', true); musicRate(1);
  }

  // ------------------------------------------------------------------ helpers
  const W2S = (wx, wy) => [Math.round((wx - cam.x) * PPU), Math.round(VH / 2 - (wy - cam.y) * PPU)];
  function burst(x, y, n, colors, spd, life, grav) {
    for (let i = 0; i < n; i++) {
      const a = Math.random() * Math.PI * 2, v = spd * (0.3 + Math.random() * 0.7);
      particles.push({ x, y, vx: Math.cos(a) * v, vy: Math.sin(a) * v, life: life * (0.6 + Math.random() * .4), max: life, c: colors[i % colors.length], g: grav === undefined ? -12 : grav, s: Math.random() < .3 ? 2 : 1 });
    }
  }
  function floatText(x, y, text, color) { floaters.push({ x, y, text, color, t: 0 }); }

  function handleEvents() {
    const ch = CHARACTERS[charId];
    for (const ev of sim.events) {
      switch (ev.type) {
        case 'jump': sfx('jump', .7); burst(sim.p.x, sim.p.y, 6, ['#b8a8e0', '#6a5f8e'], 3, .35, -6); break;
        case 'airJump':
          sfx('air_jump', .8); burst(sim.p.x - .1, sim.p.y, 14, [ch.color, '#ff8a2a', '#ffffff'], 5, .4, -18); break;
        case 'land': sfx('land', ev.hard ? .8 : .45); burst(sim.p.x, sim.p.y, ev.hard ? 10 : 5, ['#8a7fb0', '#4b4268'], 3, .3, -8); if (ev.hard) shake = Math.max(shake, .15); break;
        case 'slide': sfx('slide', .6); break;
        case 'dive': sfx('dash', .35, 1.6); break;
        case 'attack': sfx('attack', .7); slashT = 0; break;
        case 'byte': {
          sfx('byte', .45, 1 + Math.min(.5, (byteChain++) * .03)); byteChainT = .5;
          burst(ev.x, ev.y, 4, ['#2ff3ff', '#ffffff'], 2.5, .3, 0); break;
        }
        case 'repair': sfx('repair', .8); floatText(ev.x, ev.y + .6, '+1 HP', '#7dff6a'); burst(ev.x, ev.y, 14, ['#7dff6a', '#ffffff'], 4, .6, 0); break;
        case 'shard': sfx('shard', 1); floatText(ev.x, ev.y + 1, 'DATA SHARD SECURED', '#ff3fa4'); burst(ev.x, ev.y, 40, ['#ff3fa4', '#2ff3ff', '#ffffff'], 7, 1, 0); flashT = .25; break;
        case 'pad': sfx('pad', .8); burst(ev.x, ev.y + .3, 12, ['#ffd23a', '#ff8a2a'], 5, .4, -10); break;
        case 'droneDown':
          sfx('explode', .7); shake = Math.max(shake, .22); hitStop = Math.max(hitStop, ev.how === 'emp' ? 0 : .05);
          particles.push({ x: ev.x, y: ev.y, anim: 'explosion', t: 0, life: .42, max: .42 });
          burst(ev.x, ev.y, 16, ['#ffd23a', '#ff8a2a', '#ff3fa4', '#565f7e'], 7, .7, -14);
          floatText(ev.x, ev.y + .5, ev.how === 'stomp' ? 'STOMP +25' : '+25', '#ffd23a');
          break;
        case 'canHit': burst(ev.x, ev.y + .4, 14, ['#4a5a78', '#7dff6a', '#7d90b4'], 6, .6, -20); break;
        case 'hurt':
          sfx('hurt', 1); shake = .35; hitStop = .08; flashT = .18;
          floatText(sim.p.x, sim.p.y + 1.6, '-1', '#ff2d55'); break;
        case 'dead':
          sfx(ev.cause === 'fall' ? 'game_over' : 'game_over', 1); shake = .4; musicRate(.6);
          setTimeout(() => { if (sim.state === 'dead') showOver(ev.cause); }, 900); break;
        case 'checkpoint': sfx('checkpoint', .9); floatText(ev.x, sim.groundAt(ev.x) + 2.4, 'CHECKPOINT', '#7dff6a'); break;
        case 'dash': sfx('dash', .9); shake = .1; break;
        case 'emp': sfx('emp', 1); empRing = 0; shake = .3; flashT = .2; break;
        case 'glitch': sfx('glitch', 1); realSlow = 3.0; break;
        case 'abilityDenied': sfx('denied', .4); break;
        case 'shoot': sfx('shoot', .5); break;
        case 'deflect': sfx('deflect', .9); burst(ev.x, ev.y, 10, ['#ff8a2a', '#ffffff'], 5, .35, 0); break;
        case 'respawn': countdown = 1.6; cdLast = -1; musicRate(1); break;
        case 'win': onWin(); break;
      }
    }
    sim.events.length = 0;
  }
  let byteChain = 0, byteChainT = 0;

  // ------------------------------------------------------------------ loop
  let acc = 0, last = performance.now();
  const STEP = 1 / 120;
  function frame(now) {
    let dt = Math.min(0.05, (now - last) / 1000); last = now;
    if (screen === 'play' && sim) update(dt);
    else if (screen === 'title' || screen === 'select') { titleT += dt; cam.x += dt * 4; }
    render(dt);
    drawSelectPreviews(dt);
    requestAnimationFrame(frame);
  }
  let titleT = 0;

  function update(dt) {
    if (consume('pause')) { pauseGame(); return; }
    byteChainT -= dt; if (byteChainT <= 0) byteChain = 0;
    if (countdown > 0) {
      countdown -= dt;
      const c = Math.ceil(countdown - 0.2);
      if (c !== cdLast) { cdLast = c; if (c > 0 && c <= 3) sfx('count', .6); else if (c <= 0) sfx('go', .7); }
      pressed.jump = pressed.slide = pressed.attack = pressed.ability = false;
      camFollow(dt);
      return;
    }
    if (hitStop > 0) { hitStop -= dt; return; }
    // time glitch (Lin) slows the world; runs on real time
    let scale = 1;
    if (realSlow > 0) { realSlow -= dt; scale = 0.5; musicRate(.75); if (realSlow <= 0) musicRate(1); }
    acc += dt * scale;
    let guard = 0;
    while (acc >= STEP && guard++ < 20) {
      const inp = {
        jumpPressed: consume('jump'), jumpHeld: !!keys.jump,
        slidePressed: consume('slide'), slideHeld: !!keys.slide,
        attackPressed: consume('attack'), abilityPressed: consume('ability'),
      };
      const px = sim.p.x;
      sim.step(STEP, inp);
      if (sim.p.onGround) runDist += sim.p.x - px;
      acc -= STEP;
      handleEvents();
      if (sim.state !== 'run') { acc = 0; break; }
    }
    // dash afterimages
    if (sim.p.dashT > 0 && Math.random() < .8) ghosts.push({ x: sim.p.x, y: sim.p.y, t: .25, f: frameName() });
    // alarm zone
    const al = sim.inZoneAlarm();
    if (al && !alarmOn) { sfx('alarm', .6); }
    alarmOn = al;
    camFollow(dt);
  }

  function camFollow(dt) {
    const p = sim.p;
    if (p.onGround) lastGroundY = p.y;
    let ty = lastGroundY + 2.4;
    if (p.y < lastGroundY - .5) ty = p.y + 2.4;      // follow falls
    if (p.y > lastGroundY + 3.2) ty = p.y - 0.8;      // and big launches
    cam.y += (ty - cam.y) * Math.min(1, dt * 4);
    const tx = p.x - 5.5;
    cam.x += (tx - cam.x) * Math.min(1, dt * 12);
  }

  function frameName() {
    const p = sim.p, id = charId;
    if (sim.state === 'won') return `${id}_win_0${Math.floor(performance.now() / 300) % 2}`;
    if (p.hurtT > 0) return `${id}_hurt_0${p.hurtT > .17 ? 0 : 1}`;
    if (p.dashT > 0) return `${id}_ability_0${Math.floor(performance.now() / 60) % 2}`;
    if (p.attackT >= 0) return `${id}_attack_0${Math.min(3, Math.floor(p.attackT / 0.07))}`;
    if (p.sliding) return `${id}_slide_0${Math.floor(performance.now() / 120) % 2}`;
    if (!p.onGround) {
      if (p.vy > 4) return `${id}_jump_00`;
      if (p.vy > -3) return `${id}_jump_01`;
      return `${id}_fall_0${Math.floor(performance.now() / 120) % 2}`;
    }
    return `${id}_run_0${Math.floor(runDist / 0.42) % 8}`;
  }

  // ------------------------------------------------------------------ render
  function render(dt) {
    ctx.setTransform(1, 0, 0, 1, 0, 0);
    let ox = 0, oy = 0;
    if (shake > 0) { shake -= dt; ox = (Math.random() - .5) * 8 * shake; oy = (Math.random() - .5) * 8 * shake; }
    ctx.setTransform(1, 0, 0, 1, Math.round(ox), Math.round(oy));
    drawBackground();
    if (!sim) { drawTitleWorld(); ctx.setTransform(1, 0, 0, 1, 0, 0); return; }
    drawWorld(dt);
    ctx.setTransform(1, 0, 0, 1, 0, 0);
    drawOverlays(dt);
    if (screen === 'play' || screen === 'paused') drawHUD();
  }

  function drawBackground() {
    if (PAR.sky.complete) ctx.drawImage(PAR.sky, 0, 0, VW, VH);
    const layers = [['city_far', .08, 70], ['city_mid', .18, 120], ['city_near', .32, 180]];
    for (const [k, f, base] of layers) {
      const im = PAR[k]; if (!im.complete) continue;
      const yoff = base + (cam.y - 2.4) * PPU * f * 0.6;
      let x = -((cam.x * PPU * f) % im.width); if (x > 0) x -= im.width;
      for (; x < VW; x += im.width) ctx.drawImage(im, Math.round(x), Math.round(VH - im.height - 40 + yoff - base + 60));
    }
    // haze
    const g = ctx.createLinearGradient(0, VH * .45, 0, VH);
    g.addColorStop(0, 'rgba(90,31,94,0)'); g.addColorStop(1, 'rgba(60,20,80,.55)');
    ctx.fillStyle = g; ctx.fillRect(0, 0, VW, VH);
    // rain
    ctx.fillStyle = 'rgba(170,200,255,.18)';
    const t = performance.now() / 1000;
    for (let i = 0; i < 70; i++) {
      const rx = ((i * 97.3 + t * 60 - cam.x * 10) % VW + VW) % VW;
      const ry = ((i * 53.7 + t * 420) % (VH + 20)) - 10;
      ctx.fillRect(Math.round(rx), Math.round(ry), 1, 7);
    }
  }

  function drawTitleWorld() {
    // a strip of rooftop so the title screen has life
    const groundY = VH - 70;
    for (let x = -((cam.x * PPU) % 32) - 32; x < VW; x += 32) {
      spr('rooftop_violet', x, groundY, { ax: 0, ay: 0 });
      for (let y = groundY + 16; y < VH; y += 32) spr('facade_violet_' + (Math.abs(Math.floor((x + cam.x * PPU) / 32)) % 3), x, y, { ax: 0, ay: 0 });
    }
    const id = screen === 'select' ? selCursorId() : 'kai';
    spr(`${id}_run_0${Math.floor(titleT * 14) % 8}`, 150, groundY + 2);
  }

  function drawRoof(r) {
    const [x0] = W2S(r.x0, r.y), [x1, top] = W2S(r.x1, r.y);
    if (x1 < -40 || x0 > VW + 40) return;
    const st = r.style;
    for (let x = x0; x < x1; x += 32) {
      const w = Math.min(32, x1 - x);
      const col = Math.floor((x - x0) / 32);
      for (let y = top + 14; y < VH + 32; y += 32) {
        const v = (((col * 7 + Math.floor((y - top) / 32) * 3 + Math.floor(r.x0)) % 5) + 5) % 5;
        const name = 'facade_' + st + '_' + (v === 4 ? 2 : v % 2);
        const f = A.frames[name];
        ctx.drawImage(atlas, f[0], f[1], w, 32, x, y, w, 32);
      }
      const f = A.frames['rooftop_' + st];
      ctx.drawImage(atlas, f[0], f[1], w, 16, x, top - 2, w, 16);
    }
    ctx.fillStyle = 'rgba(10,6,20,.55)';
    ctx.fillRect(x0, top + 14, 3, VH);
    ctx.fillStyle = 'rgba(255,255,255,.08)';
    ctx.fillRect(x1 - 2, top + 14, 2, VH);
  }

  function drawPlatform(pl) {
    const [x0, y] = W2S(pl.x0, pl.y), [x1] = W2S(pl.x1, pl.y);
    if (x1 < -20 || x0 > VW + 20) return;
    // cables up to the sky
    ctx.fillStyle = '#241e38';
    for (let x = x0 + 12; x < x1; x += 96) ctx.fillRect(x, 0, 2, y);
    ctx.fillStyle = '#3b3456'; ctx.fillRect(x0, y, x1 - x0, 6);
    ctx.fillStyle = '#6a5f8e'; ctx.fillRect(x0, y, x1 - x0, 1);
    ctx.fillStyle = '#ff3fa4'; ctx.fillRect(x0, y + 6, x1 - x0, 2);
    ctx.fillStyle = 'rgba(255,63,164,.25)'; ctx.fillRect(x0, y + 8, x1 - x0, 6);
    ctx.fillStyle = '#241e38';
    for (let x = x0; x < x1; x += 8) ctx.fillRect(x, y + 2, 4, 3);
  }

  function drawWorld(dt) {
    const t = performance.now() / 1000;
    const p = sim.p, ch = CHARACTERS[charId];
    // decor (behind)
    for (const d of LEVEL.decor) {
      const [sx, sy] = W2S(d.x, d.y);
      if (sx < -60 || sx > VW + 60) continue;
      spr(d.k, sx, sy + 1);
    }
    // exit tower behind
    { const [sx, sy] = W2S(LEVEL.exitX + 3.5, sim.groundAt(LEVEL.exitX));
      spr('extraction_0' + (Math.floor(t * 3) % 2), sx, sy + 1); }
    for (const r of LEVEL.roofs) drawRoof(r);
    for (const pl of LEVEL.platforms) drawPlatform(pl);
    { const [sx, sy] = W2S(LEVEL.exitX - 2.5, sim.groundAt(LEVEL.exitX)); spr('extraction_pad', sx, sy - 2, { ax: 0, ay: 0 }); }
    // entities
    const i0 = sim.lower(cam.x - 3);
    for (let i = i0; i < sim.ents.length; i++) {
      const e = sim.ents[i];
      if (e.ox > cam.x + 24) break;
      drawEntity(e, t);
    }
    for (const b of sim.bolts) {
      const [sx, sy] = W2S(b.x, b.y);
      spr('bolt_0' + (Math.floor(t * 20) % 2), sx, sy, { ay: .5 });
    }
    // ghosts
    ghosts = ghosts.filter(g => (g.t -= dt) > 0);
    for (const g of ghosts) {
      const [sx, sy] = W2S(g.x, g.y);
      sprTint(g.f, ch.color, sx, sy + 1, { alpha: g.t * 2.4, ay: 1 });
    }
    // player
    const [px, py] = W2S(p.x, p.y);
    const fname = frameName();
    const blink = p.inv > 0 && sim.state === 'run' && Math.floor(t * 14) % 2 === 0;
    if (sim.state !== 'dead' || p.y > LEVEL.killY) {
      // shadow
      const gy = sim.groundAt(p.x);
      if (gy > -100 && p.y - gy < 6) {
        const [, gsy] = W2S(p.x, gy);
        ctx.fillStyle = 'rgba(10,4,20,.45)';
        const w = Math.max(6, 18 - (p.y - gy) * 3);
        ctx.fillRect(Math.round(px - w / 2), gsy - 1, Math.round(w), 2);
      }
      if (realSlow > 0) sprTint(fname, '#7dff6a', px - 3, py + 1, { alpha: .35, ay: 1 });
      if (!blink) spr(fname, px, py + 1);
      else sprTint(fname, '#ffffff', px, py + 1, { alpha: .8, ay: 1 });
    }
    // slash fx
    if (slashT >= 0) {
      slashT += dt;
      const k = Math.floor(slashT / 0.05);
      if (k > 2) slashT = -1;
      else sprTint('slash_0' + k, ch.color, px + 22 * (ch.reach / 1.8), py - 22, { s: 1.0 * (ch.reach / 1.8) + .1, add: true });
    }
    // EMP ring
    if (empRing >= 0) {
      empRing += dt;
      const k = Math.floor(empRing / .06);
      if (k > 5) empRing = -1; else sprTint('ring_0' + k, '#38b6ff', px, py - 18, { s: 2 + k * 1.6, alpha: 1 - k / 6, add: true });
    }
    // particles
    particles = particles.filter(q => (q.life -= dt) > 0);
    for (const q of particles) {
      if (q.anim) {
        q.t += dt;
        const [sx, sy] = W2S(q.x, q.y);
        spr('explosion_0' + Math.min(5, Math.floor(q.t / .07)), sx, sy, { ay: .5 });
        continue;
      }
      q.vy += q.g * dt; q.x += q.vx * dt; q.y += q.vy * dt;
      const [sx, sy] = W2S(q.x, q.y);
      ctx.globalAlpha = Math.min(1, q.life / q.max * 1.5);
      ctx.fillStyle = q.c; ctx.fillRect(sx, sy, q.s * 2, q.s * 2);
    }
    ctx.globalAlpha = 1;
    floaters = floaters.filter(f => (f.t += dt) < 1.1);
    ctx.font = '8px "Press Start 2P", monospace'; ctx.textAlign = 'center';
    for (const f of floaters) {
      const [sx, sy] = W2S(f.x, f.y + f.t * 1.2);
      ctx.globalAlpha = Math.min(1, (1.1 - f.t) * 3);
      ctx.fillStyle = '#120a1a'; ctx.fillText(f.text, sx + 1, sy + 1);
      ctx.fillStyle = f.color; ctx.fillText(f.text, sx, sy);
    }
    ctx.globalAlpha = 1; ctx.textAlign = 'left';
  }

  function drawEntity(e, t) {
    const [sx, sy] = W2S(e.x, e.y);
    if (sx < -80 || sx > VW + 80) return;
    switch (e.t) {
      case 'byte': if (e.alive) spr('byte_0' + (Math.floor(t * 10 + e.ox) % 6), sx, sy, { ay: .5 }); break;
      case 'can': if (e.alive) spr('garbage_can', sx, sy + 1); break;
      case 'barrier': spr('barrier', sx, sy + 1); break;
      case 'pad': spr('jumppad_0' + (sim.p.pad && Math.abs(sim.p.x - e.x) < 2 ? 1 : 0), sx, sy + 1); break;
      case 'drone': case 'gunner':
        if (!e.alive) break;
        spr(`drone_${e.t === 'gunner' ? 'gunner' : 'scout'}_0${Math.floor(t * 16) % 4}`, sx, sy, { ay: .5 });
        if (e.t === 'gunner') {
          // targeting laser toward runner height
          const dx = e.x - sim.p.x;
          if (dx < 15 && dx > 6) {
            ctx.fillStyle = `rgba(255,138,42,${.25 + .25 * Math.sin(t * 20)})`;
            const [, ty] = W2S(0, sim.groundAt(e.x) + .8);
            ctx.fillRect(sx - 140, ty, 130, 1);
          }
        }
        break;
      case 'repair': if (e.alive) spr('repair_0' + (Math.floor(t * 4) % 2), sx, sy + Math.sin(t * 3) * 2, { ay: .5 }); break;
      case 'shard':
        if (e.alive) {
          sprTint('glow', '#ff3fa4', sx, sy, { s: 1.6 + Math.sin(t * 4) * .2, alpha: .5, add: true });
          spr('shard_0' + (Math.floor(t * 6) % 4), sx, sy + Math.sin(t * 2.5) * 3, { ay: .5 });
        }
        break;
      case 'checkpoint': spr(e.alive ? 'checkpoint_off' : 'checkpoint_on', sx, sy + 1); break;
      case 'laser': {
        const off = sim.empT > 0;
        const [, topY] = W2S(e.x, e.y + e.h);
        const [, botY] = W2S(e.x, e.y);
        if (!off || Math.floor(t * 12) % 7 === 0) {
          const f = A.frames['laser_beam_0' + (Math.floor(t * 15) % 3)];
          for (let y = topY; y < botY; y += 32) ctx.drawImage(atlas, f[0], f[1], 16, Math.min(32, botY - y), sx - 8, y, 16, Math.min(32, botY - y));
          sprTint('glow', '#ff2d55', sx, (topY + botY) / 2, { s: .5, sy: 1, alpha: .35, add: true });
        }
        spr('laser_emitter_' + (off ? 'off' : 'on'), sx, topY + 2);
        if (e.mode === 'high') { ctx.fillStyle = '#241e38'; ctx.fillRect(sx - 1, 0, 2, Math.max(0, topY - 14)); }
        else { ctx.fillStyle = '#3b3456'; ctx.fillRect(sx - 5, botY - 3, 10, 3); }
        break;
      }
    }
  }

  // ------------------------------------------------------------------ overlays / HUD
  function drawOverlays(dt) {
    const t = performance.now() / 1000;
    if (alarmOn && sim.state === 'run') {
      const a = .12 + .1 * Math.sin(t * 6);
      const g = ctx.createRadialGradient(VW / 2, VH / 2, VH * .35, VW / 2, VH / 2, VH * .85);
      g.addColorStop(0, 'rgba(255,45,85,0)'); g.addColorStop(1, `rgba(255,45,85,${a})`);
      ctx.fillStyle = g; ctx.fillRect(0, 0, VW, VH);
    }
    if (realSlow > 0) {
      ctx.fillStyle = 'rgba(125,255,106,.07)'; ctx.fillRect(0, 0, VW, VH);
      ctx.fillStyle = 'rgba(0,0,0,.18)';
      for (let y = (Math.floor(t * 60) % 3); y < VH; y += 3) ctx.fillRect(0, y, VW, 1);
      if (Math.random() < .3) { const y = Math.random() * VH; ctx.drawImage(cv, 0, y, VW, 6, 4, y, VW, 6); }
    }
    if (flashT > 0) { flashT -= dt; ctx.fillStyle = `rgba(255,255,255,${flashT * 2})`; ctx.fillRect(0, 0, VW, VH); }
    // scanlines (subtle CRT)
    ctx.fillStyle = 'rgba(0,0,0,.07)';
    for (let y = 0; y < VH; y += 2) ctx.fillRect(0, y, VW, 1);
    if (countdown > 0 && screen === 'play') {
      const c = Math.ceil(countdown - .2);
      const label = c > 0 ? String(c) : 'RUN!';
      ctx.font = '32px "Press Start 2P", monospace'; ctx.textAlign = 'center';
      ctx.fillStyle = '#120a1a'; ctx.fillText(label, VW / 2 + 3, VH / 2 + 3);
      ctx.fillStyle = c > 0 ? '#2ff3ff' : '#ff3fa4'; ctx.fillText(label, VW / 2, VH / 2);
      if (sim && sim.stats.deaths === 0 && countdown > 1) {
        ctx.font = '8px "Press Start 2P", monospace';
        ctx.fillStyle = '#ffd23a'; ctx.fillText(LEVEL.name + '  -  ' + LEVEL.subtitle.toUpperCase(), VW / 2, VH / 2 + 30);
      }
      ctx.textAlign = 'left';
    }
    drawPrompt();
  }

  function drawPrompt() {
    if (!sim || screen !== 'play') return;
    const p = sim.p;
    let pr = null;
    for (const q of LEVEL.prompts) if (p.x >= q.x - 9 && p.x <= q.x + 4) pr = q;
    if (!pr) return;
    const ch = CHARACTERS[charId];
    const title = pr.ability ? ch.ability : pr.title;
    const body = pr.ability ? ch.abilityText : pr.body;
    const key = pr.ability ? 'W' : pr.key;
    const fade = Math.min(1, (p.x - (pr.x - 9)) * 2, (pr.x + 4 - p.x) * 1.5);
    ctx.globalAlpha = Math.max(0, fade);
    ctx.font = '8px "Press Start 2P", monospace';
    const kw = Math.max(26, ctx.measureText(key).width + 14);
    const w = 380, x = (VW - w) / 2, y = 34;
    ctx.font = '16px "VT323", monospace';
    const lines = splitLines(body, w - kw - 34);
    const h = Math.max(48, 30 + lines.length * 13);
    ctx.fillStyle = 'rgba(18,10,36,.9)'; ctx.fillRect(x, y, w, h);
    ctx.fillStyle = pr.key === '!' ? '#ff2d55' : '#2ff3ff';
    ctx.fillRect(x, y, w, 2); ctx.fillRect(x, y + h - 2, w, 2);
    ctx.font = '8px "Press Start 2P", monospace';
    ctx.fillStyle = '#5a4e80'; ctx.fillRect(x + 10, y + 10, kw, 26);
    ctx.fillStyle = '#2c2640'; ctx.fillRect(x + 11, y + 11, kw - 2, 21);
    ctx.fillStyle = '#ffffff'; ctx.textAlign = 'center'; ctx.fillText(key, x + 10 + kw / 2, y + 26); ctx.textAlign = 'left';
    ctx.font = '10px "Press Start 2P", monospace';
    ctx.fillStyle = pr.key === '!' ? '#ff2d55' : '#ffd23a';
    ctx.fillText(title, x + kw + 22, y + 20);
    ctx.font = '16px "VT323", monospace'; ctx.fillStyle = '#e8e2ff';
    lines.forEach((l, i) => ctx.fillText(l, x + kw + 22, y + 36 + i * 13));
    ctx.globalAlpha = 1;
  }

  function splitLines(text, maxW) {
    const out = []; let line = '';
    for (const w of text.split(' ')) {
      const test = line ? line + ' ' + w : w;
      if (ctx.measureText(test).width > maxW && line) { out.push(line); line = w; } else line = test;
    }
    if (line) out.push(line);
    return out;
  }

  function wrapText(text, x, y, maxW, lh) {
    const words = text.split(' '); let line = '';
    for (const w of words) {
      const test = line ? line + ' ' + w : w;
      if (ctx.measureText(test).width > maxW && line) { ctx.fillText(line, x, y); y += lh; line = w; }
      else line = test;
    }
    if (line) ctx.fillText(line, x, y);
  }

  function drawHUD() {
    const ch = CHARACTERS[charId], p = sim.p;
    // hearts
    for (let i = 0; i < ch.hp; i++) spr(i < p.hp ? 'heart_full' : 'heart_empty', 12 + i * 18, 12, { ax: 0, ay: 0 });
    // bytes
    spr('icon_byte', 12, 34, { ax: 0, ay: 0 });
    ctx.font = '10px "Press Start 2P", monospace';
    ctx.fillStyle = '#120a1a'; ctx.fillText(String(sim.stats.bytes).padStart(3, '0'), 33, 47);
    ctx.fillStyle = '#2ff3ff'; ctx.fillText(String(sim.stats.bytes).padStart(3, '0'), 32, 46);
    // shard indicator
    const f = A.frames.icon_shard;
    ctx.globalAlpha = sim.stats.shard ? 1 : .3;
    ctx.drawImage(atlas, f[0], f[1], f[2], f[3], 82, 30, 16, 24);
    ctx.globalAlpha = 1;
    // progress bar
    const bx = VW - 178, by = 14, bw = 160;
    ctx.fillStyle = 'rgba(18,10,36,.8)'; ctx.fillRect(bx - 2, by - 2, bw + 4, 10);
    ctx.fillStyle = '#3d2466'; ctx.fillRect(bx, by, bw, 6);
    const prog = Math.max(0, Math.min(1, p.x / LEVEL.exitX));
    ctx.fillStyle = alarmOn ? '#ff2d55' : '#2ff3ff'; ctx.fillRect(bx, by, Math.round(bw * prog), 6);
    for (const e of sim.ents) if (e.t === 'checkpoint') { ctx.fillStyle = e.alive ? '#5a5070' : '#7dff6a'; ctx.fillRect(bx + Math.round(bw * e.x / LEVEL.exitX) - 1, by - 3, 2, 12); }
    ctx.fillStyle = '#ff3fa4'; ctx.fillRect(bx + bw - 2, by - 3, 3, 12);
    ctx.font = '8px "Press Start 2P", monospace'; ctx.fillStyle = '#e8e2ff';
    ctx.fillText(fmtTime(sim.time), bx, by + 20);
    ctx.textAlign = 'right'; ctx.fillText(Math.round(prog * 100) + '%', bx + bw, by + 20); ctx.textAlign = 'left';
    // ability
    const ax = 12, ay = VH - 46;
    ctx.fillStyle = 'rgba(18,10,36,.85)'; ctx.fillRect(ax - 2, ay - 2, 36, 36);
    spr('ability_' + charId, ax, ay, { ax: 0, ay: 0 });
    let ready = p.abilityCD <= 0;
    if (ch.abilityKey === 'thrusters') ready = !p.airJumped;
    if (!ready) {
      const k = ch.cooldown ? p.abilityCD / ch.cooldown : 1;
      ctx.fillStyle = 'rgba(18,10,36,.72)'; ctx.fillRect(ax, ay + Math.round(32 * (1 - k)), 32, Math.round(32 * k));
    } else { ctx.strokeStyle = ch.color; ctx.lineWidth = 1; ctx.strokeRect(ax - 1.5, ay - 1.5, 35, 35); }
    ctx.font = '8px "Press Start 2P", monospace'; ctx.fillStyle = '#ffd23a';
    ctx.fillText('W', ax + 40, ay + 12);
    ctx.fillStyle = ready ? ch.color : '#8a7fb0';
    ctx.fillText(ch.ability, ax + 40, ay + 26);
    // control reminder (GDD)
    ctx.textAlign = 'right'; ctx.fillStyle = 'rgba(232,226,255,.55)';
    ctx.fillText('SPC JUMP  S SLIDE  CLICK ATTACK  W ABILITY', VW - 12, VH - 12); ctx.textAlign = 'left';
  }
  const fmtTime = s => { const m = Math.floor(s / 60), r = s - m * 60; return m + ':' + r.toFixed(2).padStart(5, '0'); };

  // ------------------------------------------------------------------ screens (DOM)
  const $ = id => document.getElementById(id);
  function setScreen(s) {
    screen = s;
    document.querySelectorAll('.screen').forEach(el => { el.hidden = el.dataset.screen !== s; });
    $('touch').hidden = s !== 'play';
    if (s === 'select') renderSelect();
    if (s === 'title' || s === 'select') playMusic('music_menu');
  }

  // title
  $('btn-start').addEventListener('click', () => { initAudio(); sfx('ui_select'); setScreen('select'); });

  // select
  let selIdx = Math.max(0, CHAR_IDS.indexOf(save.selected));
  const selCursorId = () => CHAR_IDS[selIdx];
  function isUnlocked(id) { return save.unlocked.includes(id); }
  function renderSelect() {
    $('wallet').textContent = save.bytes;
    const grid = $('roster'); grid.innerHTML = '';
    CHAR_IDS.forEach((id, i) => {
      const c = CHARACTERS[id], un = isUnlocked(id);
      const card = document.createElement('button');
      card.className = 'card' + (i === selIdx ? ' sel' : '') + (un ? '' : ' locked');
      card.style.setProperty('--c', c.color);
      card.id = 'card-' + id;
      card.innerHTML = `<canvas width="48" height="48" data-prev="${id}"></canvas>
        <span class="cname">${c.name}</span><span class="ctitle">${c.title}</span>
        <span class="cab">${un ? c.ability : 'LOCKED'}</span>`;
      card.addEventListener('click', () => { selIdx = i; sfx('ui_move'); renderSelect(); });
      card.addEventListener('dblclick', () => confirmSelect());
      grid.appendChild(card);
    });
    const c = CHARACTERS[selCursorId()], un = isUnlocked(c.id);
    $('d-name').textContent = c.name; $('d-name').style.color = c.color;
    $('d-title').textContent = c.title.toUpperCase();
    $('d-bio').textContent = c.bio;
    $('d-ab').textContent = c.ability; $('d-abtext').textContent = c.abilityText;
    $('d-ab-icon').src = iconURI('ability_' + c.id);
    const stat = (v, max) => `<i style="width:${Math.round(v / max * 100)}%"></i>`;
    $('d-stats').innerHTML =
      `<div><span>HEALTH</span><b>${stat(c.hp, 4)}</b><em>${c.hp}</em></div>` +
      `<div><span>SPEED</span><b>${stat(c.speed - .8, .3)}</b><em>${c.speed.toFixed(2)}x</em></div>` +
      `<div><span>REACH</span><b>${stat(c.reach - 1.4, 1)}</b><em>${c.reach.toFixed(1)}m</em></div>` +
      `<div><span>COOLDOWN</span><b>${stat(c.cooldown ? 11 - c.cooldown : 11, 11)}</b><em>${c.cooldown ? c.cooldown + 's' : 'NONE'}</em></div>`;
    const btn = $('btn-go');
    if (un) { btn.textContent = 'RUN AS ' + c.name.split(' ')[0]; btn.disabled = false; btn.dataset.mode = 'go'; }
    else if (c.unlock.type === 'bytes') {
      btn.textContent = `UNLOCK  ${c.unlock.cost} BYTES`; btn.disabled = save.bytes < c.unlock.cost; btn.dataset.mode = 'buy';
    } else { btn.textContent = c.unlock.label; btn.disabled = true; btn.dataset.mode = 'none'; }
    $('d-lock').textContent = un ? '' : (c.unlock.type === 'bytes' ? `Spend ${c.unlock.cost} Data Bytes to recruit. You have ${save.bytes}.` : 'Clear Level 1 to recruit.');
    $('best').textContent = save.best.level01 ? `BEST  ${save.best.level01.rank}  ·  ${save.best.level01.score} PTS  ·  ${fmtTime(save.best.level01.time)}` : 'NO RECORD YET';
  }
  const iconCache = {};
  function iconURI(name) {
    if (iconCache[name]) return iconCache[name];
    const f = A.frames[name]; const c = document.createElement('canvas'); c.width = f[2]; c.height = f[3];
    c.getContext('2d').drawImage(atlas, f[0], f[1], f[2], f[3], 0, 0, f[2], f[3]);
    return (iconCache[name] = c.toDataURL());
  }
  let prevT = 0;
  function drawSelectPreviews(dt) {
    if (screen !== 'select') return;
    prevT += dt;
    document.querySelectorAll('canvas[data-prev]').forEach(c => {
      const id = c.dataset.prev, g = c.getContext('2d'); g.imageSmoothingEnabled = false;
      g.clearRect(0, 0, 48, 48);
      const sel = id === selCursorId();
      const name = sel ? `${id}_run_0${Math.floor(prevT * 12) % 8}` : `${id}_idle_0${Math.floor(prevT * 4) % 4}`;
      const f = A.frames[name];
      g.drawImage(atlas, f[0], f[1], 48, 48, 0, 0, 48, 48);
    });
  }
  function confirmSelect() {
    const id = selCursorId(), c = CHARACTERS[id];
    if (isUnlocked(id)) { save.selected = id; charId = id; writeSave(); sfx('ui_select'); startLevel(); }
    else if (c.unlock.type === 'bytes' && save.bytes >= c.unlock.cost) {
      save.bytes -= c.unlock.cost; save.unlocked.push(id); writeSave(); sfx('unlock'); renderSelect();
    } else sfx('denied');
  }
  $('btn-go').addEventListener('click', confirmSelect);
  $('btn-back').addEventListener('click', () => { sfx('ui_back'); setScreen('title'); });

  function menuKey(e) {
    if (screen === 'title' && (e.code === 'Enter' || e.code === 'Space')) { e.preventDefault(); sfx('ui_select'); setScreen('select'); }
    else if (screen === 'select') {
      if (e.code === 'ArrowRight' || e.code === 'KeyD') { selIdx = (selIdx + 1) % 4; sfx('ui_move'); renderSelect(); }
      else if (e.code === 'ArrowLeft' || e.code === 'KeyA') { selIdx = (selIdx + 3) % 4; sfx('ui_move'); renderSelect(); }
      else if (e.code === 'Enter' || e.code === 'Space') { e.preventDefault(); confirmSelect(); }
      else if (e.code === 'Escape') { sfx('ui_back'); setScreen('title'); }
    } else if (screen === 'paused' && (e.code === 'Escape' || e.code === 'KeyP')) resume();
    else if (screen === 'over') {
      if (e.code === 'Enter' || e.code === 'Space') { e.preventDefault(); retryCheckpoint(); }
    } else if (screen === 'results' && (e.code === 'Enter' || e.code === 'Space')) { e.preventDefault(); setScreen('select'); }
  }

  // pause
  function pauseGame() { setScreen('paused'); musicRate(1); if (AU.musicGain) AU.musicGain.gain.value = save.music * .2; sfx('ui_back'); }
  function resume() { if (AU.musicGain) AU.musicGain.gain.value = save.music * .55; last = performance.now(); setScreen('play'); for (const k in keys) keys[k] = false; }
  $('btn-resume').addEventListener('click', resume);
  $('btn-restart').addEventListener('click', () => { resume(); startLevel(); });
  $('btn-quit').addEventListener('click', () => { bankBytes(); sim = null; setScreen('select'); });
  $('vol-music').value = save.music; $('vol-sfx').value = save.sfx;
  $('vol-music').addEventListener('input', e => { save.music = +e.target.value; writeSave(); if (AU.musicGain) AU.musicGain.gain.value = save.music * .2; });
  $('vol-sfx').addEventListener('input', e => { save.sfx = +e.target.value; writeSave(); if (AU.sfxGain) AU.sfxGain.gain.value = save.sfx * .7; sfx('byte', .6); });

  // game over
  function showOver(cause) {
    $('over-cause').textContent = cause === 'fall' ? 'You fell from the rooftops.' : 'Security took you down.';
    $('over-cp').textContent = sim.checkpointX > LEVEL.spawnX ? 'RETRY FROM CHECKPOINT' : 'RETRY';
    setScreen('over');
  }
  function retryCheckpoint() { sfx('ui_select'); setScreen('play'); sim.respawn(); handleEvents(); cam.x = sim.p.x - 5.5; lastGroundY = sim.p.y; cam.y = sim.p.y + 2.4; }
  $('over-cp').addEventListener('click', retryCheckpoint);
  $('over-restart').addEventListener('click', () => { sfx('ui_select'); startLevel(); });
  $('over-quit').addEventListener('click', () => { bankBytes(); sim = null; setScreen('select'); });

  function bankBytes() {
    if (!sim || sim.banked) return;
    save.bytes += sim.stats.bytes; sim.banked = true; writeSave();
  }

  // results
  function onWin() {
    sfx('level_complete', 1);
    playMusic('music_menu', true);
    setTimeout(() => {
      const s = sim.stats, rank = sim.rank(), score = sim.score();
      const prevBest = save.best.level01;
      const newUnlocks = [];
      bankBytes();
      if (!save.cleared.level01) {
        save.cleared.level01 = true;
        if (!save.unlocked.includes('nova')) { save.unlocked.push('nova'); newUnlocks.push('NOVA REYES joined the crew'); }
      }
      const order = 'CBAS';
      if (!prevBest || score > prevBest.score) save.best.level01 = { score, rank: order.indexOf(rank) > order.indexOf(prevBest ? prevBest.rank : 'C') || !prevBest ? rank : prevBest.rank, time: sim.time };
      writeSave();
      for (const id of ['jax', 'lin']) {
        const c = CHARACTERS[id];
        if (!save.unlocked.includes(id) && save.bytes >= c.unlock.cost) newUnlocks.push(`${c.name} can be recruited (${c.unlock.cost} bytes)`);
      }
      $('r-rank').textContent = rank;
      $('r-rank').dataset.rank = rank;
      $('r-rows').innerHTML = [
        ['DATA BYTES', `${s.bytes} / ${sim.totalBytes}`], ['DRONES DOWN', s.drones], ['HITS TAKEN', s.hits],
        ['RETRIES', s.deaths], ['TIME', fmtTime(sim.time)], ['SCORE', score + (prevBest && score > prevBest.score ? '  NEW BEST' : '')],
      ].map(([a, b]) => `<div><span>${a}</span><b>${b}</b></div>`).join('');
      $('r-unlocks').innerHTML = newUnlocks.map(u => `<li>${u}</li>`).join('');
      $('r-wallet').textContent = save.bytes;
      setScreen('results');
    }, 1400);
  }
  $('r-next').addEventListener('click', () => { sfx('ui_select'); sim = null; setScreen('select'); });
  $('r-again').addEventListener('click', () => { sfx('ui_select'); startLevel(); });

  // boot
  const boot = () => { fitCanvas(); setScreen('title'); requestAnimationFrame(frame); };
  if (document.fonts && document.fonts.load) {
    Promise.all([document.fonts.load('10px "Press Start 2P"'), document.fonts.load('16px "VT323"')]).finally(boot);
  } else boot();
  window.__dh = { get sim() { return sim; }, startLevel, setScreen };
})();
