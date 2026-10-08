using UnityEngine;

namespace DataHeist
{
    /// <summary>
    /// Night-city backdrop: sky glued to the camera, three skyline layers scrolling at
    /// different rates, plus screen-space rain. Optional: the team's Gemini skyline as the far layer.
    /// </summary>
    public class Parallax : MonoBehaviour
    {
        struct Layer { public SpriteRenderer sr; public float factor, width, baseY; }

        Transform _cam;
        Layer[] _layers;
        SpriteRenderer _sky;
        Transform _rain;
        SpriteRenderer[] _drops;
        Vector2[] _dropSeed;
        float _anchorY;

        public static Parallax Create(Transform cam, float groundY)
        {
            var go = new GameObject("Parallax");
            var p = go.AddComponent<Parallax>();
            p._cam = cam;
            p._anchorY = groundY;
            p.Build();
            return p;
        }

        void Build()
        {
            _sky = SR.Make("Sky", transform, SpriteLibrary.Get("Parallax/sky"), DH.SortSky, Vector3.zero);
            _layers = new[]
            {
                MakeLayer("city_far", 0.08f, DH.SortFar, -0.2f),
                MakeLayer("city_mid", 0.18f, DH.SortMid, -1.6f),
                MakeLayer("city_near", 0.32f, DH.SortNear, -3.2f),
            };
            // rain
            _rain = new GameObject("Rain").transform;
            _rain.SetParent(transform, false);
            var rainSprite = SpriteLibrary.Get("FX/rain");
            _drops = new SpriteRenderer[70];
            _dropSeed = new Vector2[70];
            for (int i = 0; i < _drops.Length; i++)
            {
                _drops[i] = SR.Make("Drop", _rain, rainSprite, DH.SortNear + 5, Vector3.zero);
                _drops[i].color = new Color(1, 1, 1, 0.6f);
                _dropSeed[i] = new Vector2(i * 97.3f, i * 53.7f);
            }
        }

        Layer MakeLayer(string name, float factor, int order, float baseY)
        {
            var sprite = SpriteLibrary.Get("Parallax/" + name);
            float w = sprite != null ? sprite.bounds.size.x : 20f;
            var sr = SR.Tiled(name, transform, sprite, order, Vector3.zero, new Vector2(w * 3f, sprite != null ? sprite.bounds.size.y : 6f));
            return new Layer { sr = sr, factor = factor, width = w, baseY = baseY };
        }

        void LateUpdate()
        {
            if (_cam == null) return;
            Vector3 c = _cam.position;
            float halfH = DH.OrthoSize, halfW = halfH * Camera.main.aspect;

            if (_sky != null && _sky.sprite != null)
            {
                var b = _sky.sprite.bounds.size;
                _sky.transform.position = new Vector3(c.x - halfW, c.y - halfH, 0);
                _sky.transform.localScale = new Vector3(halfW * 2 / b.x, halfH * 2 / b.y, 1);
            }
            foreach (var l in _layers)
            {
                if (l.sr.sprite == null) continue;
                float scroll = c.x * l.factor;
                float start = c.x - halfW - Mathf.Repeat(scroll, l.width) - 0.0f;
                // vertical: anchored to the city floor, drifting slightly with the camera
                float y = c.y - halfH - 0.625f - (c.y - _anchorY - 2.4f) * l.factor * 0.6f;
                l.sr.transform.position = new Vector3(SR.Snap(start), SR.Snap(y), 0);
            }
            // rain in screen space
            float t = Time.unscaledTime;
            float w = halfW * 2, h = halfH * 2 + 0.6f;
            for (int i = 0; i < _drops.Length; i++)
            {
                float rx = Mathf.Repeat(_dropSeed[i].x / DH.PPU + t * 60f / DH.PPU - c.x * 10f / DH.PPU, w);
                float ry = Mathf.Repeat(_dropSeed[i].y / DH.PPU + t * 420f / DH.PPU, h);
                _drops[i].transform.position = new Vector3(c.x - halfW + rx, c.y + halfH - ry, 0);
            }
        }
    }
}
