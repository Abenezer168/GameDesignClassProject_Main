// Headless playtest bot: beam search over inputs to prove Level 1 is
// clearable by every runner with zero hits and no ability use, and to
// measure timing windows. Usage: node bot.js level01.json [charId] [--ability]
const fs = require('fs');
const { Sim, CHARACTERS } = require('./sim.js');

const level = JSON.parse(fs.readFileSync(process.argv[2], 'utf8'));
const chars = process.argv[3] && !process.argv[3].startsWith('--') ? [process.argv[3]] : Object.keys(CHARACTERS);
const DT = 1 / 120, TICKS = 9;   // decide every 75 ms (human-ish)
const BEAM = 24;

const ACTIONS = [
  { name: 'idle' },
  { name: 'jump', press: 'jump', holdJ: true },
  { name: 'holdJ', holdJ: true },
  { name: 'tapJ', press: 'jump' },
  { name: 'slide', press: 'slide', holdS: true },
  { name: 'holdS', holdS: true },
  { name: 'attack', press: 'attack' },
];

function run(cid) {
  let beam = [{ sim: new Sim(level, cid), path: [] }];
  let decisions = 0, minAlt = Infinity;
  const tightSpots = [];
  while (true) {
    const next = [];
    for (const node of beam) {
      for (const a of ACTIONS) {
        const s = node.sim.clone();
        let ok = true;
        for (let k = 0; k < TICKS; k++) {
          const inp = {
            jumpPressed: k === 0 && a.press === 'jump', jumpHeld: !!a.holdJ,
            slidePressed: k === 0 && a.press === 'slide', slideHeld: !!a.holdS,
            attackPressed: k === 0 && a.press === 'attack', abilityPressed: false,
          };
          s.step(DT, inp);
          if (s.stats.hits > 0 || s.state === 'dead') { ok = false; break; }
          if (s.state === 'won') break;
        }
        if (!ok) continue;
        next.push({ sim: s, path: node.path.length < 4000 ? node.path.concat([a.name]) : node.path, act: a.name });
      }
    }
    decisions++;
    if (!next.length) {
      const s = beam[0].sim;
      return { cid, ok: false, x: s.p.x.toFixed(2), t: s.time.toFixed(2) };
    }
    const won = next.find(n => n.sim.state === 'won');
    if (won) {
      const s = won.sim;
      return { cid, ok: true, time: s.time.toFixed(1), bytes: s.stats.bytes + '/' + s.totalBytes, drones: s.stats.drones, rank: s.rank(), score: s.score(), tight: tightSpots.slice(0, 12) };
    }
    // score: progress, bytes, prefer not sliding/attacking needlessly (diversity via dedupe)
    next.sort((a, b) => (b.sim.stats.bytes * 3 + b.sim.p.x) - (a.sim.stats.bytes * 3 + a.sim.p.x) || (a.act === 'idle' ? -1 : 1));
    const seen = new Set();
    beam = [];
    for (const n of next) {
      const p = n.sim.p;
      const key = [p.x.toFixed(1), p.y.toFixed(1), p.vy.toFixed(0), p.sliding, p.attackCD > 0, n.sim.stats.bytes].join('|');
      if (seen.has(key)) continue;
      seen.add(key); beam.push(n);
      if (beam.length >= BEAM) break;
    }
    // how many distinct survivors? few => tight timing window here
    if (seen.size <= 3) tightSpots.push(beam[0].sim.p.x.toFixed(0));
  }
}

for (const c of chars) {
  const t0 = Date.now();
  const r = run(c);
  console.log(JSON.stringify(r), ((Date.now() - t0) / 1000).toFixed(1) + 's');
}
