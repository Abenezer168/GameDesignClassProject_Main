using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace DataHeist
{
    /// <summary>
    /// Tiny runtime UI factory so every Data Heist screen is built from code
    /// (no prefab wiring needed). Reference resolution is the 640x360 game canvas.
    /// </summary>
    public static class UIKit
    {
        static TMP_FontAsset _font;

        public static TMP_FontAsset Font
        {
            get
            {
                if (_font != null) return _font;
                var ttf = Resources.Load<Font>(DH.FontPath);
                if (ttf != null)
                {
                    try { _font = TMP_FontAsset.CreateFontAsset(ttf); }
                    catch (Exception e) { Debug.LogWarning("[DataHeist] Could not build pixel font: " + e.Message); }
                }
                if (_font == null) _font = TMP_Settings.defaultFontAsset;
                return _font;
            }
        }

        public static void EnsureEventSystem()
        {
            if (EventSystem.current != null) return;
            var existing = UnityEngine.Object.FindAnyObjectByType<EventSystem>();
            if (existing != null) return;
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            var module = go.AddComponent<InputSystemUIInputModule>();
            module.AssignDefaultActions();
        }

        public static Canvas Canvas(string name, int sortOrder = 10)
        {
            EnsureEventSystem();
            var go = new GameObject(name);
            var c = go.AddComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            c.sortingOrder = sortOrder;
            c.pixelPerfect = true;
            var s = go.AddComponent<CanvasScaler>();
            s.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            s.referenceResolution = new Vector2(640, 360);
            s.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            return c;
        }

        public static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        /// <summary>Anchors a rect: anchor (0..1), pivot, position (px from anchor) and size.</summary>
        public static RectTransform Place(this RectTransform r, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            r.anchorMin = r.anchorMax = anchor;
            r.pivot = pivot;
            r.anchoredPosition = pos;
            r.sizeDelta = size;
            return r;
        }

        public static RectTransform Stretch(this RectTransform r, float inset = 0)
        {
            r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
            r.offsetMin = new Vector2(inset, inset); r.offsetMax = new Vector2(-inset, -inset);
            return r;
        }

        public static Image Image(string name, Transform parent, Color color, Sprite sprite = null)
        {
            var r = Rect(name, parent);
            var img = r.gameObject.AddComponent<Image>();
            img.color = color;
            img.sprite = sprite;
            if (sprite != null)
            {
                // keep 1 sprite pixel = 1 canvas pixel at the 640x360 reference resolution
                img.pixelsPerUnitMultiplier = 100f / sprite.pixelsPerUnit;
                if (sprite.border != Vector4.zero) img.type = UnityEngine.UI.Image.Type.Sliced;
            }
            img.raycastTarget = false;
            return img;
        }

        public static Image Panel(string name, Transform parent, Color accent)
        {
            var sprite = SpriteLibrary.Get(accent == DH.Pink ? "UI/panel_pink" : "UI/panel_cyan");
            var img = Image(name, parent, Color.white, sprite);
            if (sprite == null) img.color = new Color(DH.Panel.r, DH.Panel.g, DH.Panel.b, 0.92f);
            img.raycastTarget = true;
            return img;
        }

        public static TextMeshProUGUI Text(string name, Transform parent, string text, float size, Color color,
            TextAlignmentOptions align = TextAlignmentOptions.Left)
        {
            var r = Rect(name, parent);
            var t = r.gameObject.AddComponent<TextMeshProUGUI>();
            t.font = Font;
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.raycastTarget = false;
            t.textWrappingMode = TextWrappingModes.Normal;
            t.lineSpacing = 12;
            return t;
        }

        public static Button Button(string name, Transform parent, string label, Action onClick, bool primary = true)
        {
            var r = Rect(name, parent);
            var img = r.gameObject.AddComponent<Image>();
            img.color = primary ? DH.Cyan : new Color(1, 1, 1, 0.06f);
            var b = r.gameObject.AddComponent<Button>();
            var cb = b.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = primary ? new Color(1, 1, 1, 1) : new Color(1.4f, 1.4f, 2f, 1);
            cb.selectedColor = cb.highlightedColor;
            cb.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1);
            cb.disabledColor = new Color(0.35f, 0.3f, 0.45f, 0.8f);
            cb.colorMultiplier = 1.6f;
            b.colors = cb;
            b.targetGraphic = img;
            var t = Text("Label", r, label, 8, primary ? DH.Night : DH.Ink, TextAlignmentOptions.Center);
            t.rectTransform.Stretch();
            b.onClick.AddListener(() =>
            {
                AudioManager.I.Sfx("ui_select", 0.8f);
                onClick?.Invoke();
            });
            var hover = r.gameObject.AddComponent<UIHoverSound>();
            hover.enabled = true;
            if (!primary)
            {
                var o = r.gameObject.AddComponent<Outline>();
                o.effectColor = DH.PurpleLine;
                o.effectDistance = new Vector2(1, -1);
            }
            return b;
        }

        public static void Label(this Button b, string text)
        {
            var t = b.GetComponentInChildren<TextMeshProUGUI>();
            if (t != null) t.text = text;
        }

        public static VerticalLayoutGroup VStack(Transform t, float spacing, TextAnchor align = TextAnchor.UpperCenter)
        {
            var v = t.gameObject.AddComponent<VerticalLayoutGroup>();
            v.spacing = spacing;
            v.childAlignment = align;
            v.childControlWidth = true; v.childControlHeight = true;
            v.childForceExpandWidth = true; v.childForceExpandHeight = false;
            return v;
        }

        public static LayoutElement Size(Component c, float minH, float prefW = -1)
        {
            if (!c.gameObject.TryGetComponent<LayoutElement>(out var le)) le = c.gameObject.AddComponent<LayoutElement>();
            le.minHeight = minH; le.preferredHeight = minH;
            if (prefW > 0) le.preferredWidth = prefW;
            return le;
        }

        public static void Select(Selectable s)
        {
            if (s == null || EventSystem.current == null) return;
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(s.gameObject);
        }
    }
}
