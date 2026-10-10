using System.Collections.Generic;
using UnityEngine;

namespace DataHeist
{
    /// <summary>Animates the runner sprite from simulation state (same frame rules as the browser build).</summary>
    public class PlayerView : MonoBehaviour
    {
        RunnerSim _sim;
        CharacterDefinition _ch;
        SpriteRenderer _body, _shadow, _slash, _ring, _echo;
        readonly Dictionary<string, Sprite[]> _anims = new Dictionary<string, Sprite[]>();
        Sprite[] _slashFrames, _ringFrames;
        readonly List<SpriteRenderer> _ghosts = new List<SpriteRenderer>();
        readonly List<float> _ghostLife = new List<float>();
        float _runDist, _lastX, _slashT = -1, _ringT = -1;
        public float GlitchTime;   // set by LevelController while Time Glitch is active

        public void Init(RunnerSim sim)
        {
            _sim = sim; _ch = sim.ch;
            foreach (var a in new[] { "idle", "run", "jump", "fall", "slide", "attack", "hurt", "ability", "win" })
                _anims[a] = SpriteLibrary.CharacterAnim(_ch.id, a);
            _shadow = SR.Box("Shadow", transform, new Color(0.04f, 0.015f, 0.08f, 0.45f), DH.SortPlayer - 2, -0.25f, -1f / DH.PPU, 0.5f, 2f / DH.PPU);
            _shadow.transform.SetParent(null);
            _echo = SR.Make("GlitchEcho", transform, null, DH.SortPlayer - 1, Vector3.zero);
            _body = SR.Make("Body", transform, null, DH.SortPlayer, Vector3.zero);
            _slashFrames = SpriteLibrary.Anim("FX/slash");
            _ringFrames = SpriteLibrary.Anim("FX/ring");
            _slash = SR.Make("Slash", transform, null, DH.SortFX, Vector3.zero);
            _slash.color = _ch.color;
            _ring = SR.Make("EmpRing", transform, null, DH.SortFX, new Vector3(0, 0.55f, 0));
            _ring.color = DH.Hex("38b6ff");
            _lastX = sim.p.x;
        }

        public void OnAttack() => _slashT = 0;
        public void OnEmp() => _ringT = 0;
        public void ResetRun() { _lastX = _sim.p.x; }

        Sprite Frame(string anim, int i)
        {
            if (!_anims.TryGetValue(anim, out var f) || f.Length == 0) return null;
            return f[Mathf.Clamp(i, 0, f.Length - 1) % f.Length];
        }

        Sprite CurrentFrame()
        {
            var p = _sim.p;
            float now = Time.unscaledTime;
            if (_sim.state == SimState.Won) return Frame("win", Mathf.FloorToInt(now / 0.3f) % 2);
            if (p.hurtT > 0) return Frame("hurt", p.hurtT > 0.17f ? 0 : 1);
            if (p.dashT > 0) return Frame("ability", Mathf.FloorToInt(now / 0.06f) % 2);
            if (p.attackT >= 0) return Frame("attack", Mathf.Min(3, Mathf.FloorToInt(p.attackT / 0.07f)));
            if (p.sliding) return Frame("slide", Mathf.FloorToInt(now / 0.12f) % 2);
            if (!p.onGround)
            {
                if (p.vy > 4) return Frame("jump", 0);
                if (p.vy > -3) return Frame("jump", 1);
                return Frame("fall", Mathf.FloorToInt(now / 0.12f) % 2);
            }
            // feet planted (the Guardian's arena): stand ready instead of running in place
            if (_sim.SpeedNow() <= 0f) return Frame("idle", Mathf.FloorToInt(now / 0.2f) % 4);
            return Frame("run", Mathf.FloorToInt(_runDist / 0.42f) % 8);
        }

        void LateUpdate()
        {
            if (_sim == null) return;
            var p = _sim.p;
            float dt = Time.deltaTime;
            if (p.onGround) _runDist += Mathf.Max(0, p.x - _lastX);
            _lastX = p.x;

            transform.position = new Vector3(SR.Snap(p.x), SR.Snap(p.y) - 1f / DH.PPU, 0);
            var frame = CurrentFrame();
            _body.sprite = frame;
            bool blink = p.inv > 0 && _sim.state == SimState.Running && Mathf.FloorToInt(Time.unscaledTime * 14f) % 2 == 0;
            _body.color = blink ? new Color(1, 1, 1, 0.35f) : Color.white;
            _body.enabled = !(_sim.state == SimState.Dead && p.y < _sim.L.killY);

            // glitch echo
            _echo.enabled = GlitchTime > 0;
            if (_echo.enabled)
            {
                _echo.sprite = frame;
                _echo.transform.localPosition = new Vector3(-3f / DH.PPU, 0, 0);
                _echo.color = new Color(DH.Green.r, DH.Green.g, DH.Green.b, 0.35f);
            }

            // shadow on the roof below
            float gy = _sim.GroundAt(p.x);
            bool shadow = !float.IsNegativeInfinity(gy) && p.y - gy < 6;
            _shadow.enabled = shadow;
            if (shadow)
            {
                float w = Mathf.Max(6, 18 - (p.y - gy) * 3) / DH.PPU;
                _shadow.transform.position = new Vector3(SR.Snap(p.x), SR.Snap(gy), 0);
                float unit = SR.Pixel != null ? SR.Pixel.bounds.size.x : 0.125f;
                _shadow.transform.localScale = new Vector3(w / unit, 2f / DH.PPU / unit, 1);
            }

            // slash arc
            if (_slashT >= 0 && _slashFrames.Length > 0)
            {
                _slashT += dt;
                int k = Mathf.FloorToInt(_slashT / 0.05f);
                if (k > 2) { _slashT = -1; _slash.enabled = false; }
                else
                {
                    float s = _ch.attackReach / 1.8f;
                    _slash.enabled = true;
                    _slash.sprite = _slashFrames[Mathf.Min(k, _slashFrames.Length - 1)];
                    _slash.transform.localPosition = new Vector3(0.7f * s, 0.7f, 0);
                    _slash.transform.localScale = Vector3.one * (s + 0.1f);
                }
            }
            else _slash.enabled = false;

            // EMP ring
            if (_ringT >= 0 && _ringFrames.Length > 0)
            {
                _ringT += dt;
                int k = Mathf.FloorToInt(_ringT / 0.06f);
                if (k > 5) { _ringT = -1; _ring.enabled = false; }
                else
                {
                    _ring.enabled = true;
                    _ring.sprite = _ringFrames[Mathf.Min(k, _ringFrames.Length - 1)];
                    _ring.transform.localScale = Vector3.one * (2 + k * 1.6f);
                    var c = _ring.color; c.a = 1 - k / 6f; _ring.color = c;
                }
            }
            else _ring.enabled = false;

            // phase-dash afterimages
            if (p.dashT > 0 && Random.value < 0.8f) SpawnGhost(frame);
            for (int i = 0; i < _ghosts.Count; i++)
            {
                if (!_ghosts[i].enabled) continue;
                _ghostLife[i] -= dt;
                if (_ghostLife[i] <= 0) { _ghosts[i].enabled = false; continue; }
                var c = _ch.color; c.a = _ghostLife[i] * 2.4f;
                _ghosts[i].color = c;
            }
        }

        void SpawnGhost(Sprite frame)
        {
            int idx = _ghosts.FindIndex(g => !g.enabled);
            if (idx < 0)
            {
                _ghosts.Add(SR.Make("Ghost", null, null, DH.SortPlayer - 1, Vector3.zero));
                _ghostLife.Add(0);
                idx = _ghosts.Count - 1;
            }
            var g = _ghosts[idx];
            g.enabled = true;
            g.sprite = frame;
            g.transform.position = transform.position;
            _ghostLife[idx] = 0.25f;
        }

        void OnDestroy()
        {
            if (_shadow != null) Destroy(_shadow.gameObject);
            foreach (var g in _ghosts) if (g != null) Destroy(g.gameObject);
        }
    }
}
