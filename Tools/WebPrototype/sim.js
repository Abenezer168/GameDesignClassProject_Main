// The Data Heist - deterministic gameplay simulation (no rendering).
// Shared by the browser prototype and the headless playtest bot.
// World units, y-up. The Unity C# port mirrors these rules 1:1.
(function (root) {
  'use strict';

  const CHARACTERS = {
    kai: {
      id: 'kai', name: 'KAI OKAFOR', title: 'The Courier', gender: 'm',
      hp: 3, speed: 1.0, jump: 1.0, reach: 1.8, color: '#2ff3ff',
      ability: 'KICK THRUSTERS', abilityKey: 'thrusters', cooldown: 0,
      abilityText: 'Fire boot thrusters for a second jump in mid-air. Recharges every landing.',
      bio: 'Fastest courier in Sector 7. Never misses a drop.',
      unlock: { type: 'free' },
    },
    nova: {
      id: 'nova', name: 'NOVA REYES', title: 'The Runner', gender: 'f',
      hp: 3, speed: 1.06, jump: 1.0, reach: 2.0, color: '#ff3fa4',
      ability: 'PHASE DASH', abilityKey: 'dash', cooldown: 3.0,
      abilityText: 'Burst forward in a phased sprint. Pass through hazards and clear huge gaps.',
      bio: 'Ex-corporate parkour champion. Quit the day they put a chip in her.',
      unlock: { type: 'level', level: 'level01', label: 'CLEAR LEVEL 1' },
    },
    jax: {
      id: 'jax', name: 'JAX MERCER', title: 'The Breaker', gender: 'm',
      hp: 4, speed: 0.95, jump: 1.0, reach: 2.3, color: '#38b6ff',
      ability: 'EMP PULSE', abilityKey: 'emp', cooldown: 8.0,
      abilityText: 'Overload nearby circuits. Fries every drone and shuts down lasers for 4 seconds.',
      bio: 'Former security engineer. Built half the drones. Now he breaks them.',
      unlock: { type: 'bytes', cost: 300, label: '300 BYTES' },
    },
    lin: {
      id: 'lin', name: 'LIN SATO', title: 'The Ghost', gender: 'f',
      hp: 3, speed: 1.0, jump: 1.0, reach: 1.9, color: '#7dff6a',
      ability: 'TIME GLITCH', abilityKey: 'glitch', cooldown: 10.0,
      abilityText: 'Hack the city clock. Time slows to half speed and nearby Data Bytes fly to you.',
      bio: 'The hacker who found the shutdown key. Nobody has seen her face.',
      unlock: { type: 'bytes', cost: 500, label: '500 BYTES' },
    },
  };

  const HAZ = {
    can: { w: 0.6, h: 0.8, oy: 0 },
    barrier: { w: 1.6, h: 2.5, oy: 0.75 },
    pad: { w: 1.0, h: 0.3, oy: 0 },
  };

  function aabb(ax0, ay0, ax1, ay1, bx0, by0, bx1, by1) {
    return ax0 < bx1 && ax1 > bx0 && ay0 < by1 && ay1 > by0;
  }

  class Sim {
    constructor(level, charId) {
      this.L = level;
      this.P = level.physics;
      this.ch = CHARACTERS[charId];
      this.events = [];
      this.time = 0;
      this.ents = level.entities.map(e => Object.assign({ alive: true }, e))
        .sort((a, b) => a.x - b.x);
      this.ents.forEach((e, i) => { e.id = i; e.ox = e.x; e.oy = e.y; e.baseY = e.y; e.cd = 0.8; e.phase = (e.x * 1.7) % 6.28; });
      this.totalBytes = this.ents.filter(e => e.t === 'byte').length;
      this.bolts = [];
      this.stats = { bytes: 0, hits: 0, deaths: 0, drones: 0, shard: false };
      this.checkpointX = level.spawnX;
      this.cpSnap = { bytes: 0, drones: 0 };
      this.empT = 0; this.slowT = 0; this.magnetT = 0;
      this.state = 'run';    // run | dead | won
      this.spawn(level.spawnX);
    }

    // index of first entity whose original x >= x (entities sorted by x)
    lower(x) {
      let lo = 0, hi = this.ents.length;
      while (lo < hi) { const m = (lo + hi) >> 1; if (this.ents[m].ox < x) lo = m + 1; else hi = m; }
      return lo;
    }

    groundAt(x) {
      for (const r of this.L.roofs) if (x >= r.x0 && x <= r.x1) return r.y;
      return -Infinity;
    }

    spawn(x) {
      const p = {
        x, y: this.groundAt(x), vy: 0, onGround: true, sliding: false, slideT: 0,
        attackT: -1, attackCD: 0, inv: 0, hp: this.ch.hp, airJumped: false,
        dashT: 0, abilityCD: 0, coyote: 0, buffer: 0, cut: false, pad: false,
        diving: false, slideQueued: false, hurtT: 0, landT: 0, wall: false,
      };
      this.p = p;
    }

    emit(type, data) { this.events.push(Object.assign({ type, t: this.time }, data || {})); }

    speedNow() {
      const p = this.p;
      let s = this.P.runSpeed * this.ch.speed;
      for (const z of this.L.zones) if (p.x >= z.x0 && p.x <= z.x1) s *= z.speed;
      if (p.dashT > 0) s *= 2.6;
      if (p.wall) s = 0;
      return s;
    }

    inZoneAlarm() {
      for (const z of this.L.zones) if (z.alarm && this.p.x >= z.x0 && this.p.x <= z.x1) return true;
      return false;
    }

    height() { return this.p.sliding ? this.P.slideHeight : this.P.standHeight; }

    // Main tick. `inp`: {jumpPressed, jumpHeld, slidePressed, slideHeld, attackPressed, abilityPressed}
    step(dt, inp) {
      if (this.state !== 'run') return;
      const P = this.P, p = this.p, ch = this.ch;
      this.time += dt;
      // --- timers
      p.inv = Math.max(0, p.inv - dt);
      p.attackCD = Math.max(0, p.attackCD - dt);
      p.abilityCD = Math.max(0, p.abilityCD - dt);
      p.coyote = Math.max(0, p.coyote - dt);
      p.buffer = Math.max(0, p.buffer - dt);
      p.hurtT = Math.max(0, p.hurtT - dt);
      p.landT = Math.max(0, p.landT - dt);
      this.empT = Math.max(0, this.empT - dt);
      this.magnetT = Math.max(0, this.magnetT - dt);
      if (p.attackT >= 0) { p.attackT += dt; if (p.attackT > 0.28) p.attackT = -1; }
      if (p.dashT > 0) { p.dashT -= dt; if (p.dashT <= 0) { p.dashT = 0; this.emit('dashEnd'); } }

      if (inp.jumpPressed) p.buffer = P.jumpBuffer;

      // --- ability
      if (inp.abilityPressed) this.useAbility();

      // --- jump
      if (p.buffer > 0 && (p.onGround || p.coyote > 0) && !p.wall) {
        p.vy = P.jumpVelocity * ch.jump;
        p.onGround = false; p.coyote = 0; p.buffer = 0; p.cut = false; p.pad = false;
        if (p.sliding) this.endSlide(true);
        this.emit('jump');
      } else if (inp.jumpPressed && !p.onGround && p.coyote <= 0 && ch.abilityKey === 'thrusters' && !p.airJumped) {
        this.airJump();
      }
      if (!inp.jumpHeld && p.vy > 0 && !p.cut && !p.pad && !p.onGround) {
        p.vy *= P.jumpCutMultiplier; p.cut = true;
      }

      // --- slide / dive
      if (inp.slidePressed) {
        if (p.onGround && !p.sliding) this.startSlide();
        else if (!p.onGround && !p.diving && p.dashT <= 0) {
          p.diving = true; p.vy = Math.min(p.vy, -P.maxFall * 0.75); p.slideQueued = true; this.emit('dive');
        }
      }
      if (p.sliding) {
        p.slideT += dt;
        const blocked = this.ceilingBlocked();
        const keep = p.slideT < P.slideMin || (inp.slideHeld && p.slideT < P.slideMax) || blocked;
        if (!keep) this.endSlide(false);
      }

      // --- attack
      if (inp.attackPressed && p.attackCD <= 0) {
        p.attackT = 0; p.attackCD = 0.32; this.emit('attack');
      }

      // --- physics
      const spd = this.speedNow();
      if (p.dashT > 0) { p.vy = Math.max(p.vy, 0); p.vy = 0; }
      else { p.vy -= P.gravity * dt; if (p.vy < -P.maxFall) p.vy = -P.maxFall; }
      const prevY = p.y;
      const prevX = p.x;
      let nx = p.x + spd * dt;
      let ny = p.y + p.vy * dt;
      const hw = P.width / 2;

      // walls: running into the side of a taller roof
      p.wall = false;
      for (const r of this.L.roofs) {
        if (prevX + hw <= r.x0 + 1e-6 && nx + hw > r.x0 && ny < r.y - 0.05) {
          nx = r.x0 - hw; p.wall = true; break;
        }
      }
      // landing
      let landed = false;
      if (p.vy <= 0) {
        let best = -Infinity;
        const surfaces = this.L.roofs.concat(this.L.platforms);
        for (const s of surfaces) {
          if (nx + hw > s.x0 && nx - hw < s.x1 && prevY >= s.y - 0.02 && ny <= s.y) {
            if (s.y > best) best = s.y;
          }
        }
        if (best > -Infinity) {
          ny = best; landed = true;
        }
      }
      const wasGround = p.onGround;
      p.x = nx; p.y = ny;
      if (landed) {
        if (!wasGround) {
          p.landT = 0.12; this.emit('land', { hard: p.vy < -18 });
          p.airJumped = false; p.cut = false; p.pad = false;
          if (p.slideQueued || p.diving) { p.diving = false; p.slideQueued = false; if (inp.slideHeld || true) this.startSlide(); }
        }
        p.vy = 0; p.onGround = true;
      } else {
        if (wasGround) p.coyote = P.coyoteTime;
        p.onGround = false;
        if (p.sliding && p.slideT > 0.05) this.endSlide(false);
      }

      this.updateEntities(dt);
      this.collide();

      // fall death
      if (p.y < this.L.killY) this.die('fall');
      if (p.x >= this.L.exitX && this.state === 'run') {
        this.state = 'won'; this.emit('win');
      }
    }

    startSlide() {
      const p = this.p; p.sliding = true; p.slideT = 0; this.emit('slide');
    }
    endSlide(force) {
      const p = this.p;
      if (!force && this.ceilingBlocked()) return;
      p.sliding = false; p.slideT = 0;
    }
    ceilingBlocked() {
      const p = this.p, P = this.P, hw = P.width / 2;
      for (let i = this.lower(p.x - 3); i < this.ents.length; i++) {
        const e = this.ents[i];
        if (e.ox > p.x + 3) break;
        if (!e.alive) continue;
        const b = this.boxOf(e);
        if (!b || !(e.t === 'barrier' || (e.t === 'laser' && e.mode === 'high' && this.empT <= 0))) continue;
        if (aabb(p.x - hw, p.y, p.x + hw + 0.3, p.y + P.standHeight, b[0], b[1], b[2], b[3])) return true;
      }
      return false;
    }

    airJump() {
      const p = this.p;
      p.vy = 14.0; p.airJumped = true; p.cut = false; p.pad = false; p.diving = false; p.slideQueued = false;
      this.emit('airJump');
    }

    useAbility() {
      const p = this.p, ch = this.ch;
      switch (ch.abilityKey) {
        case 'thrusters':
          if (!p.onGround && p.coyote <= 0 && !p.airJumped) this.airJump();
          else if (p.onGround) { p.buffer = this.P.jumpBuffer; }
          return;
        case 'dash':
          if (p.abilityCD > 0) return this.emit('abilityDenied');
          p.dashT = 0.34; p.abilityCD = ch.cooldown; p.vy = 0; p.diving = false;
          if (p.sliding) this.endSlide(true);
          this.emit('dash');
          return;
        case 'emp':
          if (p.abilityCD > 0) return this.emit('abilityDenied');
          p.abilityCD = ch.cooldown; this.empT = 4.0;
          for (const e of this.ents) {
            if (!e.alive) continue;
            if ((e.t === 'drone' || e.t === 'gunner') && Math.abs(e.x - p.x) < 11) this.killDrone(e, 'emp');
          }
          this.bolts = this.bolts.filter(b => Math.abs(b.x - p.x) > 11);
          this.emit('emp');
          return;
        case 'glitch':
          if (p.abilityCD > 0) return this.emit('abilityDenied');
          p.abilityCD = ch.cooldown; this.slowT = 3.0; this.magnetT = 4.0;
          this.emit('glitch');
          return;
      }
    }

    boxOf(e) {
      switch (e.t) {
        case 'can': return [e.x - 0.3, e.y, e.x + 0.3, e.y + 0.8];
        case 'barrier': return [e.x - 0.8, e.y + 0.75, e.x + 0.8, e.y + 3.25];
        case 'laser':
          return e.mode === 'low' ? [e.x - 0.15, e.y, e.x + 0.15, e.y + e.h] : [e.x - 0.15, e.y, e.x + 0.15, e.y + e.h];
        case 'drone': case 'gunner': return [e.x - 0.4, e.y - 0.3, e.x + 0.4, e.y + 0.3];
        case 'pad': return [e.x - 0.5, e.y, e.x + 0.5, e.y + 0.35];
        case 'byte': return [e.x - 0.3, e.y - 0.3, e.x + 0.3, e.y + 0.3];
        case 'repair': return [e.x - 0.35, e.y - 0.35, e.x + 0.35, e.y + 0.35];
        case 'shard': return [e.x - 0.5, e.y - 0.7, e.x + 0.5, e.y + 0.7];
      }
      return null;
    }

    updateEntities(dt) {
      const p = this.p;
      for (let i = this.lower(p.x - 8); i < this.ents.length; i++) {
        const e = this.ents[i];
        if (e.ox > p.x + 22) break;
        if (!e.alive) continue;
        if ((e.t === 'drone' || e.t === 'gunner') && e.move === 'bob') {
          e.y = e.baseY + Math.sin(this.time * 3 + e.phase) * 0.18;
        }
        if (e.t === 'gunner') {
          const dx = e.x - p.x;
          if (dx < 15 && dx > 6) {
            e.cd -= dt;
            if (e.cd <= 0) {
              e.cd = 1.45;
              // always aimed at chest height of a runner standing on the roof: slide, jump or slash it
              const ty = this.groundAt(e.x) + 0.8;
              this.bolts.push({ x: e.x - 0.5, y: ty, vx: -7.5, alive: true, born: this.time, sy: e.y });
              this.emit('shoot', { x: e.x, y: e.y });
            }
          }
        }
        if (e.t === 'byte' && this.magnetT > 0) {
          const dx = p.x - e.x, dy = (p.y + 0.6) - e.y, d = Math.hypot(dx, dy);
          if (d < 4.5 && d > 0.01) {
            const v = 14 * dt;
            e.x += dx / d * Math.min(v, d); e.y += dy / d * Math.min(v, d);
          }
        }
      }
      for (const b of this.bolts) {
        b.x += b.vx * dt;
        // home slightly toward firing height (straight shot from muzzle to target height)
        const age = this.time - b.born;
        if (age < 0.25) b.y = b.sy + (b.y - b.sy);
      }
      this.bolts = this.bolts.filter(b => b.alive && b.x > p.x - 20);
    }

    collide() {
      const p = this.p, P = this.P, hw = P.width / 2, h = this.height();
      const px0 = p.x - hw, px1 = p.x + hw, py0 = p.y, py1 = p.y + h;
      const dashing = p.dashT > 0;
      // attack hitbox
      const atk = p.attackT >= 0 && p.attackT <= 0.17;
      const ax0 = p.x + 0.05, ax1 = p.x + this.ch.reach, ay0 = p.y - 0.1, ay1 = p.y + 1.8;
      for (let i = this.lower(p.x - 6); i < this.ents.length; i++) {
        const e = this.ents[i];
        if (e.ox > p.x + 6) break;
        if (!e.alive) continue;
        const b = this.boxOf(e);
        if (!b) {
          if (e.t === 'checkpoint' && p.x >= e.x) {
            e.alive = false; this.checkpointX = e.x;
            this.cpSnap = { bytes: this.stats.bytes, drones: this.stats.drones };
            this.emit('checkpoint', { x: e.x });
          }
          continue;
        }
        if (b[0] > p.x + 3 || b[2] < p.x - 3) continue;
        const touching = aabb(px0, py0, px1, py1, b[0], b[1], b[2], b[3]);
        switch (e.t) {
          case 'byte':
            if (aabb(px0 - 0.1, py0, px1 + 0.1, py1 + 0.15, b[0], b[1], b[2], b[3])) {
              e.alive = false; this.stats.bytes++; this.emit('byte', { x: e.x, y: e.y });
            }
            break;
          case 'repair':
            if (touching) { e.alive = false; p.hp = Math.min(this.ch.hp, p.hp + 1); this.emit('repair', { x: e.x, y: e.y }); }
            break;
          case 'shard':
            if (touching) { e.alive = false; this.stats.shard = true; this.emit('shard', { x: e.x, y: e.y }); }
            break;
          case 'pad':
            if (touching && p.vy <= 0 && !dashing) {
              p.vy = P.padVelocity; p.onGround = false; p.pad = true; p.cut = true; p.airJumped = false;
              if (p.sliding) this.endSlide(true);
              this.emit('pad', { x: e.x, y: e.y });
            }
            break;
          case 'drone': case 'gunner': {
            if (atk && aabb(ax0, ay0, ax1, ay1, b[0], b[1], b[2], b[3])) { this.killDrone(e, 'slash'); break; }
            if (!touching) break;
            if (dashing) { this.killDrone(e, 'dash'); break; }
            if (p.vy < 0 && p.y >= e.y - 0.05) {
              this.killDrone(e, 'stomp'); p.vy = P.stompBounce; p.airJumped = false; p.cut = true; p.pad = true;
              break;
            }
            this.killDrone(e, 'crash');
            this.hurt();
            break;
          }
          case 'can': case 'barrier':
            if (touching && !dashing) {
              if (e.t === 'can') { e.alive = false; this.emit('canHit', { x: e.x, y: e.y }); }
              this.hurt();
            }
            break;
          case 'laser':
            if (touching && !dashing && this.empT <= 0) this.hurt();
            break;
        }
      }
      for (const bo of this.bolts) {
        if (!bo.alive) continue;
        if (atk && aabb(ax0, ay0, ax1, ay1, bo.x - 0.25, bo.y - 0.12, bo.x + 0.25, bo.y + 0.12)) {
          bo.alive = false; this.emit('deflect', { x: bo.x, y: bo.y }); continue;
        }
        if (aabb(px0, py0, px1, py1, bo.x - 0.25, bo.y - 0.12, bo.x + 0.25, bo.y + 0.12)) {
          bo.alive = false;
          if (!dashing) this.hurt(); else this.emit('deflect', { x: bo.x, y: bo.y });
        }
      }
    }

    killDrone(e, how) {
      e.alive = false; this.stats.drones++;
      this.emit('droneDown', { x: e.x, y: e.y, how });
    }

    hurt() {
      const p = this.p;
      if (p.inv > 0 || this.state !== 'run') return;
      p.hp -= 1; p.inv = 1.25; p.hurtT = 0.35; this.stats.hits++;
      this.emit('hurt', { hp: p.hp });
      if (p.hp <= 0) this.die('hp');
    }

    die(cause) {
      if (this.state !== 'run') return;
      this.state = 'dead'; this.stats.deaths++;
      this.emit('dead', { cause });
    }

    // restore everything past the checkpoint and respawn there
    respawn() {
      const cx = this.checkpointX;
      for (const e of this.ents) {
        if (e.t === 'checkpoint' || e.ox <= cx + 0.01) continue;
        e.x = e.ox; e.y = e.oy; e.alive = true; e.cd = 0.8;
      }
      this.stats.bytes = this.ents.filter(e => e.t === 'byte' && !e.alive).length;
      this.stats.drones = this.cpSnap.drones;
      this.stats.shard = false;
      this.bolts = []; this.empT = 0; this.slowT = 0; this.magnetT = 0;
      this.state = 'run';
      this.spawn(cx);
      this.p.inv = 1.0;
      this.emit('respawn', { x: cx });
    }

    rank() {
      const s = this.stats, pct = s.bytes / this.totalBytes;
      if (pct >= 0.85 && s.hits === 0 && s.deaths === 0) return 'S';
      if (pct >= 0.7 && s.hits <= 2 && s.deaths === 0) return 'A';
      if (pct >= 0.45) return 'B';
      return 'C';
    }

    score() {
      const s = this.stats;
      const timeBonus = Math.max(0, Math.round((this.L.parTime * 1.3 - this.time) * 20));
      return s.bytes * 10 + (s.shard ? 500 : 0) + s.drones * 25 + timeBonus + (s.hits === 0 ? 300 : 0) - s.deaths * 100;
    }

    clone() {
      const c = Object.create(Sim.prototype);
      c.L = this.L; c.P = this.P; c.ch = this.ch; c.events = [];
      c.time = this.time; c.totalBytes = this.totalBytes;
      c.ents = this.ents.map(e => Object.assign({}, e));
      c.bolts = this.bolts.map(b => Object.assign({}, b));
      c.stats = Object.assign({}, this.stats);
      c.checkpointX = this.checkpointX; c.cpSnap = Object.assign({}, this.cpSnap);
      c.empT = this.empT; c.slowT = this.slowT; c.magnetT = this.magnetT;
      c.state = this.state; c.p = Object.assign({}, this.p);
      return c;
    }
  }

  const api = { Sim, CHARACTERS };
  if (typeof module !== 'undefined' && module.exports) module.exports = api;
  else root.DataHeist = api;
})(typeof window !== 'undefined' ? window : globalThis);
