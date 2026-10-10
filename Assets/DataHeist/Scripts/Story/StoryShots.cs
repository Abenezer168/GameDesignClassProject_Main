using System;
using System.Collections.Generic;
using UnityEngine;

namespace DataHeist
{
    /// <summary>
    /// One staged shot of the story intro: a little diorama built from game sprites, a camera
    /// move, and the caption shown while it plays. Shots live side by side along the x axis
    /// (one per Spacing), so the shared parallax sky and rain carry across cuts.
    /// </summary>
    public abstract class StoryShot
    {
        public const float Spacing = 400f;

        public string chapter, text;
        /// <summary>Character id whose portrait frames the caption (optional).</summary>
        public string portrait;
        protected Transform root;
        protected StoryController story;
        protected float X;        // world x where this shot is staged

        public void Init(StoryController s, int index)
        {
            story = s;
            X = index * Spacing;
            root = new GameObject($"Shot{index + 1}_{GetType().Name}").transform;
            root.SetParent(s.transform, false);
            Build();
            root.gameObject.SetActive(false);
        }

        public void SetActive(bool on) => root.gameObject.SetActive(on);

        protected abstract void Build();

        /// <summary>Camera centre at time t (seconds since the shot started).</summary>
        public abstract Vector2 CameraAt(float t);

        public virtual void Enter() { }
        public virtual void Tick(float t, float dt) { }
        public virtual void Exit() { }

        // ---------------------------------------------------------------- staging helpers
        protected static float HalfW => DH.OrthoSize * Camera.main.aspect;
        protected const float CamY = DH.OrthoSize - 1.7f;     // same framing as the menus
        // rooftop shots sit lower so the ledge clears the 76 px caption bar
        protected const float GroundCamY = 2.25f;

        protected SpriteRenderer Sprite(string name, string path, int order, float x, float y)
        {
            return SR.Make(name, root, path != null ? SpriteLibrary.Get(path) : null, order, new Vector3(SR.Snap(x), SR.Snap(y), 0));
        }

        protected SpriteRenderer Glow(float x, float y, Color c, float scale, int order)
        {
            var g = Sprite("Glow", "FX/glow", order, x, y);
            g.color = c;
            g.transform.localScale = new Vector3(scale, scale, 1);
            return g;
        }

        protected SpriteRenderer Beam(float x, float y, Color c, int order, float angle)
        {
            var b = Sprite("Beam", "Story/searchlight", order, x, y);
            b.color = c;
            b.transform.rotation = Quaternion.Euler(0, 0, angle);
            return b;
        }

        /// <summary>Rooftop ledge + facade from x0 for width units at height 0.</summary>
        protected void Rooftop(float x0, float width, string style = "violet")
        {
            SR.Tiled("Ledge", root, SpriteLibrary.Get($"Tiles/rooftop_{style}"), DH.SortRoofTop, new Vector3(x0, 0.06f, 0), new Vector2(width, 0.5f));
            SR.Tiled("Facade", root, SpriteLibrary.Get($"Tiles/facade_{style}_0"), DH.SortFacade, new Vector3(x0, -0.44f, 0), new Vector2(width, 8));
        }

        protected SpriteRenderer Decor(string kind, float x)
        {
            var d = Sprite(kind, "Decor/" + kind, DH.SortDecor, x, -1f / DH.PPU);
            return d;
        }

        protected static Sprite Frame(Sprite[] frames, float t, float fps, float offset = 0)
        {
            if (frames == null || frames.Length == 0) return null;
            return frames[Mathf.FloorToInt((t + offset) * fps) % frames.Length];
        }

        protected static float Ease(float t) => Mathf.SmoothStep(0, 1, Mathf.Clamp01(t));
    }

    // ======================================================================= 1. SECTOR 7
    /// <summary>Establishing shot: the skyline in the rain, the Halcyon tower looming behind it.</summary>
    public class ShotCity : StoryShot
    {
        SpriteRenderer _tower, _beacon;
        readonly List<SpriteRenderer> _beams = new List<SpriteRenderer>();
        SpriteRenderer[] _drones;
        Sprite[] _droneFrames;

        protected override void Build()
        {
            _tower = Sprite("HalcyonTower", "Story/tower", DH.SortFar + 2, X + 4f, -5f);
            _tower.color = new Color(0.62f, 0.55f, 0.82f);   // distance haze
            _beacon = Glow(X + 4f, 8.85f, DH.Red, 0.6f, DH.SortFar + 3);
            float[] bx = { -6f, 1.5f, 9f };
            Color[] bc = { new Color(1, 1, 1, 0.16f), new Color(DH.Cyan.r, DH.Cyan.g, DH.Cyan.b, 0.18f), new Color(1, 1, 1, 0.13f) };
            for (int i = 0; i < bx.Length; i++) _beams.Add(Beam(X + bx[i], 0.5f, bc[i], DH.SortFar + 1, 180f));
            _droneFrames = SpriteLibrary.Anim("Enemies/drone_scout");
            _drones = new[] { Sprite("Drone", null, DH.SortMid + 1, 0, 0), Sprite("Drone", null, DH.SortMid + 1, 0, 0) };
        }

        public override Vector2 CameraAt(float t) => new Vector2(X - 3f + t * 0.45f, CamY);

        public override void Tick(float t, float dt)
        {
            for (int i = 0; i < _beams.Count; i++)
                _beams[i].transform.rotation = Quaternion.Euler(0, 0, 180f + Mathf.Sin(t * (0.35f + i * 0.13f) + i * 2f) * 24f);
            _beacon.enabled = Mathf.Repeat(t, 1.4f) < 0.7f;
            for (int i = 0; i < _drones.Length; i++)
            {
                float dir = i == 0 ? 1 : -1;
                float x = X + (i == 0 ? -12f : 14f) + dir * t * 1.6f;
                _drones[i].sprite = Frame(_droneFrames, t, 10, i * 0.3f);
                _drones[i].flipX = dir < 0;
                _drones[i].transform.position = new Vector3(SR.Snap(x), SR.Snap(6.6f - i * 1.2f + Mathf.Sin(t * 2.4f + i) * 0.12f), 0);
            }
        }
    }

    // ======================================================================= 2. HALCYON
    /// <summary>The tower up close: crown, corporate mark and a drone patrol sweeping red light.</summary>
    public class ShotTower : StoryShot
    {
        SpriteRenderer _beacon;
        readonly List<(SpriteRenderer drone, SpriteRenderer beam, float baseX, float baseY, float phase)> _patrol =
            new List<(SpriteRenderer, SpriteRenderer, float, float, float)>();
        Sprite[] _droneFrames;

        protected override void Build()
        {
            Sprite("HalcyonTower", "Story/tower", DH.SortNear + 2, X, -5f);
            _beacon = Glow(X, 8.85f, DH.Red, 0.9f, DH.SortNear + 3);
            _droneFrames = SpriteLibrary.Anim("Enemies/drone_scout");
            float[,] spots = { { -4.5f, 6.2f }, { 3.5f, 4.6f }, { -1.5f, 2.6f } };
            for (int i = 0; i < 3; i++)
            {
                var d = Sprite("Drone", null, DH.SortEnemies, 0, 0);
                var beam = Beam(0, 0, new Color(DH.Red.r, DH.Red.g, DH.Red.b, 0.28f), DH.SortEnemies - 1, 0);
                beam.transform.SetParent(d.transform, false);
                beam.transform.localPosition = new Vector3(0, -0.15f, 0);
                _patrol.Add((d, beam, X + spots[i, 0], spots[i, 1], i * 1.7f));
            }
        }

        public override Vector2 CameraAt(float t) => new Vector2(X - 4.5f + Ease(t / 9f) * 4.5f, CamY + 0.2f);

        public override void Tick(float t, float dt)
        {
            _beacon.enabled = Mathf.Repeat(t, 1.2f) < 0.6f;
            foreach (var (d, beam, bx, by, ph) in _patrol)
            {
                float x = bx + Mathf.Sin(t * 0.5f + ph) * 2.2f;
                d.sprite = Frame(_droneFrames, t, 10, ph);
                d.flipX = Mathf.Cos(t * 0.5f + ph) < 0;
                d.transform.position = new Vector3(SR.Snap(x), SR.Snap(by + Mathf.Sin(t * 3f + ph) * 0.12f), 0);
                beam.transform.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(t * 1.1f + ph) * 22f);
            }
        }
    }

    // ======================================================================= 3. THE SHARDS
    /// <summary>The vault: a Data Shard floating over its pedestal, orbited by data.</summary>
    public class ShotVault : StoryShot
    {
        SpriteRenderer _shard, _glow, _ring;
        Sprite[] _shardFrames, _ringFrames, _byteFrames;
        readonly List<SpriteRenderer> _bytes = new List<SpriteRenderer>();
        Vector2 _shardPos;

        protected override void Build()
        {
            const float cy = CamY + 1f;   // lifted so the pedestal clears the caption bar
            Sprite("Vault", "Story/vault", DH.SortNear + 10, X, cy);
            float top = cy + DH.OrthoSize;
            _shardPos = new Vector2(X, top - 252f / DH.PPU + 1.75f);
            _glow = Glow(_shardPos.x, _shardPos.y, new Color(DH.Pink.r, DH.Pink.g, DH.Pink.b, 0.45f), 3f, DH.SortPickups);
            _shardFrames = SpriteLibrary.Anim("Pickups/shard");
            _shard = Sprite("Shard", null, DH.SortPickups + 2, _shardPos.x, _shardPos.y);
            _shard.transform.localScale = new Vector3(2, 2, 1);
            _ringFrames = SpriteLibrary.Anim("FX/ring");
            _ring = Sprite("Ring", null, DH.SortPickups + 1, _shardPos.x, _shardPos.y);
            _ring.color = new Color(DH.Cyan.r, DH.Cyan.g, DH.Cyan.b, 0.7f);
            _ring.transform.localScale = new Vector3(3, 3, 1);
            _byteFrames = SpriteLibrary.Anim("Pickups/byte");
            for (int i = 0; i < 10; i++) _bytes.Add(Sprite("Byte", null, DH.SortPickups + 3, 0, 0));
        }

        public override Vector2 CameraAt(float t) => new Vector2(X, CamY);  // vault art is exactly one screen

        public override void Enter() => AudioManager.I.Sfx("shard", 0.55f, 0.8f);

        public override void Tick(float t, float dt)
        {
            float bob = Mathf.Sin(t * 1.6f) * 0.18f;
            _shard.sprite = Frame(_shardFrames, t, 6);
            _shard.transform.position = new Vector3(_shardPos.x, SR.Snap(_shardPos.y + bob), 0);
            float pulse = 0.35f + Mathf.Sin(t * 2.2f) * 0.12f;
            _glow.color = new Color(DH.Pink.r, DH.Pink.g, DH.Pink.b, pulse);
            // ring bursts every 1.6 s
            float rt = Mathf.Repeat(t, 1.6f) / 0.6f;
            _ring.enabled = rt < 1f && _ringFrames.Length > 0;
            if (_ring.enabled) _ring.sprite = _ringFrames[Mathf.Min(_ringFrames.Length - 1, (int)(rt * _ringFrames.Length))];
            for (int i = 0; i < _bytes.Count; i++)
            {
                float a = t * 0.9f + i * Mathf.PI * 2 / _bytes.Count;
                float depth = Mathf.Sin(a);
                var b = _bytes[i];
                b.sprite = Frame(_byteFrames, t, 10, i * 0.13f);
                b.sortingOrder = DH.SortPickups + (depth > 0 ? 3 : 0);
                b.color = new Color(1, 1, 1, depth > 0 ? 1f : 0.45f);
                b.transform.position = new Vector3(SR.Snap(_shardPos.x + Mathf.Cos(a) * 2.6f), SR.Snap(_shardPos.y + bob * 0.5f + depth * 0.45f), 0);
            }
        }
    }

    // ======================================================================= 4. THE COURIER
    /// <summary>Kai alone on a rooftop in the rain; a job offer flickers up from his wrist.</summary>
    public class ShotCourier : StoryShot
    {
        SpriteRenderer _kai, _holo;
        Sprite[] _idle;
        StoryController.WorldLabel _offer;
        bool _shown;
        Vector3 _holoPos;

        protected override void Build()
        {
            Rooftop(X - 24, 60);
            Decor("antenna", X - 6.5f);
            Decor("water_tank", X - 3.2f);
            Decor("sign_pink", X + 6.4f);
            Decor("vent", X + 8.8f);
            _idle = SpriteLibrary.CharacterAnim("kai", "idle");
            _kai = Sprite("Kai", null, DH.SortPlayer, X + 1.2f, 1f / DH.PPU);
            Glow(X + 1.2f, 0.7f, new Color(DH.Cyan.r, DH.Cyan.g, DH.Cyan.b, 0.18f), 2.2f, DH.SortPlayer - 1);
            _holoPos = new Vector3(X + 3.4f, 2.6f, 0);
            _holo = Sprite("Hologram", "Story/holo_frame", DH.SortFX, _holoPos.x, _holoPos.y);
            _holo.enabled = false;
            _offer = story.AddLabel("<color=#ff3fa4>INCOMING JOB</color>\n1 DATA SHARD\n<color=#2ff3ff>PAYOUT 10,000 B</color>", 5, DH.Ink, () => _holoPos);
            _offer.Visible = false;
        }

        public override Vector2 CameraAt(float t) => new Vector2(X - 3.2f + Ease(t / 7f) * 4.4f, GroundCamY);

        public override void Enter() { _shown = false; _holo.enabled = false; _offer.Visible = false; }

        public override void Exit() => _offer.Visible = false;

        public override void Tick(float t, float dt)
        {
            _kai.sprite = Frame(_idle, t, 5);
            if (!_shown && t > 2.2f)
            {
                _shown = true;
                AudioManager.I.Sfx("unlock", 0.6f, 1.3f);
            }
            if (!_shown) return;
            float k = Mathf.Clamp01((t - 2.2f) / 0.25f);
            bool flicker = k < 1 && Mathf.Repeat(t * 30, 2) < 1;
            _holo.enabled = !flicker;
            _holo.transform.localScale = new Vector3(1, Mathf.Max(0.05f, k), 1);
            _offer.Visible = k >= 1;
        }
    }

    // ======================================================================= 5. THE CREW
    /// <summary>The four runners stepping into the light one at a time, names called out.</summary>
    public class ShotCrew : StoryShot
    {
        struct Member
        {
            public CharacterDefinition def;
            public SpriteRenderer sr, beam;
            public Sprite[] idle;
            public StoryController.WorldLabel label;
            public float x;
        }

        readonly List<Member> _crew = new List<Member>();
        int _revealed;

        protected override void Build()
        {
            Rooftop(X - 24, 60, "teal");
            Decor("ac_unit", X - 7.5f);
            Decor("antenna", X + 11.5f);
            var roster = CharacterDefinition.All;
            for (int i = 0; i < roster.Count; i++)
            {
                var c = roster[i];
                float x = X - 3.9f + i * 2.6f;
                var m = new Member
                {
                    def = c, x = x,
                    idle = SpriteLibrary.CharacterAnim(c.id, "idle"),
                    sr = Sprite(c.id, null, DH.SortPlayer, x, 1f / DH.PPU),
                    beam = Beam(x, 9.5f, new Color(c.color.r, c.color.g, c.color.b, 0.2f), DH.SortDecor + 1, 0),
                };
                var pos = new Vector3(x, 2.7f, 0);
                m.label = story.AddLabel($"<color=#{ColorUtility.ToHtmlStringRGB(c.color)}>{c.displayName.Split(' ')[0]}</color>\n<size=4><color=#8a7fb0>{c.title.ToUpperInvariant()}</color></size>",
                    6, DH.Ink, () => pos);
                _crew.Add(m);
            }
        }

        public override Vector2 CameraAt(float t) => new Vector2(X - 0.6f + Ease(t / 8f) * 1.2f, GroundCamY);

        public override void Enter()
        {
            _revealed = 0;
            foreach (var m in _crew) { m.label.Visible = false; m.beam.enabled = false; }
        }

        public override void Exit() { foreach (var m in _crew) m.label.Visible = false; }

        public override void Tick(float t, float dt)
        {
            int shouldReveal = Mathf.Clamp(Mathf.FloorToInt((t - 0.5f) / 0.8f) + 1, 0, _crew.Count);
            while (_revealed < shouldReveal)
            {
                var m = _crew[_revealed++];
                m.beam.enabled = true;
                m.label.Visible = true;
                AudioManager.I.Sfx("ui_select", 0.6f, 0.9f + _revealed * 0.08f);
            }
            for (int i = 0; i < _crew.Count; i++)
            {
                var m = _crew[i];
                m.sr.sprite = Frame(m.idle, t, 5, i * 0.37f);
                m.sr.color = i < _revealed ? Color.white : new Color(0.05f, 0.03f, 0.1f, 1);
            }
        }
    }

    // ======================================================================= 6. THE HEIST
    /// <summary>The crew sprinting across the rooftops with security drones on their tail.</summary>
    public class ShotRun : StoryShot
    {
        const float Speed = 7.5f;
        readonly List<(SpriteRenderer sr, Sprite[] run, float lead)> _runners = new List<(SpriteRenderer, Sprite[], float)>();
        readonly List<(SpriteRenderer drone, SpriteRenderer beam, float lead, float y)> _chasers = new List<(SpriteRenderer, SpriteRenderer, float, float)>();
        Sprite[] _droneFrames;

        protected override void Build()
        {
            Rooftop(X - 24, 260);
            var rng = new System.Random(3);
            string[] props = { "ac_unit", "vent", "antenna", "water_tank", "sign_cyan", "vent" };
            for (float x = X + 4; x < X + 230; x += 6 + (float)rng.NextDouble() * 8)
                Decor(props[rng.Next(props.Length)], x);
            var roster = CharacterDefinition.All;
            float[] lead = { 9.2f, 7.4f, 5.8f, 4.1f };
            for (int i = 0; i < roster.Count && i < lead.Length; i++)
                _runners.Add((Sprite(roster[i].id, null, DH.SortPlayer - i, 0, 0), SpriteLibrary.CharacterAnim(roster[i].id, "run"), lead[i]));
            _droneFrames = SpriteLibrary.Anim("Enemies/drone_scout");
            for (int i = 0; i < 2; i++)
            {
                var d = Sprite("Chaser", null, DH.SortEnemies, 0, 0);
                var beam = Beam(0, 0, new Color(DH.Red.r, DH.Red.g, DH.Red.b, 0.3f), DH.SortEnemies - 1, 0);
                beam.transform.SetParent(d.transform, false);
                beam.transform.localPosition = new Vector3(0, -0.15f, 0);
                _chasers.Add((d, beam, 1.2f + i * 1.4f, 3.4f + i * 1.1f));
            }
        }

        public override Vector2 CameraAt(float t) => new Vector2(X + t * Speed, GroundCamY);

        public override void Enter()
        {
            AudioManager.I.Sfx("alarm", 0.5f);
            story.Alarm = true;
        }

        public override void Exit() => story.Alarm = false;

        public override void Tick(float t, float dt)
        {
            float camX = CameraAt(t).x, left = camX - HalfW;
            for (int i = 0; i < _runners.Count; i++)
            {
                var (sr, run, lead) = _runners[i];
                sr.sprite = Frame(run, t, 14, i * 0.21f);
                float surge = Mathf.Sin(t * 0.9f + i) * 0.35f;
                sr.transform.position = new Vector3(SR.Snap(left + lead + surge), 1f / DH.PPU, 0);
            }
            for (int i = 0; i < _chasers.Count; i++)
            {
                var (d, beam, lead, y) = _chasers[i];
                float catchUp = Mathf.Min(1.5f, t * 0.25f);
                d.sprite = Frame(_droneFrames, t, 12, i * 0.4f);
                d.transform.position = new Vector3(SR.Snap(left + lead + catchUp), SR.Snap(y + Mathf.Sin(t * 3.2f + i) * 0.15f), 0);
                beam.transform.localRotation = Quaternion.Euler(0, 0, -20f + Mathf.Sin(t * 1.7f + i) * 14f);
            }
        }
    }
}
