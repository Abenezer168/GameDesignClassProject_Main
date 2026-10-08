using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace DataHeist
{
    /// <summary>Pooled pixel particles, sprite explosions and floating score text.</summary>
    public class FXSystem : MonoBehaviour
    {
        class Particle
        {
            public SpriteRenderer sr;
            public Vector2 pos, vel;
            public float life, max, grav;
            public Sprite[] anim;
            public float animT;
        }

        class Floater
        {
            public TextMeshPro tmp;
            public Vector2 pos;
            public float t;
        }

        readonly List<Particle> _parts = new List<Particle>();
        readonly List<Floater> _floats = new List<Floater>();
        Sprite[] _explosion;
        float _unit;

        void Awake()
        {
            _explosion = SpriteLibrary.Anim("FX/explosion");
            _unit = SR.Pixel != null ? SR.Pixel.bounds.size.x : 0.125f;
        }

        Particle Get()
        {
            foreach (var p in _parts) if (p.life <= 0) return p;
            var np = new Particle { sr = SR.Make("P", transform, SR.Pixel, DH.SortFX, Vector3.zero) };
            _parts.Add(np);
            return np;
        }

        public void Burst(Vector2 at, int n, Color[] colors, float speed, float life, float gravity = -12f)
        {
            for (int i = 0; i < n; i++)
            {
                var p = Get();
                float a = Random.value * Mathf.PI * 2f, v = speed * (0.3f + Random.value * 0.7f);
                p.pos = at; p.vel = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * v;
                p.max = p.life = life * (0.6f + Random.value * 0.4f);
                p.grav = gravity; p.anim = null;
                p.sr.sprite = SR.Pixel;
                p.sr.color = colors[i % colors.Length];
                float size = (Random.value < 0.3f ? 4f : 2f) / DH.PPU;
                p.sr.transform.localScale = Vector3.one * (size / _unit);
                p.sr.enabled = true;
            }
        }

        public void Explosion(Vector2 at)
        {
            if (_explosion.Length == 0) return;
            var p = Get();
            p.pos = at; p.vel = Vector2.zero; p.grav = 0;
            p.max = p.life = 0.42f; p.anim = _explosion; p.animT = 0;
            p.sr.color = Color.white;
            p.sr.transform.localScale = Vector3.one;
            p.sr.enabled = true;
        }

        public void FloatText(Vector2 at, string text, Color color)
        {
            Floater f = _floats.Find(x => x.t >= 1.1f);
            if (f == null)
            {
                var go = new GameObject("FloatText");
                go.transform.SetParent(transform, false);
                var tmp = go.AddComponent<TextMeshPro>();
                tmp.font = UIKit.Font;
                tmp.fontSize = 2.6f;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.textWrappingMode = TextWrappingModes.NoWrap;
                tmp.outlineWidth = 0.25f;
                tmp.outlineColor = new Color32(18, 10, 26, 255);
                tmp.GetComponent<MeshRenderer>().sortingOrder = DH.SortText;
                f = new Floater { tmp = tmp };
                _floats.Add(f);
            }
            f.tmp.gameObject.SetActive(true);
            f.tmp.text = text;
            f.tmp.color = color;
            f.pos = at; f.t = 0;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            foreach (var p in _parts)
            {
                if (p.life <= 0) continue;
                p.life -= dt;
                if (p.life <= 0) { p.sr.enabled = false; continue; }
                if (p.anim != null)
                {
                    p.animT += dt;
                    p.sr.sprite = p.anim[Mathf.Min(p.anim.Length - 1, Mathf.FloorToInt(p.animT / 0.07f))];
                }
                else
                {
                    p.vel.y += p.grav * dt;
                    p.pos += p.vel * dt;
                    var c = p.sr.color; c.a = Mathf.Min(1, p.life / p.max * 1.5f); p.sr.color = c;
                }
                p.sr.transform.position = new Vector3(SR.Snap(p.pos.x), SR.Snap(p.pos.y), 0);
            }
            foreach (var f in _floats)
            {
                if (f.t >= 1.1f) continue;
                f.t += dt;
                if (f.t >= 1.1f) { f.tmp.gameObject.SetActive(false); continue; }
                f.tmp.transform.position = new Vector3(f.pos.x, f.pos.y + f.t * 1.2f, 0);
                var c = f.tmp.color; c.a = Mathf.Min(1, (1.1f - f.t) * 3f); f.tmp.color = c;
            }
        }
    }
}
