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
            }
            _views.Add(v);
        }

        void LateUpdate()
        {
            if (_sim == null) return;
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
                    case "laser":
                    {
                        bool off = _sim.empT > 0;
                        bool flick = off && Mathf.FloorToInt(t * 12f) % 7 == 0;
                        v.beam.enabled = !off || flick;
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
                if (_boltFrames.Length > 0) sr.sprite = _boltFrames[Mathf.FloorToInt(t * 20f) % _boltFrames.Length];
                sr.transform.position = new Vector3(SR.Snap(b.x), SR.Snap(b.y), 0);
            }
            for (; i < _boltPool.Count; i++) _boltPool[i].gameObject.SetActive(false);
        }
    }
}
