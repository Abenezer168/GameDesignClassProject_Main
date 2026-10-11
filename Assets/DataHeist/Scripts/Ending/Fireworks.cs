using UnityEngine;

namespace DataHeist
{
    /// <summary>
    /// Fireworks over the skyline: rockets rise on a spark trail and burst into rings of glowing
    /// particles with a bloom flash and a bang. Pooled sprites; set Active to start/stop launches.
    /// </summary>
    public class Fireworks : MonoBehaviour
    {
        const int Max = 420;
        public bool Active;
        public float rate = 1.1f;            // launches per second
        public Rect area;                    // world-space burst area (x range, y range)

        SpriteRenderer[] _p;
        Vector2[] _v;
        float[] _life, _max;
        Color[] _col;
        SpriteRenderer[] _flash;
        float[] _flashT;
        int _next;
        float _launchT;

        struct Rocket { public Vector2 pos, vel; public float fuse; public Color col; public bool live; }
        readonly Rocket[] _rockets = new Rocket[8];

        static readonly Color[] Palette =
        {
            new Color(1f, 0.25f, 0.64f), new Color(0.18f, 0.95f, 1f), new Color(1f, 0.82f, 0.23f),
            new Color(0.49f, 1f, 0.42f), new Color(0.6f, 0.36f, 1f), Color.white,
        };

        public static Fireworks Create(Transform parent, Rect area)
        {
            var f = new GameObject("Fireworks").AddComponent<Fireworks>();
            f.transform.SetParent(parent, false);
            f.area = area;
            f.Build();
            return f;
        }

        void Build()
        {
            var px = SR.Pixel;
            _p = new SpriteRenderer[Max]; _v = new Vector2[Max]; _life = new float[Max]; _max = new float[Max]; _col = new Color[Max];
            for (int i = 0; i < Max; i++)
            {
                _p[i] = SR.Make("Spark", transform, px, DH.SortFar + 6, Vector3.zero);
                _p[i].transform.localScale = Vector3.one * 0.45f;
                _p[i].enabled = false;
            }
            _flash = new SpriteRenderer[6]; _flashT = new float[6];
            for (int i = 0; i < _flash.Length; i++)
            {
                _flash[i] = SR.Make("Flash", transform, SpriteLibrary.Get("FX/glow"), DH.SortFar + 5, Vector3.zero);
                _flash[i].enabled = false;
            }
        }

        /// <summary>Launches one rocket now (also used for scripted volleys).</summary>
        public void Launch(float x = float.NaN, Color? color = null)
        {
            for (int i = 0; i < _rockets.Length; i++)
            {
                if (_rockets[i].live) continue;
                float tx = float.IsNaN(x) ? Random.Range(area.xMin, area.xMax) : x;
                float ty = Random.Range(area.yMin, area.yMax);
                float startY = area.yMin - 5f;
                float fuse = Random.Range(0.8f, 1.1f);
                _rockets[i] = new Rocket { pos = new Vector2(tx, startY), vel = new Vector2(Random.Range(-0.6f, 0.6f), (ty - startY) / fuse), fuse = fuse,
                    col = color ?? Palette[Random.Range(0, Palette.Length)], live = true };
                AudioManager.I.Sfx("firework_launch", 0.35f, Random.Range(0.9f, 1.2f));
                return;
            }
        }

        void Burst(Vector2 at, Color c)
        {
            int n = Random.Range(48, 72);
            float speed = Random.Range(4.5f, 6.5f);
            bool ring = Random.value < 0.5f;
            for (int k = 0; k < n; k++)
            {
                int i = _next = (_next + 1) % Max;
                float a = k / (float)n * Mathf.PI * 2f + Random.Range(-0.05f, 0.05f);
                float s = ring ? speed : speed * Random.Range(0.3f, 1f);
                _v[i] = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * s;
                _p[i].transform.position = at;
                _life[i] = _max[i] = Random.Range(1.1f, 1.8f);
                _col[i] = Random.value < 0.15f ? Color.white : c;
                _p[i].enabled = true;
            }
            for (int j = 0; j < _flash.Length; j++)
            {
                if (_flashT[j] > 0) continue;
                _flashT[j] = 0.35f;
                _flash[j].enabled = true;
                _flash[j].transform.position = at;
                _flash[j].color = new Color(c.r, c.g, c.b, 0.8f);
                break;
            }
            AudioManager.I.Sfx("firework_" + Random.Range(1, 4), 0.55f, Random.Range(0.85f, 1.15f));
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (Active)
            {
                _launchT -= dt;
                if (_launchT <= 0) { Launch(); _launchT = Random.Range(0.5f, 1.4f) / Mathf.Max(0.1f, rate); }
            }
            for (int i = 0; i < _rockets.Length; i++)
            {
                if (!_rockets[i].live) continue;
                _rockets[i].pos += _rockets[i].vel * dt;
                _rockets[i].fuse -= dt;
                // spark trail
                int k = _next = (_next + 1) % Max;
                _p[k].enabled = true;
                _p[k].transform.position = _rockets[i].pos;
                _v[k] = new Vector2(Random.Range(-0.3f, 0.3f), -1f);
                _life[k] = _max[k] = 0.35f;
                _col[k] = new Color(1f, 0.85f, 0.6f);
                if (_rockets[i].fuse <= 0) { _rockets[i].live = false; Burst(_rockets[i].pos, _rockets[i].col); }
            }
            for (int i = 0; i < Max; i++)
            {
                if (!_p[i].enabled) continue;
                _life[i] -= dt;
                if (_life[i] <= 0) { _p[i].enabled = false; continue; }
                _v[i] *= 1f - 1.6f * dt;
                _v[i].y -= 2.2f * dt;
                _p[i].transform.position += (Vector3)(_v[i] * dt);
                float k = _life[i] / _max[i];
                var c = _col[i]; c.a = k > 0.3f ? 1f : k / 0.3f * (Random.value < 0.3f ? 0.3f : 1f);   // twinkle out
                _p[i].color = c;
            }
            for (int j = 0; j < _flash.Length; j++)
            {
                if (_flashT[j] <= 0) continue;
                _flashT[j] -= dt;
                float s = 1.5f + (0.35f - _flashT[j]) * 6f;
                _flash[j].transform.localScale = Vector3.one * s;
                var c = _flash[j].color; c.a = Mathf.Max(0, _flashT[j] / 0.35f) * 0.7f; _flash[j].color = c;
                if (_flashT[j] <= 0) _flash[j].enabled = false;
            }
        }
    }

    /// <summary>Confetti falling across the view (pooled pixels that flutter as they fall).</summary>
    public class Confetti : MonoBehaviour
    {
        const int N = 160;
        public bool Active;
        SpriteRenderer[] _p;
        Vector3[] _v;
        float[] _phase;

        public static Confetti Create(Transform parent)
        {
            var c = new GameObject("Confetti").AddComponent<Confetti>();
            c.transform.SetParent(parent, false);
            c._p = new SpriteRenderer[N]; c._v = new Vector3[N]; c._phase = new float[N];
            Color[] cols = { DH.Pink, DH.Cyan, DH.Amber, DH.Green, Color.white, new Color(0.6f, 0.36f, 1f) };
            for (int i = 0; i < N; i++)
            {
                c._p[i] = SR.Make("Bit", c.transform, SR.Pixel, DH.SortFX + 2, Vector3.zero);
                c._p[i].color = cols[i % cols.Length];
                c._p[i].transform.localScale = new Vector3(0.4f, 0.25f, 1);
                c._p[i].enabled = false;
            }
            return c;
        }

        void Update()
        {
            var cam = Camera.main;
            if (cam == null) return;
            float halfH = DH.OrthoSize, halfW = halfH * cam.aspect, dt = Time.deltaTime;
            var cp = cam.transform.position;
            for (int i = 0; i < N; i++)
            {
                var p = _p[i];
                if (!p.enabled)
                {
                    if (!Active || Random.value > 0.08f) continue;
                    p.enabled = true;
                    p.transform.position = new Vector3(cp.x + Random.Range(-halfW, halfW), cp.y + halfH + Random.Range(0f, 1f), 0);
                    _v[i] = new Vector3(Random.Range(-0.6f, 0.6f), Random.Range(-1.4f, -2.4f), 0);
                    _phase[i] = Random.value * 6f;
                }
                _phase[i] += dt * 6f;
                p.transform.position += (_v[i] + new Vector3(Mathf.Sin(_phase[i]) * 0.8f, 0, 0)) * dt;
                p.transform.localScale = new Vector3(0.4f * Mathf.Abs(Mathf.Cos(_phase[i])) + 0.05f, 0.25f, 1);
                if (p.transform.position.y < cp.y - halfH - 0.5f) p.enabled = false;
            }
        }
    }
}
