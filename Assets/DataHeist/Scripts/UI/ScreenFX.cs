using UnityEngine;
using UnityEngine.UI;

namespace DataHeist
{
    /// <summary>
    /// Cinematic finish for menu screens: a soft vignette plus faint CRT scanlines, drawn on a
    /// canvas above the scene. Textures are generated once at runtime (no assets to import).
    /// </summary>
    public static class ScreenFX
    {
        static Sprite _vignette, _scanlines;

        public static void Add(int sortOrder = 50, float vignette = 0.75f, float scanlines = 0.10f)
        {
            var canvas = UIKit.Canvas("ScreenFX", sortOrder);
            canvas.GetComponent<GraphicRaycaster>().enabled = false;
            var root = (RectTransform)canvas.transform;
            var v = UIKit.Image("Vignette", root, new Color(0, 0, 0, vignette), Vignette);
            v.rectTransform.Stretch();
            var s = UIKit.Image("Scanlines", root, new Color(0, 0, 0, scanlines), Scanlines);
            s.type = Image.Type.Tiled;
            s.rectTransform.Stretch();
        }

        static Sprite Vignette
        {
            get
            {
                if (_vignette != null) return _vignette;
                const int w = 128, h = 72;
                var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
                for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float dx = (x + 0.5f) / w * 2 - 1, dy = (y + 0.5f) / h * 2 - 1;
                    float d = Mathf.Sqrt(dx * dx * 0.8f + dy * dy * 1.1f);
                    tex.SetPixel(x, y, new Color(1, 1, 1, Mathf.SmoothStep(0, 1, Mathf.InverseLerp(0.55f, 1.35f, d))));
                }
                tex.Apply();
                return _vignette = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100);
            }
        }

        static Sprite Scanlines
        {
            get
            {
                if (_scanlines != null) return _scanlines;
                var tex = new Texture2D(1, 2, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Point };
                tex.SetPixel(0, 0, new Color(1, 1, 1, 1));
                tex.SetPixel(0, 1, new Color(1, 1, 1, 0));
                tex.Apply();
                return _scanlines = Sprite.Create(tex, new Rect(0, 0, 1, 2), new Vector2(0.5f, 0.5f), 1);
            }
        }
    }
}
