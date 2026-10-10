using System.Collections.Generic;
using UnityEngine;

namespace DataHeist
{
    /// <summary>
    /// Presentation for every simulated entity and bolt. Reads RunnerSim each frame;
    /// never writes gameplay state.
    /// </summary>
    public class EntityViews : MonoBehaviour
    {
        class View
        {
            public SimEntity e;
            public GameObject go;
            public SpriteRenderer sr;
            public SpriteRenderer extra;   // glow / targeting line / emitter
            public SpriteRenderer beam;
            public SpriteRenderer emitter;
            public Sprite[] frames;
            public float fps;
        }

        RunnerSim _sim;
        readonly List<View> _views = new List<View>();
        readonly List<SpriteRenderer> _boltPool = new List<SpriteRenderer>();
        Sprite[] _boltFrames, _beamFrames;
        Sprite _cpOn, _cpOff, _emOn, _emOff;
        Sprite[] _padFrames;
        Sprite[] _sniperFrames, _sniperBolt, _shockOn;
        Sprite _shockOff, _signHang, _signFallen;
        Sprite[] _turretFrames;
        Sprite _pressHead, _pressShaft, _machineHang, _machineFallen;
        Sprite _doorClosed, _doorOpen, _bossDead, _bossCharge, _bossSlam;
        Sprite[] _bossIdle, _orbFrames, _waveFrames;
        float _bossFlash;

        /// <summary>White flash on the Guardian when a reflected shot lands (called by LevelController).</summary>
        public void FlashBoss() => _bossFlash = 0.12f;

        public void Init(RunnerSim sim)
        {
            _sim = sim;
            _boltFrames = SpriteLibrary.Anim("Enemies/bolt");
            _beamFrames = SpriteLibrary.Anim("Obstacles/laser_beam");
            _cpOn = SpriteLibrary.Get("Obstacles/checkpoint_on");
            _cpOff = SpriteLibrary.Get("Obstacles/checkpoint_off");
            _emOn = SpriteLibrary.Get("Obstacles/laser_emitter_on");
            _emOff = SpriteLibrary.Get("Obstacles/laser_emitter_off");
            _padFrames = SpriteLibrary.Anim("Obstacles/jumppad");
            if (System.Array.Exists(sim.ents, e => e.t == "boss" || e.t == "vault"))
            {
                _doorClosed = SpriteLibrary.Get("Obstacles/vault_door_closed");
                _doorOpen = SpriteLibrary.Get("Obstacles/vault_door_open");
                _bossIdle = SpriteLibrary.Anim("Enemies/guardian_idle");
                _bossCharge = SpriteLibrary.Get("Enemies/guardian_charge_00");
                _bossSlam = SpriteLibrary.Get("Enemies/guardian_slam_00");
                _bossDead = SpriteLibrary.Get("Enemies/guardian_dead_00");
                _orbFrames = SpriteLibrary.Anim("Enemies/boss_orb");
                _waveFrames = SpriteLibrary.Anim("Enemies/shockwave");
            }
            if (System.Array.Exists(sim.ents, e => e.t == "turret" || e.t == "press" || e.kind == "machine"))
            {
                _turretFrames = SpriteLibrary.Anim("Enemies/turret");
                _pressHead = SpriteLibrary.Get("Obstacles/crusher_head");
                _pressShaft = SpriteLibrary.Get("Obstacles/crusher_shaft");
                _machineHang = SpriteLibrary.Get("Obstacles/machine_hang");
                _machineFallen = SpriteLibrary.Get("Obstacles/machine_fallen");
            }
            if (System.Array.Exists(sim.ents, e => e.t == "sniper" || e.t == "shock" || e.t == "sign"))
            {
                _sniperFrames = SpriteLibrary.Anim("Enemies/sniper_window");
                _sniperBolt = SpriteLibrary.Anim("Enemies/sniper_bolt");
                _shockOn = SpriteLibrary.Anim("Obstacles/shock_on");
                _shockOff = SpriteLibrary.Get("Obstacles/shock_off");
                _signHang = SpriteLibrary.Get("Obstacles/sign_hang");
                _signFallen = SpriteLibrary.Get("Obstacles/sign_fallen");
            }
            foreach (var e in sim.ents) Create(e);
        }

        void Create(SimEntity e)
        {
            var v = new View { e = e, go = new GameObject($"{e.t}_{e.id}") };
            v.go.transform.SetParent(transform, false);
            v.sr = v.go.AddComponent<SpriteRenderer>();
            v.sr.sortingOrder = DH.SortProps;
            float px = 1f / DH.PPU;
            switch (e.t)
            {
                case "byte":
                    v.frames = SpriteLibrary.Anim("Pickups/byte"); v.fps = 10; v.sr.sortingOrder = DH.SortPickups; break;
                case "can": v.sr.sprite = SpriteLibrary.Get("Obstacles/garbage_can"); break;
                case "barrier": v.sr.sprite = SpriteLibrary.Get("Obstacles/barrier"); v.sr.sortingOrder = DH.SortProps + 1; break;
                case "pad": v.sr.sprite = _padFrames.Length > 0 ? _padFrames[0] : null; break;
                case "drone":
                case "gunner":
                    v.frames = SpriteLibrary.Anim(e.t == "gunner" ? "Enemies/drone_gunner" : "Enemies/drone_scout");
                    v.fps = 16; v.sr.sortingOrder = DH.SortEnemies;
                    if (e.t == "gunner")
                    {
                        v.extra = SR.Box("TargetLine", v.go.transform, DH.Orange, DH.SortEnemies - 1, -4.6f, 0, 4.1f, px);
                        v.extra.enabled = false;
                    }
                    break;
                case "repair": v.frames = SpriteLibrary.Anim("Pickups/repair"); v.fps = 4; v.sr.sortingOrder = DH.SortPickups; break;
                case "shard":
                    v.frames = SpriteLibrary.Anim("Pickups/shard"); v.fps = 6; v.sr.sortingOrder = DH.SortPickups + 1;
                    v.extra = SR.Make("Glow", v.go.transform, SpriteLibrary.Get("FX/glow"), DH.SortPickups, Vector3.zero);
                    v.extra.color = new Color(DH.Pink.r, DH.Pink.g, DH.Pink.b, 0.5f);
                    v.extra.transform.localScale = Vector3.one * 1.6f;
                    break;
                case "checkpoint": v.sr.sprite = _cpOff; break;
                case "laser":
                    v.sr.sprite = null;
                    v.emitter = SR.Make("Emitter", v.go.transform, _emOn, DH.SortProps + 1, new Vector3(0, e.h - 2f / DH.PPU, 0));
                    v.beam = SR.Tiled("Beam", v.go.transform, _beamFrames.Length > 0 ? _beamFrames[0] : null, DH.SortProps,
                        Vector3.zero, new Vector2(0.5f, e.h));
                    if (e.mode == "high")
                        v.extra = SR.Box("Cable", v.go.transform, DH.Hex("241e38"), DH.SortProps - 1, -px, e.h + 12 * px, 2 * px, 20f);
                    else
                        v.extra = SR.Box("Base", v.go.transform, DH.Hex("3b3456"), DH.SortProps - 1, -5 * px, -3 * px, 10 * px, 3 * px);
                    break;
                case "exit":
                    v.go.SetActive(false); break;
                case "sniper":
                    // the nest sits in a penthouse wall behind the roof; the laser sight is a thin box
                    v.sr.sortingOrder = DH.SortDecor + 3;
                    v.extra = SR.Box("LaserSight", v.go.transform, DH.Red, DH.SortBolts - 1, -20f, 0, 19.3f, px);
                    v.extra.enabled = false;
                    v.beam = SR.Make("Dot", v.go.transform, SpriteLibrary.Get("FX/glow"), DH.SortBolts, Vector3.zero);
                    v.beam.color = DH.Red;
                    v.beam.transform.localScale = Vector3.one * 0.35f;
                    v.beam.enabled = false;
                    break;
                case "shock":
                    v.sr.drawMode = SpriteDrawMode.Tiled;
                    v.sr.sprite = _shockOff;
                    v.sr.size = new Vector2(e.w, 0.25f);
                    v.sr.sortingOrder = DH.SortProps - 1;
                    v.extra = SR.Make("Glow", v.go.transform, SpriteLibrary.Get("FX/glow"), DH.SortProps - 2, new Vector3(e.w / 2f, 0.1f, 0));
                    v.extra.color = new Color(DH.Cyan.r, DH.Cyan.g, DH.Cyan.b, 0.35f);
                    v.extra.transform.localScale = new Vector3(e.w * 0.9f, 0.6f, 1);
                    break;
                case "fragment":
                    v.frames = SpriteLibrary.Anim("Pickups/fragment"); v.fps = 8; v.sr.sortingOrder = DH.SortPickups + 1;
                    v.extra = SR.Make("Glow", v.go.transform, SpriteLibrary.Get("FX/glow"), DH.SortPickups, Vector3.zero);
                    v.extra.color = new Color(DH.Amber.r, DH.Amber.g, DH.Amber.b, 0.45f);
                    v.extra.transform.localScale = Vector3.one * 1.3f;
                    break;
                case "evidence":
                    v.frames = SpriteLibrary.Anim("Pickups/evidence"); v.fps = 6; v.sr.sortingOrder = DH.SortPickups + 1;
                    v.extra = SR.Make("Glow", v.go.transform, SpriteLibrary.Get("FX/glow"), DH.SortPickups, Vector3.zero);
                    v.extra.color = new Color(DH.Pink.r, DH.Pink.g, DH.Pink.b, 0.5f);
                    v.extra.transform.localScale = Vector3.one * 1.8f;
                    break;
                case "vault":
                    v.sr.sprite = _doorClosed; v.sr.sortingOrder = DH.SortProps + 1;
                    break;
                case "boss":
                    v.sr.sortingOrder = DH.SortEnemies - 2;
                    // aim line for the laser, red floor glow for the slam, core glow for the orbs
                    v.extra = SR.Box("AimLine", v.go.transform, DH.Red, DH.SortBolts - 1, -14f, 0, 12.2f, px);
                    v.extra.enabled = false;
                    v.beam = SR.Box("SlamWarning", v.go.transform, new Color(1, 0.18f, 0.33f, 0.5f), DH.SortProps - 2, -12f, -2f, 12f, 3 * px);
                    v.beam.enabled = false;
                    v.emitter = SR.Make("Core", v.go.transform, SpriteLibrary.Get("FX/glow"), DH.SortEnemies - 1, new Vector3(0, 0.0f, 0));
                    v.emitter.color = new Color(DH.Red.r, DH.Red.g, DH.Red.b, 0.6f);
                    break;
                case "turret":
                    v.sr.sortingOrder = DH.SortEnemies;
                    v.extra = SR.Box("WarnLine", v.go.transform, DH.Red, DH.SortBolts - 1, -14f, 0, 13.4f, px);
                    v.extra.enabled = false;
                    break;
                case "press":
                    v.sr.sprite = _pressHead;
                    v.sr.sortingOrder = DH.SortProps + 3;
                    v.beam = SR.Tiled("Shaft", v.go.transform, _pressShaft, DH.SortProps + 2, Vector3.zero, new Vector2(0.5f, 6f));
                    v.extra = SR.Box("Danger", v.go.transform, new Color(1, 0.18f, 0.33f, 0.35f), DH.SortProps - 2, -0.75f, 0, 1.5f, 2 * px);
                    break;
                case "sign" when e.kind == "machine":
                    v.sr.sprite = _machineHang;
                    v.sr.sortingOrder = DH.SortProps + 2;
                    v.extra = SR.Box("Chain", v.go.transform, DH.Hex("6a5f8e"), DH.SortProps + 1, -px, 1.2f, 2 * px, 8f);
                    v.beam = SR.Box("Danger", v.go.transform, new Color(1, 0.18f, 0.33f, 0.45f), DH.SortProps - 2, -0.8f, 0, 1.6f, 2 * px);
                    v.beam.enabled = false;
                    break;
                case "sign":
                    v.sr.sprite = _signHang;
                    v.sr.sortingOrder = DH.SortProps + 2;
                    v.extra = SR.Box("Cable", v.go.transform, DH.Hex("6a5f8e"), DH.SortProps + 1, -0.7f, 1.1f, px, 8f);
                    v.beam = SR.Box("Cable2", v.go.transform, DH.Hex("6a5f8e"), DH.SortProps + 1, 0.65f, 1.1f, px, 8f);
                    break;
            }
            _views.Add(v);
        }

        void LateUpdate()
        {
            if (_sim == null) return;
            float px = 1f / DH.PPU;
            float t = Time.time;
            var p = _sim.p;
            float camX = Camera.main != null ? Camera.main.transform.position.x : p.x;
            foreach (var v in _views)
            {
                var e = v.e;
                bool near = e.ox > camX - 16 && e.ox < camX + 18;
                bool show = near && (e.alive || e.t == "barrier" || e.t == "checkpoint" || e.t == "laser" || e.t == "pad");
                if (v.go.activeSelf != show) v.go.SetActive(show);
                if (!show) continue;

                float y = e.y;
                if (e.t == "repair") y += Mathf.Sin(t * 3f) * 2f / DH.PPU;
                if (e.t == "shard") y += Mathf.Sin(t * 2.5f) * 3f / DH.PPU;
                if (e.t == "sniper") y += 1f;                    // centre of the 2-unit penthouse wall
                if (e.t == "sign" && e.st == RunnerSim.SignShaking) y += Mathf.Sin(t * 70f) * 1.5f / DH.PPU;
                v.go.transform.position = new Vector3(SR.Snap(e.x), SR.Snap(e.t == "laser" ? e.y : y), 0);

                if (v.frames != null && v.frames.Length > 0)
                    v.sr.sprite = v.frames[Mathf.FloorToInt(t * v.fps + e.ox) % v.frames.Length];

                switch (e.t)
                {
                    case "checkpoint": v.sr.sprite = e.alive ? _cpOff : _cpOn; break;
                    case "pad":
                        if (_padFrames.Length > 1) v.sr.sprite = p.pad && Mathf.Abs(p.x - e.x) < 2 ? _padFrames[1] : _padFrames[0];
                        break;
                    case "shard":
                        v.extra.transform.localScale = Vector3.one * (1.6f + Mathf.Sin(t * 4f) * 0.2f);
                        break;
                    case "gunner":
                    {
                        float dx = e.x - p.x;
                        bool aiming = dx < 15 && dx > 6;
                        v.extra.enabled = aiming;
                        if (aiming)
                        {
                            float aimY = _sim.GroundAt(e.x) + 0.8f;
                            var lp = v.extra.transform.localPosition;
                            v.extra.transform.localPosition = new Vector3(lp.x, aimY - e.y, 0);
                            var c = DH.Orange; c.a = 0.25f + 0.25f * Mathf.Sin(t * 20f);
                            v.extra.color = c;
                        }
                        break;
                    }
                    case "sniper":
                    {
                        bool aiming = e.st == 1;
                        float k = aiming ? 1f - e.cd / RunnerSim.SniperTelegraph : 0f;   // 0 -> 1 as the shot nears
                        v.sr.sprite = _sniperFrames.Length > 1 && e.st == 2 && e.cd > RunnerSim.SniperCooldown - 0.1f ? _sniperFrames[1] : _sniperFrames[0];
                        v.extra.enabled = aiming && (k < 0.75f || Mathf.FloorToInt(t * 30f) % 2 == 0);
                        v.beam.enabled = aiming;
                        if (aiming)
                        {
                            float ly = e.aimY - (e.y + 1f);
                            v.extra.transform.localPosition = new Vector3(v.extra.transform.localPosition.x, ly, 0);
                            v.extra.transform.localScale = new Vector3(v.extra.transform.localScale.x, (k > 0.75f ? 3f : 2f) / DH.PPU / SR.Pixel.bounds.size.x, 1);
                            var c = DH.Red; c.a = 0.35f + 0.6f * k;
                            v.extra.color = c;
                            // the dot rides along the line toward the runner's chest
                            float dotX = Mathf.Max(p.x + 0.3f, e.x - 19.3f) - e.x;
                            v.beam.transform.localPosition = new Vector3(dotX, ly, 0);
                        }
                        break;
                    }
                    case "shock":
                    {
                        bool live = RunnerSim.ShockLive(e) && _sim.empT <= 0;
                        bool warning = RunnerSim.ShockWarning(e) && _sim.empT <= 0;
                        v.sr.sprite = live && _shockOn.Length > 0 ? _shockOn[Mathf.FloorToInt(t * 18f) % _shockOn.Length] : _shockOff;
                        v.sr.color = warning && Mathf.FloorToInt(t * 16f) % 2 == 0 ? DH.Amber : Color.white;
                        v.sr.size = new Vector2(e.w, 0.25f);
                        v.extra.enabled = live;
                        // tiled sprites pivot at the bottom-centre; e.x is the strip's left edge
                        v.go.transform.position = new Vector3(SR.Snap(e.x + e.w / 2f), SR.Snap(e.y - 2f / DH.PPU), 0);
                        v.extra.transform.localPosition = new Vector3(0, 0.1f, 0);
                        break;
                    }
                    case "fragment":
                    case "evidence":
                        v.go.transform.position = new Vector3(SR.Snap(e.x), SR.Snap(e.y + Mathf.Sin(t * 3f + e.ox) * 3f / DH.PPU), 0);
                        v.extra.transform.localScale = Vector3.one * ((e.t == "evidence" ? 1.8f : 1.3f) + Mathf.Sin(t * 5f) * 0.15f);
                        break;
                    case "vault":
                        v.sr.sprite = e.st == 1 ? _doorOpen : _doorClosed;
                        break;
                    case "boss":
                    {
                        // 128 px sprite, centre pivot: centre sits 2 units above the floor
                        v.go.transform.position = new Vector3(SR.Snap(e.x), SR.Snap(e.y + 2f), 0);
                        int tele = RunnerSim.BossTelegraphing(e);
                        bool dead = e.st >= 5;
                        v.sr.sprite = dead ? _bossDead
                            : tele == 2 ? _bossSlam
                            : tele >= 0 || e.st == 4 ? _bossCharge
                            : _bossIdle.Length > 0 ? _bossIdle[Mathf.FloorToInt(t * 3f) % _bossIdle.Length] : null;
                        _bossFlash = Mathf.Max(0, _bossFlash - Time.deltaTime);
                        v.sr.color = _bossFlash > 0 ? new Color(3f, 3f, 3f) : dead ? new Color(0.6f, 0.6f, 0.7f) : Color.white;
                        if (dead && e.st == 5) v.go.transform.position += new Vector3(Mathf.Sin(t * 60f) * 2f / DH.PPU, 0, 0);
                        float left = RunnerSim.BossTelegraphLeft(e);
                        v.extra.enabled = tele == 0 && (left > 0.25f || Mathf.FloorToInt(t * 30f) % 2 == 0);
                        if (v.extra.enabled)
                        {
                            float aim = _sim.GroundAt(p.x) + 0.8f - (e.y + 2f);
                            v.extra.transform.localPosition = new Vector3(v.extra.transform.localPosition.x, aim, 0);
                            var c = DH.Red; c.a = 0.4f + 0.5f * (1f - Mathf.Clamp01(left));
                            v.extra.color = c;
                        }
                        v.beam.enabled = tele == 2 && Mathf.FloorToInt(t * 16f) % 2 == 0;
                        v.emitter.enabled = !dead;
                        float charge = tele == 1 || e.st == 4 ? 1.6f + Mathf.Sin(t * 30f) * 0.3f : 0.9f;
                        v.emitter.transform.localScale = Vector3.one * charge;
                        break;
                    }
                    case "turret":
                    {
                        v.sr.sprite = _turretFrames.Length > 2
                            ? _turretFrames[e.st == 1 ? 1 : e.st == 2 && e.cd > RunnerSim.TurretCooldown - 0.1f ? 2 : 0] : null;
                        v.go.transform.position = new Vector3(SR.Snap(e.x), SR.Snap(e.y + 0.5f), 0);
                        bool aiming = e.st == 1;
                        float k = aiming ? 1f - e.cd / RunnerSim.TurretTelegraph : 0;
                        v.extra.enabled = aiming && (k < 0.7f || Mathf.FloorToInt(t * 30f) % 2 == 0);
                        if (aiming)
                        {
                            v.extra.transform.localPosition = new Vector3(v.extra.transform.localPosition.x, e.aimY - (e.y + 0.5f), 0);
                            var c = DH.Red; c.a = 0.3f + 0.6f * k;
                            v.extra.color = c;
                        }
                        break;
                    }
                    case "press":
                    {
                        float bottom = RunnerSim.PressBottom(e);
                        bool warn = RunnerSim.PressWarning(e);
                        float shake = warn ? Mathf.Sin(t * 80f) * 1.5f / DH.PPU : 0;
                        // the plate (pivot bottom-centre) rides at its current height; the shaft goes up from it,
                        // and the danger marker stays on the floor underneath
                        v.go.transform.position = new Vector3(SR.Snap(e.x + shake), SR.Snap(e.y + bottom), 0);
                        v.beam.transform.localPosition = new Vector3(0, 0.85f, 0);
                        v.extra.transform.localPosition = new Vector3(0, -bottom + px, 0);
                        v.extra.color = new Color(1, 0.18f, 0.33f, warn ? 0.9f : bottom < 1.5f ? 0.6f : 0.25f);
                        break;
                    }
                    case "sign" when e.kind == "machine":
                    {
                        bool landed = e.st == RunnerSim.SignLanded;
                        v.sr.sprite = landed ? _machineFallen : _machineHang;
                        v.extra.enabled = e.st <= RunnerSim.SignShaking;
                        // red marker on the floor under the load while it threatens to drop
                        v.beam.enabled = e.st <= RunnerSim.SignFalling && e.x - p.x < RunnerSim.SignTrigger + 5f;
                        if (v.beam.enabled)
                        {
                            float gy = _sim.GroundAt(e.x) - v.go.transform.position.y;
                            v.beam.transform.localPosition = new Vector3(0, gy + px, 0);
                            var c = DH.Red; c.a = 0.35f + 0.35f * Mathf.Abs(Mathf.Sin(t * (e.st >= 1 ? 18f : 5f)));
                            v.beam.color = c;
                        }
                        break;
                    }
                    case "sign":
                    {
                        bool landed = e.st == RunnerSim.SignLanded;
                        v.sr.sprite = landed ? _signFallen : _signHang;
                        v.extra.enabled = v.beam.enabled = e.st <= RunnerSim.SignShaking;
                        break;
                    }
                    case "laser":
                    {
                        bool off = !_sim.LaserLive(e);
                        bool warning = RunnerSim.LaserWarning(e);
                        bool flick = (_sim.empT > 0 && Mathf.FloorToInt(t * 12f) % 7 == 0) || (warning && Mathf.FloorToInt(t * 20f) % 3 == 0);
                        v.beam.enabled = !off || flick;
                        v.beam.color = warning ? new Color(1, 1, 1, 0.45f) : Color.white;
                        if (_beamFrames.Length > 0) v.beam.sprite = _beamFrames[Mathf.FloorToInt(t * 15f) % _beamFrames.Length];
                        v.beam.size = new Vector2(0.5f, e.h);
                        v.emitter.sprite = off ? _emOff : _emOn;
                        break;
                    }
                }
            }
            SyncBolts(t);
        }

        void SyncBolts(float t)
        {
            int i = 0;
            foreach (var b in _sim.bolts)
            {
                if (!b.alive) continue;
                if (i >= _boltPool.Count)
                    _boltPool.Add(SR.Make("Bolt", transform, null, DH.SortBolts, Vector3.zero));
                var sr = _boltPool[i++];
                sr.gameObject.SetActive(true);
                var frames = b.kind == 3 && _orbFrames != null ? _orbFrames
                    : b.kind == 4 && _waveFrames != null ? _waveFrames
                    : (b.sniper || b.kind == 2) && _sniperBolt != null && _sniperBolt.Length > 0 ? _sniperBolt : _boltFrames;
                if (frames.Length > 0) sr.sprite = frames[Mathf.FloorToInt(t * 20f) % frames.Length];
                // shockwaves roll along the floor (sprite centre 0.3 up); slashed-back shots turn cyan and fly right
                sr.transform.position = new Vector3(SR.Snap(b.x), SR.Snap(b.kind == 4 ? b.y + 0.3f : b.y), 0);
                sr.flipX = b.reflected;
                sr.color = b.reflected ? DH.Cyan : Color.white;
            }
            for (; i < _boltPool.Count; i++) _boltPool[i].gameObject.SetActive(false);
        }
    }
}
