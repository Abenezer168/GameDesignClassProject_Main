using System;
using System.Collections.Generic;
using UnityEngine;

namespace DataHeist
{
    /// <summary>One frame of player intent. "Pressed" flags are edge-triggered.</summary>
    public struct RunnerInputFrame
    {
        public bool jumpPressed, jumpHeld, slidePressed, slideHeld, attackPressed, abilityPressed;
    }

    public enum SimState { Running, Dead, Won }

    public enum SimEventType
    {
        Jump, AirJump, Land, Slide, Dive, Attack, Byte, Repair, Shard, Pad, DroneDown, CanHit,
        Hurt, Dead, Checkpoint, Dash, DashEnd, Emp, Glitch, AbilityDenied, Shoot, Deflect, Respawn, Win,
        // Level 2 hazards
        SniperAim, SniperShot, ShockOn, SignShake, SignFall, SignLand, SignSmash,
        // Level 3
        TurretAim, TurretShot, LaserOn, PressWarn, PressSlam,
        Fragment, VaultOpen, BossStart, BossTelegraph, BossFire, BossHit, BossPhase, BossDefeated, Evidence, Lockdown
    }

    public struct SimEvent
    {
        public SimEventType type;
        public float x, y;
        public string info;     // e.g. drone kill method, death cause
        public bool flag;       // e.g. hard landing
    }

    public class SimEntity
    {
        public int id;
        public string t, kind, move, mode;
        public float x, y, ox, oy, baseY, h, cd, phase;
        public float w, aimY, vy;   // Level 2: shock width, sniper aim height, falling-sign speed
        public int st;              // Level 2 state machine (sniper / shock / sign)
        public int hp, step;        // Level 3 boss: health and position in its attack pattern
        public bool alive = true;
        public SimEntity Clone() => (SimEntity)MemberwiseClone();
    }

    public class SimBolt
    {
        public float x, y, vx;
        public bool alive = true;
        public bool sniper;         // sniper tracer: faster, longer, drawn red
        public int kind;            // 0 bolt, 2 boss laser bolt, 3 boss orb, 4 shockwave (ground, can't be slashed)
        public bool reflected;      // slashed back toward the boss
    }

    public class SimPlayer
    {
        public float x, y, vy, slideT, attackT = -1, attackCD, inv, dashT, abilityCD, coyote, buffer, hurtT, landT;
        public int hp;
        public bool onGround = true, sliding, airJumped, cut, pad, diving, slideQueued, wall;
        public SimPlayer Clone() => (SimPlayer)MemberwiseClone();
    }

    public class SimStats
    {
        public int bytes, hits, deaths, drones, fragments;
        public bool shard, evidence;
        public SimStats Clone() => (SimStats)MemberwiseClone();
    }

    /// <summary>
    /// Deterministic, engine-independent gameplay simulation for an auto-runner level.
    /// A 1:1 port of the browser build's sim.js so the level validated by the playtest bot
    /// behaves identically here. Units are world units, y-up. Views only read from it.
    /// </summary>
    public class RunnerSim
    {
        public readonly LevelData L;
        public readonly PhysicsData P;
        public readonly CharacterDefinition ch;
        public readonly List<SimEvent> events = new List<SimEvent>();
        public SimEntity[] ents;
        public List<SimBolt> bolts = new List<SimBolt>();
        public SimPlayer p;
        public SimStats stats = new SimStats();
        public SimState state = SimState.Running;
        public float time, empT, magnetT, checkpointX;
        public int totalBytes;
        // Level 3: data fragments, the boss and the lockdown escape
        public int totalFragments;
        public float escapeT;          // seconds left before the facility locks down (while escaping)
        public bool escaping;
        string lastDeath;
        int cpDrones;

        public RunnerSim(LevelData level, CharacterDefinition character)
        {
            L = level; P = level.physics; ch = character;
            var list = new List<SimEntity>();
            foreach (var e in level.entities)
            {
                list.Add(new SimEntity
                {
                    t = e.t, kind = e.kind, move = e.move, mode = e.mode, h = e.h, w = e.w,
                    x = e.x, y = e.y, ox = e.x, oy = e.y, baseY = e.y, cd = 0.8f,
                    phase = (e.x * 1.7f) % 6.28f,
                });
            }
            list.Sort((a, b) => a.ox.CompareTo(b.ox));
            ents = list.ToArray();
            for (int i = 0; i < ents.Length; i++)
            {
                ents[i].id = i;
                if (ents[i].t == "byte") totalBytes++;
                if (ents[i].t == "fragment") totalFragments++;
                if (ents[i].t == "boss") ents[i].hp = BossMaxHp;
            }
            checkpointX = level.spawnX;
            Spawn(level.spawnX);
        }

        RunnerSim(RunnerSim o)
        {
            L = o.L; P = o.P; ch = o.ch;
            ents = new SimEntity[o.ents.Length];
            for (int i = 0; i < ents.Length; i++) ents[i] = o.ents[i].Clone();
            foreach (var b in o.bolts) bolts.Add(new SimBolt { x = b.x, y = b.y, vx = b.vx, alive = b.alive, sniper = b.sniper, kind = b.kind, reflected = b.reflected });
            totalFragments = o.totalFragments; escapeT = o.escapeT; escaping = o.escaping; lastDeath = o.lastDeath;
            p = o.p.Clone(); stats = o.stats.Clone(); state = o.state;
            time = o.time; empT = o.empT; magnetT = o.magnetT; checkpointX = o.checkpointX;
            totalBytes = o.totalBytes; cpDrones = o.cpDrones;
        }

        public RunnerSim Clone() => new RunnerSim(this);

        // ------------------------------------------------------------------ queries
        public int LowerIndex(float x)
        {
            int lo = 0, hi = ents.Length;
            while (lo < hi) { int m = (lo + hi) >> 1; if (ents[m].ox < x) lo = m + 1; else hi = m; }
            return lo;
        }

        public float GroundAt(float x)
        {
            foreach (var r in L.roofs) if (x >= r.x0 && x <= r.x1) return r.y;
            return float.NegativeInfinity;
        }

        public float Height => p.sliding ? P.slideHeight : P.standHeight;

        public bool InAlarmZone
        {
            get
            {
                foreach (var z in L.zones) if (z.alarm && p.x >= z.x0 && p.x <= z.x1) return true;
                return false;
            }
        }

        /// <summary>The boss whose arena the runner is standing in (the run pauses until it falls), or null.</summary>
        public SimEntity ArenaBoss
        {
            get
            {
                foreach (var e in ents)
                    if (e.t == "boss" && e.st >= BossFighting && e.st < BossDown) return e;
                return null;
            }
        }

        public SimEntity Boss { get { foreach (var e in ents) if (e.t == "boss") return e; return null; } }

        public float SpeedNow()
        {
            if (ArenaBoss != null) return 0f;     // feet planted: jump, slide and slash still work
            float s = P.runSpeed * ch.speedMultiplier;
            foreach (var z in L.zones) if (p.x >= z.x0 && p.x <= z.x1) s *= z.speed;
            if (p.dashT > 0) s *= 2.6f;
            if (p.wall) s = 0;
            return s;
        }

        void Emit(SimEventType type, float x = 0, float y = 0, string info = null, bool flag = false)
            => events.Add(new SimEvent { type = type, x = x, y = y, info = info, flag = flag });

        void Spawn(float x)
        {
            p = new SimPlayer { x = x, y = GroundAt(x), hp = ch.maxHealth };
        }

        // ------------------------------------------------------------------ tick
        public void Step(float dt, RunnerInputFrame inp)
        {
            if (state != SimState.Running) return;
            time += dt;
            p.inv = Mathf.Max(0, p.inv - dt);
            p.attackCD = Mathf.Max(0, p.attackCD - dt);
            p.abilityCD = Mathf.Max(0, p.abilityCD - dt);
            p.coyote = Mathf.Max(0, p.coyote - dt);
            p.buffer = Mathf.Max(0, p.buffer - dt);
            p.hurtT = Mathf.Max(0, p.hurtT - dt);
            p.landT = Mathf.Max(0, p.landT - dt);
            empT = Mathf.Max(0, empT - dt);
            magnetT = Mathf.Max(0, magnetT - dt);
            if (p.attackT >= 0) { p.attackT += dt; if (p.attackT > 0.28f) p.attackT = -1; }
            if (p.dashT > 0) { p.dashT -= dt; if (p.dashT <= 0) { p.dashT = 0; Emit(SimEventType.DashEnd); } }

            if (inp.jumpPressed) p.buffer = P.jumpBuffer;
            if (inp.abilityPressed) UseAbility();

            // jump / air jump
            if (p.buffer > 0 && (p.onGround || p.coyote > 0) && !p.wall)
            {
                p.vy = P.jumpVelocity * ch.jumpMultiplier;
                p.onGround = false; p.coyote = 0; p.buffer = 0; p.cut = false; p.pad = false;
                if (p.sliding) EndSlide(true);
                Emit(SimEventType.Jump, p.x, p.y);
            }
            else if (inp.jumpPressed && !p.onGround && p.coyote <= 0 && ch.ability == AbilityType.KickThrusters && !p.airJumped)
            {
                AirJump();
            }
            if (!inp.jumpHeld && p.vy > 0 && !p.cut && !p.pad && !p.onGround)
            {
                p.vy *= P.jumpCutMultiplier; p.cut = true;
            }

            // slide / dive
            if (inp.slidePressed)
            {
                if (p.onGround && !p.sliding) StartSlide();
                else if (!p.onGround && !p.diving && p.dashT <= 0)
                {
                    p.diving = true; p.vy = Mathf.Min(p.vy, -P.maxFall * 0.75f); p.slideQueued = true;
                    Emit(SimEventType.Dive, p.x, p.y);
                }
            }
            if (p.sliding)
            {
                p.slideT += dt;
                bool keep = p.slideT < P.slideMin || (inp.slideHeld && p.slideT < P.slideMax) || CeilingBlocked();
                if (!keep) EndSlide(false);
            }

            // attack
            if (inp.attackPressed && p.attackCD <= 0)
            {
                p.attackT = 0; p.attackCD = 0.32f; Emit(SimEventType.Attack, p.x, p.y);
            }

            // integrate
            float spd = SpeedNow();
            if (p.dashT > 0) p.vy = 0;
            else { p.vy -= P.gravity * dt; if (p.vy < -P.maxFall) p.vy = -P.maxFall; }
            float prevY = p.y, prevX = p.x;
            float nx = p.x + spd * dt;
            float ny = p.y + p.vy * dt;
            float hw = P.width / 2f;

            p.wall = false;
            foreach (var r in L.roofs)
            {
                if (prevX + hw <= r.x0 + 1e-6f && nx + hw > r.x0 && ny < r.y - 0.05f)
                {
                    nx = r.x0 - hw; p.wall = true; break;
                }
            }

            bool landed = false;
            if (p.vy <= 0)
            {
                float best = float.NegativeInfinity;
                foreach (var s in L.roofs) Surface(s, nx, hw, prevY, ny, ref best);
                foreach (var s in L.platforms) Surface(s, nx, hw, prevY, ny, ref best);
                if (!float.IsNegativeInfinity(best)) { ny = best; landed = true; }
            }

            bool wasGround = p.onGround;
            p.x = nx; p.y = ny;
            if (landed)
            {
                if (!wasGround)
                {
                    p.landT = 0.12f;
                    Emit(SimEventType.Land, p.x, p.y, null, p.vy < -18f);
                    p.airJumped = false; p.cut = false; p.pad = false;
                    if (p.slideQueued || p.diving) { p.diving = false; p.slideQueued = false; StartSlide(); }
                }
                p.vy = 0; p.onGround = true;
            }
            else
            {
                if (wasGround) p.coyote = P.coyoteTime;
                p.onGround = false;
                if (p.sliding && p.slideT > 0.05f) EndSlide(false);
            }

            UpdateEntities(dt);
            Collide();

            if (escaping && state == SimState.Running)
            {
                escapeT -= dt;
                if (escapeT <= 0) { escapeT = 0; Die("lockdown"); }
            }
            if (p.y < L.killY) Die("fall");
            if (p.x >= L.exitX && state == SimState.Running)
            {
                state = SimState.Won; Emit(SimEventType.Win, p.x, p.y);
            }
        }

        static void Surface(RoofData s, float nx, float hw, float prevY, float ny, ref float best)
        {
            if (nx + hw > s.x0 && nx - hw < s.x1 && prevY >= s.y - 0.02f && ny <= s.y && s.y > best) best = s.y;
        }

        void StartSlide() { p.sliding = true; p.slideT = 0; Emit(SimEventType.Slide, p.x, p.y); }

        void EndSlide(bool force)
        {
            if (!force && CeilingBlocked()) return;
            p.sliding = false; p.slideT = 0;
        }

        bool CeilingBlocked()
        {
            float hw = P.width / 2f;
            foreach (var e in ents)
            {
                if (!e.alive) continue;
                bool ceiling = e.t == "barrier"
                               || (e.t == "laser" && (e.mode == "high" || e.mode == "high_cycle") && LaserLive(e))
                               || (e.t == "press" && PressBottom(e) < P.standHeight);
                if (!ceiling) continue;
                var b = BoxOf(e);
                if (Overlap(p.x - hw, p.y, p.x + hw + 0.3f, p.y + P.standHeight, b)) return true;
            }
            return false;
        }

        void AirJump()
        {
            p.vy = 14f; p.airJumped = true; p.cut = false; p.pad = false; p.diving = false; p.slideQueued = false;
            Emit(SimEventType.AirJump, p.x, p.y);
        }

        public bool AbilityReady =>
            ch.ability == AbilityType.KickThrusters ? !p.airJumped : p.abilityCD <= 0;

        void UseAbility()
        {
            switch (ch.ability)
            {
                case AbilityType.KickThrusters:
                    if (!p.onGround && p.coyote <= 0 && !p.airJumped) AirJump();
                    else if (p.onGround) p.buffer = P.jumpBuffer;
                    return;
                case AbilityType.PhaseDash:
                    if (p.abilityCD > 0) { Emit(SimEventType.AbilityDenied); return; }
                    p.dashT = 0.34f; p.abilityCD = ch.abilityCooldown; p.vy = 0; p.diving = false;
                    if (p.sliding) EndSlide(true);
                    Emit(SimEventType.Dash, p.x, p.y);
                    return;
                case AbilityType.EmpPulse:
                    if (p.abilityCD > 0) { Emit(SimEventType.AbilityDenied); return; }
                    p.abilityCD = ch.abilityCooldown; empT = 4f;
                    foreach (var e in ents)
                        if (e.alive && (e.t == "drone" || e.t == "gunner" || e.t == "sniper") && Mathf.Abs(e.x - p.x) < 11f) KillDrone(e, "emp");
                    bolts.RemoveAll(b => Mathf.Abs(b.x - p.x) <= 11f);
                    Emit(SimEventType.Emp, p.x, p.y);
                    return;
                case AbilityType.TimeGlitch:
                    if (p.abilityCD > 0) { Emit(SimEventType.AbilityDenied); return; }
                    p.abilityCD = ch.abilityCooldown; magnetT = 4f;
                    Emit(SimEventType.Glitch, p.x, p.y);   // the presentation layer slows time
                    return;
            }
        }

        // ------------------------------------------------------------------ entities
        public static Rect BoxOf(SimEntity e)
        {
            switch (e.t)
            {
                case "can": return Rect.MinMaxRect(e.x - 0.3f, e.y, e.x + 0.3f, e.y + 0.8f);
                case "barrier": return Rect.MinMaxRect(e.x - 0.8f, e.y + 0.75f, e.x + 0.8f, e.y + 3.25f);
                case "laser": return Rect.MinMaxRect(e.x - 0.15f, e.y, e.x + 0.15f, e.y + e.h);
                case "drone":
                case "gunner": return Rect.MinMaxRect(e.x - 0.4f, e.y - 0.3f, e.x + 0.4f, e.y + 0.3f);
                case "pad": return Rect.MinMaxRect(e.x - 0.5f, e.y, e.x + 0.5f, e.y + 0.35f);
                case "byte": return Rect.MinMaxRect(e.x - 0.3f, e.y - 0.3f, e.x + 0.3f, e.y + 0.3f);
                case "repair": return Rect.MinMaxRect(e.x - 0.35f, e.y - 0.35f, e.x + 0.35f, e.y + 0.35f);
                case "shard": return Rect.MinMaxRect(e.x - 0.5f, e.y - 0.7f, e.x + 0.5f, e.y + 0.7f);
                case "fragment": return Rect.MinMaxRect(e.x - 0.4f, e.y - 0.4f, e.x + 0.4f, e.y + 0.4f);
                case "evidence": return Rect.MinMaxRect(e.x - 0.5f, e.y - 0.5f, e.x + 0.5f, e.y + 0.5f);
                case "vault": return Rect.MinMaxRect(e.x - 0.6f, e.y, e.x + 0.6f, e.y + 3f);
                case "shock": return Rect.MinMaxRect(e.x, e.y, e.x + e.w, e.y + 0.15f);
                case "press":
                {
                    float b = e.y + PressBottom(e);
                    return Rect.MinMaxRect(e.x - 0.75f, b, e.x + 0.75f, b + 0.9f);
                }
                case "sign":
                    if (e.st == SignFalling) return Rect.MinMaxRect(e.x - 1f, e.y, e.x + 1f, e.y + 1.0f);
                    if (e.st == SignLanded) return Rect.MinMaxRect(e.x - 0.9f, e.y, e.x + 0.9f, e.y + 0.7f);
                    return Rect.zero;
            }
            return Rect.zero;
        }

        static bool HasBox(SimEntity e) =>
            e.t != "checkpoint" && e.t != "exit" && e.t != "sniper" && e.t != "turret" && e.t != "boss"
            && !(e.t == "sign" && e.st < SignFalling);

        // ------------------------------------------------------------------ Level 2 hazards
        // Every hazard is triggered by the runner's distance, never by the clock, so a run (and a
        // checkpoint retry) always meets it the same way and the playtest bot can prove it fair.
        public const float SniperAimAt = 17f;      // starts aiming when this far ahead
        public const float SniperTelegraph = 0.9f; // red laser on screen before the shot
        public const float SniperCooldown = 1.6f;
        public const float SniperBoltSpeed = 24f;
        public const float ShockTrigger = 14f, ShockPeriod = 1.5f, ShockOnTime = 0.9f;
        public const float SignTrigger = 10.5f, SignShakeTime = 0.35f, SignGravity = 40f;
        public const int SignHanging = 0, SignShaking = 1, SignFalling = 2, SignLanded = 3;

        /// <summary>A shock strip is live for the first ShockOnTime of every ShockPeriod after it triggers.</summary>
        public static bool ShockLive(SimEntity e)
        {
            if (e.st != 1) return false;
            float ph = Mathf.Repeat(e.cd, ShockPeriod);
            // "warn" strips (Level 3) spend 0.4 s crackling amber before every live phase
            return e.mode == "warn" ? ph >= ShockWarnTime && ph < ShockWarnTime + ShockOnTime : ph < ShockOnTime;
        }

        public const float ShockWarnTime = 0.4f;

        /// <summary>True while a warned strip is in its amber lead-in (drawn as a warning).</summary>
        public static bool ShockWarning(SimEntity e) =>
            e.st == 1 && e.mode == "warn" && Mathf.Repeat(e.cd, ShockPeriod) < ShockWarnTime;

        // ------------------------------------------------------------------ Level 3 hazards
        public const float TurretAimAt = 16f, TurretTelegraph = 0.7f, TurretCooldown = 1.2f, TurretBoltSpeed = 16f;
        public const float LaserPeriod = 2f, LaserWarnAt = 0.6f, LaserOnAt = 1f;     // off, warn (flicker), on
        public const float PressTrigger = 15f, PressUpY = 3.6f, PressDownY = 0.6f;
        // press cycle: up hold, warn shake, slam, down hold, rise
        public const float PressUp = 1.0f, PressWarn = 0.35f, PressSlam = 0.12f, PressDown = 0.6f, PressRise = 0.5f;
        public const float PressPeriod = PressUp + PressWarn + PressSlam + PressDown + PressRise;

        public static bool IsCycling(SimEntity e) => e.mode == "low_cycle" || e.mode == "high_cycle";

        /// <summary>Laser beams: always live, except cycling ones (off / warning / on) and while EMP'd.</summary>
        public bool LaserLive(SimEntity e)
        {
            if (empT > 0) return false;
            if (!IsCycling(e)) return true;
            return e.st == 1 && Mathf.Repeat(e.cd, LaserPeriod) >= LaserOnAt;
        }

        public static bool LaserWarning(SimEntity e)
        {
            if (!IsCycling(e) || e.st != 1) return false;
            float ph = Mathf.Repeat(e.cd, LaserPeriod);
            return ph >= LaserWarnAt && ph < LaserOnAt;
        }

        /// <summary>Height of a press plate's bottom edge above its floor.</summary>
        public static float PressBottom(SimEntity e)
        {
            if (e.st != 1) return PressUpY;
            float t = Mathf.Repeat(e.cd, PressPeriod);
            if (t < PressUp + PressWarn) return PressUpY;
            t -= PressUp + PressWarn;
            if (t < PressSlam) return Mathf.Lerp(PressUpY, PressDownY, t / PressSlam);
            t -= PressSlam;
            if (t < PressDown) return PressDownY;
            t -= PressDown;
            return Mathf.Lerp(PressDownY, PressUpY, t / PressRise);
        }

        public static bool PressWarning(SimEntity e)
        {
            if (e.st != 1) return false;
            float t = Mathf.Repeat(e.cd, PressPeriod);
            return t >= PressUp && t < PressUp + PressWarn;
        }

        void UpdateLevel3(SimEntity e, float dt)
        {
            float dx = e.x - p.x;
            switch (e.t)
            {
                case "turret":
                    // st 0 waiting, 1 aiming (warning line), 2 cooling down; "low" mode aims at the ankles
                    if (e.st == 0 && dx < TurretAimAt && dx > 4f)
                    {
                        e.st = 1; e.cd = TurretTelegraph;
                        e.aimY = e.y + (e.mode == "low" ? 0.25f : 0.8f);
                        Emit(SimEventType.TurretAim, e.x, e.aimY);
                    }
                    else if (e.st == 1 && (e.cd -= dt) <= 0)
                    {
                        bolts.Add(new SimBolt { x = e.x - 0.6f, y = e.aimY, vx = -TurretBoltSpeed });
                        Emit(SimEventType.TurretShot, e.x, e.aimY);
                        e.st = 2; e.cd = TurretCooldown;
                    }
                    else if (e.st == 2 && (e.cd -= dt) <= 0) e.st = 0;
                    break;
                case "laser":
                    if (e.st == 0 && dx < 14f) { e.st = 1; e.cd = 0; }
                    else if (e.st == 1)
                    {
                        bool was = LaserLive(e);
                        e.cd += dt;
                        if (!was && LaserLive(e) && dx < 12f && dx > -1f) Emit(SimEventType.LaserOn, e.x, e.y);
                    }
                    break;
                case "press":
                    if (e.st == 0 && dx < PressTrigger) { e.st = 1; e.cd = 0; }
                    else if (e.st == 1)
                    {
                        bool warn = PressWarning(e), down = PressBottom(e) <= PressDownY + 0.01f;
                        e.cd += dt;
                        if (dx < 13f && dx > -2f)
                        {
                            if (!warn && PressWarning(e)) Emit(SimEventType.PressWarn, e.x, e.y + PressUpY);
                            if (!down && PressBottom(e) <= PressDownY + 0.01f) Emit(SimEventType.PressSlam, e.x, e.y + PressDownY);
                        }
                    }
                    break;
            }
        }

        void UpdateLevel2(SimEntity e, float dt)
        {
            float dx = e.x - p.x;
            switch (e.t)
            {
                case "sniper":
                    // st 0 waiting, 1 aiming (telegraph), 2 cooling down
                    if (e.st == 0 && dx < SniperAimAt && dx > 5f)
                    {
                        e.st = 1; e.cd = SniperTelegraph;
                        e.aimY = e.y + 0.8f;               // chest height of a runner standing on this roof
                        Emit(SimEventType.SniperAim, e.x, e.aimY);
                    }
                    else if (e.st == 1)
                    {
                        e.cd -= dt;
                        if (e.cd <= 0)
                        {
                            bolts.Add(new SimBolt { x = e.x - 0.7f, y = e.aimY, vx = -SniperBoltSpeed, sniper = true });
                            Emit(SimEventType.SniperShot, e.x, e.aimY);
                            e.st = 2; e.cd = SniperCooldown;
                        }
                    }
                    else if (e.st == 2)
                    {
                        e.cd -= dt;
                        if (e.cd <= 0) e.st = 0;
                    }
                    break;
                case "shock":
                    if (e.st == 0 && dx < ShockTrigger) { e.st = 1; e.cd = 0; Emit(SimEventType.ShockOn, e.x, e.y); }
                    else if (e.st == 1)
                    {
                        bool was = ShockLive(e);
                        e.cd += dt;
                        if (!was && ShockLive(e) && dx < 12f && dx > -e.w) Emit(SimEventType.ShockOn, e.x, e.y);
                    }
                    break;
                case "sign":
                    if (e.st == SignHanging && dx < SignTrigger)
                    {
                        e.st = SignShaking; e.cd = SignShakeTime;
                        Emit(SimEventType.SignShake, e.x, e.y);
                    }
                    else if (e.st == SignShaking)
                    {
                        e.cd -= dt;
                        if (e.cd <= 0) { e.st = SignFalling; e.vy = 0; Emit(SimEventType.SignFall, e.x, e.y); }
                    }
                    else if (e.st == SignFalling)
                    {
                        e.vy -= SignGravity * dt;
                        e.y += e.vy * dt;
                        float g = GroundAt(e.x);
                        if (e.y <= g) { e.y = g; e.st = SignLanded; Emit(SimEventType.SignLand, e.x, e.y); }
                    }
                    break;
            }
        }

        static bool Overlap(float ax0, float ay0, float ax1, float ay1, Rect b) =>
            ax0 < b.xMax && ax1 > b.xMin && ay0 < b.yMax && ay1 > b.yMin;

        void UpdateEntities(float dt)
        {
            for (int i = LowerIndex(p.x - 8); i < ents.Length; i++)
            {
                var e = ents[i];
                if (e.ox > p.x + 22) break;
                if (!e.alive) continue;
                if ((e.t == "drone" || e.t == "gunner") && e.move == "bob")
                    e.y = e.baseY + Mathf.Sin(time * 3f + e.phase) * 0.18f;
                if ((e.t == "drone" || e.t == "gunner") && e.move == "patrol")
                {
                    // sweeps 1.5 units each way as the runner approaches (driven by distance, not time)
                    e.x = e.ox + Mathf.Sin((p.x - e.ox) * 0.45f + e.phase) * 1.5f;
                    e.y = e.baseY + Mathf.Sin(time * 3f + e.phase) * 0.12f;
                }
                if (e.t == "gunner")
                {
                    float dx = e.x - p.x;
                    if (dx < 15f && dx > 6f)
                    {
                        e.cd -= dt;
                        if (e.cd <= 0)
                        {
                            e.cd = 1.45f;
                            // aimed at chest height of a runner standing on the roof: slide, jump or slash it
                            bolts.Add(new SimBolt { x = e.x - 0.5f, y = GroundAt(e.x) + 0.8f, vx = -7.5f });
                            Emit(SimEventType.Shoot, e.x, e.y);
                        }
                    }
                }
                if (e.t == "sniper" || e.t == "shock" || e.t == "sign") UpdateLevel2(e, dt);
                if (e.t == "turret" || e.t == "press" || (e.t == "laser" && IsCycling(e))) UpdateLevel3(e, dt);
                if (e.t == "boss") UpdateBoss(e, dt);
                if (e.t == "vault" && e.st == 0 && e.x - p.x < 6f && stats.fragments >= totalFragments)
                {
                    e.st = 1;
                    Emit(SimEventType.VaultOpen, e.x, e.y + 1.5f);
                }
                if (e.t == "byte" && magnetT > 0)
                {
                    float dx = p.x - e.x, dy = (p.y + 0.6f) - e.y, d = Mathf.Sqrt(dx * dx + dy * dy);
                    if (d < 4.5f && d > 0.01f)
                    {
                        float v = Mathf.Min(14f * dt, d);
                        e.x += dx / d * v; e.y += dy / d * v;
                    }
                }
            }
            foreach (var b in bolts) b.x += b.vx * dt;
            bolts.RemoveAll(b => !b.alive || b.x < p.x - 20f || b.x > p.x + 30f);
        }

        void Collide()
        {
            float hw = P.width / 2f, h = Height;
            float px0 = p.x - hw, px1 = p.x + hw, py0 = p.y, py1 = p.y + h;
            bool dashing = p.dashT > 0;
            bool atk = p.attackT >= 0 && p.attackT <= 0.17f;
            float ax0 = p.x + 0.05f, ax1 = p.x + ch.attackReach, ay0 = p.y - 0.1f, ay1 = p.y + 1.8f;

            for (int i = LowerIndex(p.x - 6); i < ents.Length; i++)
            {
                var e = ents[i];
                if (e.ox > p.x + 6) break;
                if (!e.alive) continue;
                if (!HasBox(e))
                {
                    if (e.t == "checkpoint" && p.x >= e.x)
                    {
                        e.alive = false; checkpointX = e.x; cpDrones = stats.drones;
                        Emit(SimEventType.Checkpoint, e.x, GroundAt(e.x));
                    }
                    continue;
                }
                var b = BoxOf(e);
                if (b.xMin > p.x + 3 || b.xMax < p.x - 3) continue;
                bool touching = Overlap(px0, py0, px1, py1, b);
                switch (e.t)
                {
                    case "byte":
                        if (Overlap(px0 - 0.1f, py0, px1 + 0.1f, py1 + 0.15f, b))
                        {
                            e.alive = false; stats.bytes++; Emit(SimEventType.Byte, e.x, e.y);
                        }
                        break;
                    case "repair":
                        if (touching) { e.alive = false; p.hp = Mathf.Min(ch.maxHealth, p.hp + 1); Emit(SimEventType.Repair, e.x, e.y); }
                        break;
                    case "shard":
                        if (touching) { e.alive = false; stats.shard = true; Emit(SimEventType.Shard, e.x, e.y); }
                        break;
                    case "fragment":
                        if (touching)
                        {
                            e.alive = false; stats.fragments++;
                            Emit(SimEventType.Fragment, e.x, e.y, null, stats.fragments >= totalFragments);
                        }
                        break;
                    case "evidence":
                        if (touching)
                        {
                            e.alive = false; stats.evidence = true;
                            escaping = true; escapeT = EscapeTime;
                            Emit(SimEventType.Evidence, e.x, e.y);
                            Emit(SimEventType.Lockdown, e.x, e.y);
                        }
                        break;
                    case "vault":
                        if (e.st == 0 && touching) Die("vault");
                        break;
                    case "pad":
                        if (touching && p.vy <= 0 && !dashing)
                        {
                            p.vy = P.padVelocity; p.onGround = false; p.pad = true; p.cut = true; p.airJumped = false;
                            if (p.sliding) EndSlide(true);
                            Emit(SimEventType.Pad, e.x, e.y);
                        }
                        break;
                    case "drone":
                    case "gunner":
                        if (atk && Overlap(ax0, ay0, ax1, ay1, b)) { KillDrone(e, "slash"); break; }
                        if (!touching) break;
                        if (dashing) { KillDrone(e, "dash"); break; }
                        if (p.vy < 0 && p.y >= e.y - 0.05f)
                        {
                            KillDrone(e, "stomp");
                            p.vy = P.stompBounce; p.airJumped = false; p.cut = true; p.pad = true;
                            break;
                        }
                        KillDrone(e, "crash");
                        Hurt();
                        break;
                    case "can":
                    case "barrier":
                        if (touching && !dashing)
                        {
                            if (e.t == "can") { e.alive = false; Emit(SimEventType.CanHit, e.x, e.y); }
                            Hurt();
                        }
                        break;
                    case "laser":
                        if (touching && !dashing && LaserLive(e)) Hurt();
                        break;
                    case "press":
                        if (touching && !dashing) Hurt();
                        break;
                    case "shock":
                        // only feet on a live strip: jump it, wait for it to drop, or EMP it
                        if (touching && p.onGround && !dashing && empT <= 0 && ShockLive(e)) Hurt();
                        break;
                    case "sign":
                        if (atk && Overlap(ax0, ay0, ax1, ay1, b)) { e.alive = false; Emit(SimEventType.SignSmash, e.x, e.y + 0.4f); break; }
                        if (touching && !dashing)
                        {
                            e.alive = false;
                            Emit(SimEventType.SignSmash, e.x, e.y + 0.4f);
                            Hurt();
                        }
                        break;
                }
            }

            foreach (var bo in bolts)
            {
                if (!bo.alive) continue;
                var bb = BoltBox(bo);
                if (bo.reflected)
                {
                    // slashed back: it can only hit the boss now
                    var boss = Boss;
                    if (boss != null && boss.hp > 0 && boss.st >= BossFighting && boss.st < BossDown && Overlap(bb.xMin, bb.yMin, bb.xMax, bb.yMax, BossBox(boss)))
                    {
                        bo.alive = false;
                        HitBoss(boss, bo);
                    }
                    continue;
                }
                if (atk && bo.kind != 4 && Overlap(ax0, ay0, ax1, ay1, bb))
                {
                    if (bo.kind == 2 || bo.kind == 3)
                    {
                        bo.reflected = true; bo.vx = BossReflectSpeed;     // parry it back at the Guardian
                        Emit(SimEventType.Deflect, bo.x, bo.y, "reflect");
                    }
                    else { bo.alive = false; Emit(SimEventType.Deflect, bo.x, bo.y); }
                    continue;
                }
                if (Overlap(px0, py0, px1, py1, bb))
                {
                    bo.alive = false;
                    if (!dashing) Hurt(); else Emit(SimEventType.Deflect, bo.x, bo.y);
                }
            }
        }

        void KillDrone(SimEntity e, string how)
        {
            e.alive = false; stats.drones++;
            Emit(SimEventType.DroneDown, e.x, e.y, how);
        }

        void Hurt()
        {
            if (p.inv > 0 || state != SimState.Running) return;
            p.hp -= 1; p.inv = 1.25f; p.hurtT = 0.35f; stats.hits++;
            // during the lockdown escape every hit also costs time
            if (escaping) escapeT = Mathf.Max(0, escapeT - EscapeHitPenalty);
            Emit(SimEventType.Hurt, p.x, p.y, escaping ? "time" : null);
            if (p.hp <= 0) Die("hp");
        }

        void Die(string cause)
        {
            if (state != SimState.Running) return;
            state = SimState.Dead; stats.deaths++;
            lastDeath = cause;
            Emit(SimEventType.Dead, p.x, p.y, cause);
        }

        /// <summary>Restores everything past the last checkpoint and respawns there.</summary>
        public void Respawn()
        {
            // the vault needs every fragment: go back to the last checkpoint before the first one missed
            if (lastDeath == "vault")
            {
                float missed = float.MaxValue;
                foreach (var e in ents) if (e.t == "fragment" && e.alive) missed = Mathf.Min(missed, e.ox);
                float back = L.spawnX;
                foreach (var e in ents) if (e.t == "checkpoint" && e.ox < missed) back = Mathf.Max(back, e.ox);
                checkpointX = Mathf.Min(checkpointX, back);
            }
            float cx = checkpointX;
            foreach (var e in ents)
            {
                if (e.t == "checkpoint" || e.ox <= cx + 0.01f) continue;
                e.x = e.ox; e.y = e.oy; e.alive = true; e.cd = 0.8f;
                e.st = 0; e.vy = 0; e.step = 0;
                if (e.t == "boss") e.hp = BossMaxHp;
            }
            int collected = 0;
            foreach (var e in ents) if (e.t == "byte" && !e.alive) collected++;
            stats.bytes = collected;
            int frags = 0;
            foreach (var e in ents) if (e.t == "fragment" && !e.alive) frags++;
            stats.fragments = frags;
            bool evidenceTaken = false;
            foreach (var e in ents) if (e.t == "evidence" && !e.alive) evidenceTaken = true;
            stats.evidence = evidenceTaken;
            escaping = evidenceTaken;
            escapeT = evidenceTaken ? EscapeTime : 0;
            stats.drones = cpDrones;
            stats.shard = false;
            bolts.Clear(); empT = 0; magnetT = 0;
            state = SimState.Running;
            Spawn(cx);
            p.inv = 1f;
            Emit(SimEventType.Respawn, cx, p.y);
        }

        // ------------------------------------------------------------------ Level 3 boss
        // The Corporate Security Guardian. The runner stops at the arena line (e.w) and holds position;
        // the Guardian cycles LASER -> ORB VOLLEY -> GROUND SLAM. Laser bolts and orbs can be slashed
        // back into it (that is the only way to hurt it); shockwaves must be jumped. Every 3 hits
        // it enters a faster phase. All timing runs from the moment the fight starts, so it is
        // deterministic and the playtest bot can prove it beatable.
        public const int BossMaxHp = 9;
        public const int BossIdle = 0, BossFighting = 1, BossDown = 9;
        public const float BossReflectSpeed = 20f, EscapeTime = 34f, EscapeHitPenalty = 3f;
        const int AtkLaser = 0, AtkOrbs = 1, AtkSlam = 2;

        public static int BossPhase(SimEntity b) => b.hp > 6 ? 1 : b.hp > 3 ? 2 : 3;
        static float BossPace(SimEntity b) => BossPhase(b) == 1 ? 1f : BossPhase(b) == 2 ? 1.2f : 1.4f;

        /// <summary>Hit box of the Guardian's body (4 units wide, 4 tall, standing on the floor).</summary>
        public static Rect BossBox(SimEntity b) => Rect.MinMaxRect(b.x - 1.6f, b.y, b.x + 1.6f, b.y + 4f);

        public static Rect BoltBox(SimBolt b) =>
            b.kind == 4 ? Rect.MinMaxRect(b.x - 0.45f, b.y, b.x + 0.45f, b.y + 0.6f)
            : b.kind == 3 ? Rect.MinMaxRect(b.x - 0.22f, b.y - 0.22f, b.x + 0.22f, b.y + 0.22f)
            : b.sniper || b.kind == 2 ? Rect.MinMaxRect(b.x - 0.4f, b.y - 0.12f, b.x + 0.4f, b.y + 0.12f)
            : Rect.MinMaxRect(b.x - 0.25f, b.y - 0.12f, b.x + 0.25f, b.y + 0.12f);

        /// <summary>What the Guardian is doing now: 0 laser aim, 1 orb charge, 2 slam wind-up, -1 recovering.</summary>
        public static int BossTelegraphing(SimEntity b) => b.st == 2 ? b.step % 3 : -1;

        /// <summary>Seconds left in the current telegraph (drives the warning visuals).</summary>
        public static float BossTelegraphLeft(SimEntity b) => b.st == 2 ? b.cd : 0;

        void UpdateBoss(SimEntity e, float dt)
        {
            float pace = BossPace(e);
            switch (e.st)
            {
                case BossIdle:
                    if (p.x >= e.w - 0.01f)
                    {
                        e.st = 2; e.step = 0; e.cd = 1.4f;           // first attack after a short stare-down
                        p.x = e.w; p.vy = Mathf.Min(p.vy, 0);
                        Emit(SimEventType.BossStart, e.x, e.y);
                        Emit(SimEventType.BossTelegraph, e.x, e.y, "laser");
                    }
                    break;
                case 2:   // telegraph of the current attack
                    e.cd -= dt;
                    if (e.cd > 0) break;
                    int atk = e.step % 3;
                    if (atk == AtkLaser)
                    {
                        bolts.Add(new SimBolt { x = e.x - 1.8f, y = GroundAt(p.x) + 0.8f, vx = -18f * pace, kind = 2 });
                        Emit(SimEventType.BossFire, e.x - 1.8f, GroundAt(p.x) + 0.8f, "laser");
                        e.st = 3; e.cd = 0.9f;
                    }
                    else if (atk == AtkOrbs)
                    {
                        e.st = 4; e.vy = 0; e.cd = 0;                // vy counts orbs fired
                    }
                    else
                    {
                        bolts.Add(new SimBolt { x = e.x - 1.8f, y = e.y, vx = -10f * pace, kind = 4 });
                        Emit(SimEventType.BossFire, e.x - 1.8f, e.y, "slam");
                        e.st = 3; e.cd = 1.1f;
                    }
                    break;
                case 4:   // orb volley: low, chest, low
                    e.cd -= dt;
                    if (e.cd <= 0)
                    {
                        int n = (int)e.vy;
                        float h = n % 2 == 0 ? 0.3f : 0.8f;
                        bolts.Add(new SimBolt { x = e.x - 1.8f, y = GroundAt(p.x) + h, vx = -9f * pace, kind = 3 });
                        Emit(SimEventType.BossFire, e.x - 1.8f, GroundAt(p.x) + h, "orb");
                        e.vy = n + 1;
                        e.cd = 0.65f / pace;
                        if (e.vy >= 3) { e.st = 3; e.cd = 1.2f; }
                    }
                    break;
                case 3:   // recovery, then the next attack's telegraph
                    e.cd -= dt;
                    if (e.cd <= 0)
                    {
                        e.step++;
                        e.st = 2;
                        int next = e.step % 3;
                        e.cd = (next == AtkLaser ? 1.0f : next == AtkOrbs ? 0.7f : 0.85f) / pace;
                        Emit(SimEventType.BossTelegraph, e.x, e.y, next == AtkLaser ? "laser" : next == AtkOrbs ? "orbs" : "slam");
                    }
                    break;
                case 5:   // defeated: falls apart, then the way opens
                    e.cd -= dt;
                    if (e.cd <= 0) e.st = BossDown;
                    break;
            }
        }

        void HitBoss(SimEntity e, SimBolt b)
        {
            int phase = BossPhase(e);
            e.hp--;
            Emit(SimEventType.BossHit, b.x, b.y, null, e.hp <= 0);
            if (e.hp <= 0)
            {
                e.st = 5; e.cd = 2.0f;
                // the arena goes quiet (flag them; HitBoss runs inside the bolt loop, so no removing here)
                foreach (var o in bolts) if (!o.reflected) o.alive = false;
                stats.drones++;
                Emit(SimEventType.BossDefeated, e.x, e.y + 2f);
                return;
            }
            if (BossPhase(e) != phase) Emit(SimEventType.BossPhase, e.x, e.y + 2f, BossPhase(e).ToString());
        }

        // ------------------------------------------------------------------ results
        public string Rank()
        {
            float pct = totalBytes > 0 ? stats.bytes / (float)totalBytes : 1f;
            if (pct >= 0.85f && stats.hits == 0 && stats.deaths == 0) return "S";
            if (pct >= 0.7f && stats.hits <= 2 && stats.deaths == 0) return "A";
            if (pct >= 0.45f) return "B";
            return "C";
        }

        public int Score()
        {
            int timeBonus = Mathf.Max(0, Mathf.RoundToInt((L.parTime * 1.3f - time) * 20f));
            return stats.bytes * 10 + (stats.shard ? 500 : 0) + stats.drones * 25 + timeBonus
                   + stats.fragments * 150 + (stats.evidence ? 1000 : 0)
                   + (stats.hits == 0 ? 300 : 0) - stats.deaths * 100;
        }
    }
}
