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
        Hurt, Dead, Checkpoint, Dash, DashEnd, Emp, Glitch, AbilityDenied, Shoot, Deflect, Respawn, Win
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
        public bool alive = true;
        public SimEntity Clone() => (SimEntity)MemberwiseClone();
    }

    public class SimBolt
    {
        public float x, y, vx;
        public bool alive = true;
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
        public int bytes, hits, deaths, drones;
        public bool shard;
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
        int cpDrones;

        public RunnerSim(LevelData level, CharacterDefinition character)
        {
            L = level; P = level.physics; ch = character;
            var list = new List<SimEntity>();
            foreach (var e in level.entities)
            {
                list.Add(new SimEntity
                {
                    t = e.t, kind = e.kind, move = e.move, mode = e.mode, h = e.h,
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
            }
            checkpointX = level.spawnX;
            Spawn(level.spawnX);
        }

        RunnerSim(RunnerSim o)
        {
            L = o.L; P = o.P; ch = o.ch;
            ents = new SimEntity[o.ents.Length];
            for (int i = 0; i < ents.Length; i++) ents[i] = o.ents[i].Clone();
            foreach (var b in o.bolts) bolts.Add(new SimBolt { x = b.x, y = b.y, vx = b.vx, alive = b.alive });
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

        public float SpeedNow()
        {
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
                bool ceiling = e.t == "barrier" || (e.t == "laser" && e.mode == "high" && empT <= 0);
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
                        if (e.alive && (e.t == "drone" || e.t == "gunner") && Mathf.Abs(e.x - p.x) < 11f) KillDrone(e, "emp");
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
            }
            return Rect.zero;
        }

        static bool HasBox(SimEntity e) => e.t != "checkpoint" && e.t != "exit";

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
            bolts.RemoveAll(b => !b.alive || b.x < p.x - 20f);
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
                        if (touching && !dashing && empT <= 0) Hurt();
                        break;
                }
            }

            foreach (var bo in bolts)
            {
                if (!bo.alive) continue;
                var bb = Rect.MinMaxRect(bo.x - 0.25f, bo.y - 0.12f, bo.x + 0.25f, bo.y + 0.12f);
                if (atk && Overlap(ax0, ay0, ax1, ay1, bb))
                {
                    bo.alive = false; Emit(SimEventType.Deflect, bo.x, bo.y); continue;
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
            Emit(SimEventType.Hurt, p.x, p.y);
            if (p.hp <= 0) Die("hp");
        }

        void Die(string cause)
        {
            if (state != SimState.Running) return;
            state = SimState.Dead; stats.deaths++;
            Emit(SimEventType.Dead, p.x, p.y, cause);
        }

        /// <summary>Restores everything past the last checkpoint and respawns there.</summary>
        public void Respawn()
        {
            float cx = checkpointX;
            foreach (var e in ents)
            {
                if (e.t == "checkpoint" || e.ox <= cx + 0.01f) continue;
                e.x = e.ox; e.y = e.oy; e.alive = true; e.cd = 0.8f;
            }
            int collected = 0;
            foreach (var e in ents) if (e.t == "byte" && !e.alive) collected++;
            stats.bytes = collected;
            stats.drones = cpDrones;
            stats.shard = false;
            bolts.Clear(); empT = 0; magnetT = 0;
            state = SimState.Running;
            Spawn(cx);
            p.inv = 1f;
            Emit(SimEventType.Respawn, cx, p.y);
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
                   + (stats.hits == 0 ? 300 : 0) - stats.deaths * 100;
        }
    }
}
