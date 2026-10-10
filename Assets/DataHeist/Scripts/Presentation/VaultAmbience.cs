using UnityEngine;

namespace DataHeist
{
    /// <summary>
    /// Underground atmosphere for the vault theme: dust motes drifting through the light, and short
    /// spark showers dropping from the ceiling. Pooled sprites, no allocations after Init.
    /// </summary>
    public class VaultAmbience : MonoBehaviour
    {
        const int Dust = 36, Sparks = 24;
        Transform _cam;
        SpriteRenderer[] _dust, _sparks;
        Vector2[] _dustSeed;
        Vector3[] _sparkVel;
        float[] _sparkLife;
        float _nextShower = 2f;

        public void Init(Transform cam)
        {
            _cam = cam;
            var px = SR.Pixel;
            _dust = new SpriteRenderer[Dust];
            _dustSeed = new Vector2[Dust];
            for (int i = 0; i < Dust; i++)
            {
                _dust[i] = SR.Make("Dust", transform, px, DH.SortNear + 4, Vector3.zero);
                _dust[i].color = new Color(0.75f, 0.85f, 1f, Random.Range(0.08f, 0.25f));
                _dust[i].transform.localScale = Vector3.one * (Random.value < 0.2f ? 0.5f : 0.25f);
                _dustSeed[i] = new Vector2(Random.value * 100f, Random.value * 100f);
            }
            _sparks = new SpriteRenderer[Sparks];
            _sparkVel = new Vector3[Sparks];
            _sparkLife = new float[Sparks];
            for (int i = 0; i < Sparks; i++)
            {
                _sparks[i] = SR.Make("Spark", transform, px, DH.SortFX - 1, Vector3.zero);
                _sparks[i].color = DH.Amber;
                _sparks[i].transform.localScale = Vector3.one * 0.25f;
                _sparks[i].enabled = false;
            }
        }

        void LateUpdate()
        {
            if (_cam == null) return;
            float t = Time.time, dt = Time.deltaTime;
            Vector3 c = _cam.position;
            float halfH = DH.OrthoSize, halfW = halfH * Camera.main.aspect;
            for (int i = 0; i < Dust; i++)
            {
                var s = _dustSeed[i];
                float x = Mathf.Repeat(s.x + t * 0.25f - c.x * 0.6f, halfW * 2f);
                float y = Mathf.Repeat(s.y + Mathf.Sin(t * 0.6f + s.x) * 0.4f + t * 0.12f, halfH * 2f);
                _dust[i].transform.position = new Vector3(c.x - halfW + x, c.y - halfH + y, 0);
            }
            _nextShower -= dt;
            if (_nextShower <= 0)
            {
                _nextShower = Random.Range(1.5f, 4f);
                float sx = c.x + Random.Range(-halfW * 0.6f, halfW);
                int n = 0;
                for (int i = 0; i < Sparks && n < 8; i++)
                {
                    if (_sparks[i].enabled) continue;
                    _sparks[i].enabled = true;
                    _sparks[i].transform.position = new Vector3(sx, c.y + halfH - 1.6f, 0);
                    _sparkVel[i] = new Vector3(Random.Range(-2f, 2f), Random.Range(-1f, 2f), 0);
                    _sparkLife[i] = Random.Range(0.4f, 0.9f);
                    n++;
                }
            }
            for (int i = 0; i < Sparks; i++)
            {
                if (!_sparks[i].enabled) continue;
                _sparkLife[i] -= dt;
                if (_sparkLife[i] <= 0) { _sparks[i].enabled = false; continue; }
                _sparkVel[i].y -= 18f * dt;
                _sparks[i].transform.position += _sparkVel[i] * dt;
            }
        }
    }
}
