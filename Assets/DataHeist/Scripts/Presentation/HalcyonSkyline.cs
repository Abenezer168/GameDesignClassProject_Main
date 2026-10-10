using UnityEngine;

namespace DataHeist
{
    /// <summary>
    /// The Halcyon Corp tower far behind the skyline, with a blinking beacon and two sweeping
    /// searchlights. It holds a fixed spot on screen (like the moon), so it reads as very distant.
    /// Used by the title screen and the menus to tie them to the story's villain.
    /// </summary>
    public class HalcyonSkyline : MonoBehaviour
    {
        Transform _cam;
        float _screenX;
        SpriteRenderer _beacon;
        SpriteRenderer[] _beams;

        public static HalcyonSkyline Create(Transform cam, float screenX)
        {
            var h = new GameObject("HalcyonTower").AddComponent<HalcyonSkyline>();
            h._cam = cam;
            h._screenX = screenX;
            h.Build();
            return h;
        }

        void Build()
        {
            var body = SR.Make("Tower", transform, SpriteLibrary.Get("Story/tower"), DH.SortFar + 2, new Vector3(0, -5f, 0));
            body.color = new Color(0.62f, 0.55f, 0.82f);   // distance haze
            _beacon = SR.Make("Beacon", transform, SpriteLibrary.Get("FX/glow"), DH.SortFar + 3, new Vector3(0, 8.85f, 0));
            _beacon.color = DH.Red;
            _beacon.transform.localScale = new Vector3(0.6f, 0.6f, 1);
            _beams = new SpriteRenderer[2];
            for (int i = 0; i < 2; i++)
            {
                _beams[i] = SR.Make("Searchlight", transform, SpriteLibrary.Get("Story/searchlight"), DH.SortFar + 1, new Vector3(i == 0 ? -5f : 4f, 0.5f, 0));
                _beams[i].color = i == 0 ? new Color(1, 1, 1, 0.12f) : new Color(DH.Cyan.r, DH.Cyan.g, DH.Cyan.b, 0.14f);
            }
        }

        void LateUpdate()
        {
            if (_cam == null) return;
            float halfW = DH.OrthoSize * Camera.main.aspect;
            var c = _cam.position;
            // anchored to the bottom of the view so it stays put while the camera follows jumps
            transform.position = new Vector3(SR.Snap(c.x - halfW + halfW * 2 * _screenX), SR.Snap(c.y - (DH.OrthoSize - 1.7f)), 0);
            float t = Time.unscaledTime;
            _beacon.enabled = Mathf.Repeat(t, 1.4f) < 0.7f;
            for (int i = 0; i < _beams.Length; i++)
                _beams[i].transform.localRotation = Quaternion.Euler(0, 0, 180f + Mathf.Sin(t * (0.3f + i * 0.17f) + i * 2f) * 26f);
        }
    }
}
