using UnityEngine;
using UnityEngine.UI;

namespace DataHeist
{
    /// <summary>Frame-flip animator for SpriteRenderers or UI Images (no Animator Controller needed).</summary>
    public class SpriteAnimator : MonoBehaviour
    {
        public Sprite[] frames;
        public float fps = 10f;
        public float timeOffset;
        public bool unscaled = true;
        SpriteRenderer _sr;
        Image _img;

        void Awake()
        {
            _sr = GetComponent<SpriteRenderer>();
            _img = GetComponent<Image>();
        }

        public void Play(Sprite[] f, float framesPerSecond)
        {
            frames = f; fps = framesPerSecond;
            Update();
        }

        void Update()
        {
            if (frames == null || frames.Length == 0) return;
            float t = (unscaled ? Time.unscaledTime : Time.time) + timeOffset;
            var s = frames[Mathf.FloorToInt(t * fps) % frames.Length];
            if (_sr != null) _sr.sprite = s;
            if (_img != null) _img.sprite = s;
        }
    }

    public static class SR
    {
        static Sprite _pixel;
        /// <summary>4x4 white pixel sprite used for rectangles, particles and shadows.</summary>
        public static Sprite Pixel => _pixel != null ? _pixel : (_pixel = SpriteLibrary.Get("FX/pixel"));

        public static SpriteRenderer Make(string name, Transform parent, Sprite sprite, int order, Vector3 pos)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = order;
            return sr;
        }

        public static SpriteRenderer Tiled(string name, Transform parent, Sprite sprite, int order, Vector3 pos, Vector2 size)
        {
            var sr = Make(name, parent, sprite, order, pos);
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.tileMode = SpriteTileMode.Continuous;
            sr.size = size;
            return sr;
        }

        /// <summary>Solid rectangle from world-space min corner and size.</summary>
        public static SpriteRenderer Box(string name, Transform parent, Color color, int order, float x, float y, float w, float h)
        {
            var sr = Make(name, parent, Pixel, order, new Vector3(x + w / 2f, y + h / 2f, 0));
            sr.color = color;
            float unit = Pixel != null ? Pixel.bounds.size.x : 0.125f;
            sr.transform.localScale = new Vector3(w / unit, h / unit, 1);
            return sr;
        }

        public static float Snap(float v) => Mathf.Round(v * DH.PPU) / DH.PPU;
    }
}
