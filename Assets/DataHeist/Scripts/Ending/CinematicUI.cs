using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace DataHeist
{
    /// <summary>
    /// Screen layer for the ending's cinematics: letterbox bars, typewriter captions, speaker
    /// dialogue with a portrait, big title cards, a SKIP button and speech bubbles pinned to
    /// world positions. Runs on scaled time, so a test can fast-forward with Time.timeScale.
    /// </summary>
    public class CinematicUI : MonoBehaviour
    {
        public const float CharsPerSecond = 40f;

        public event Action OnSkip;
        public bool Interactive { get; set; }        // hide the letterbox for the playable walk

        RectTransform _root, _top, _bottom, _bubbles;
        TextMeshProUGUI _caption, _speaker, _title, _subtitle, _hint, _chapterTag;
        Image _portrait, _portraitFrame, _dim, _black;
        static Sprite _rays;
        CanvasGroup _titleGroup;
        float _bars;
        readonly List<(RectTransform rt, TextMeshProUGUI text, Func<Vector3> world, float until)> _bubbleList =
            new List<(RectTransform, TextMeshProUGUI, Func<Vector3>, float)>();

        public static CinematicUI Create()
        {
            var canvas = UIKit.Canvas("Cinematic", 20);
            var ui = canvas.gameObject.AddComponent<CinematicUI>();
            ui.Build((RectTransform)canvas.transform);
            return ui;
        }

        void Build(RectTransform root)
        {
            _root = root;
            _bubbles = UIKit.Rect("Bubbles", root).Stretch();
            _dim = UIKit.Image("Dim", root, new Color(0, 0, 0, 0));
            _dim.rectTransform.Stretch();

            var bar = new Color(0.02f, 0.01f, 0.05f, 1f);
            _top = UIKit.Image("TopBar", root, bar).rectTransform;
            _top.anchorMin = new Vector2(0, 1); _top.anchorMax = new Vector2(1, 1); _top.pivot = new Vector2(0.5f, 1);
            _bottom = UIKit.Image("BottomBar", root, bar).rectTransform;
            _bottom.anchorMin = new Vector2(0, 0); _bottom.anchorMax = new Vector2(1, 0); _bottom.pivot = new Vector2(0.5f, 0);
            _top.sizeDelta = _bottom.sizeDelta = Vector2.zero;

            _chapterTag = UIKit.Text("Chapter", _top, "", 5, DH.Dim);
            _chapterTag.rectTransform.Place(new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(24, 0), new Vector2(300, 8));
            var skip = UIKit.Button("Skip", _top, "SKIP SCENE  [ESC]", () => OnSkip?.Invoke(), false);
            skip.GetComponent<RectTransform>().Place(new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-16, 0), new Vector2(110, 16));
            skip.GetComponentInChildren<TextMeshProUGUI>().fontSize = 6;
            var nav = skip.navigation; nav.mode = Navigation.Mode.None; skip.navigation = nav;

            _portraitFrame = UIKit.Image("PortraitFrame", _bottom, DH.Panel);
            _portraitFrame.rectTransform.Place(new Vector2(0, 0), new Vector2(0, 0), new Vector2(24, 8), new Vector2(68, 68));
            var o = _portraitFrame.gameObject.AddComponent<Outline>();
            o.effectColor = DH.Amber; o.effectDistance = new Vector2(1, -1);
            _portrait = UIKit.Image("Portrait", _portraitFrame.rectTransform, Color.white);
            _portrait.rectTransform.Stretch(2);
            _portrait.preserveAspect = true;
            _portraitFrame.gameObject.SetActive(false);
            _speaker = UIKit.Text("Speaker", _bottom, "", 6, DH.Amber);
            _speaker.characterSpacing = 20;
            _caption = UIKit.Text("Caption", _bottom, "", 8, DH.Ink);
            _caption.lineSpacing = 45;
            _hint = UIKit.Text("Hint", _bottom, "", 5, new Color(1, 1, 1, 0.45f), TextAlignmentOptions.Right);
            _hint.rectTransform.Place(new Vector2(1, 0), new Vector2(1, 0), new Vector2(-24, 8), new Vector2(300, 8));
            LayoutCaption(false);

            _black = UIKit.Image("Black", root, new Color(0, 0, 0, 0));
            _black.rectTransform.Stretch(-4);

            var tg = UIKit.Rect("Title", root).Stretch();
            _titleGroup = tg.gameObject.AddComponent<CanvasGroup>();
            _titleGroup.alpha = 0;
            _titleGroup.blocksRaycasts = false;
            _title = UIKit.Text("Big", tg, "", 18, DH.Ink, TextAlignmentOptions.Center);
            _title.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 14), new Vector2(600, 60));
            _subtitle = UIKit.Text("Sub", tg, "", 7, DH.Ink, TextAlignmentOptions.Center);
            _subtitle.lineSpacing = 40;
            _subtitle.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -26), new Vector2(520, 40));
        }

        void LayoutCaption(bool speaker)
        {
            float left = speaker ? 108 : 40;
            _speaker.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(left, -12), new Vector2(400, 10));
            _caption.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(left, speaker ? -26 : -18), new Vector2(speaker ? 480 : 540, 46));
        }

        public void SetChapter(string tag) => _chapterTag.text = tag;

        void Update()
        {
            float target = Interactive ? 0f : 1f;
            _bars = Mathf.MoveTowards(_bars, target, Time.unscaledDeltaTime * 1.5f);
            float e = Mathf.SmoothStep(0, 1, _bars);
            _top.sizeDelta = new Vector2(0, Mathf.Max(22, 28 * e));
            _bottom.sizeDelta = new Vector2(0, 76 * e);
            // bubbles follow their speakers and expire
            var cam = Camera.main;
            var size = _bubbles.rect.size;
            for (int i = _bubbleList.Count - 1; i >= 0; i--)
            {
                var b = _bubbleList[i];
                if (Time.time > b.until) { Destroy(b.rt.gameObject); _bubbleList.RemoveAt(i); continue; }
                if (cam == null) continue;
                var vp = cam.WorldToViewportPoint(b.world());
                b.rt.anchoredPosition = new Vector2(Mathf.Round((vp.x - 0.5f) * size.x), Mathf.Round((vp.y - 0.5f) * size.y));
            }
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) OnSkip?.Invoke();
        }

        public static bool AdvancePressed()
        {
            var kb = Keyboard.current; var pad = Gamepad.current; var mouse = Mouse.current;
            return (kb != null && (kb.enterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame))
                   || (pad != null && pad.buttonSouth.wasPressedThisFrame)
                   || (mouse != null && mouse.leftButton.wasPressedThisFrame);
        }

        /// <summary>Typewriter caption (or a speaker's line when speaker/portrait are given). Waits until read.</summary>
        public IEnumerator Say(string text, string speaker = null, Sprite portrait = null, float hold = 2.2f)
        {
            bool sp = speaker != null;
            LayoutCaption(sp);
            _portraitFrame.gameObject.SetActive(portrait != null);
            if (portrait != null) _portrait.sprite = portrait;
            _speaker.text = speaker ?? "";
            _caption.text = text;
            _caption.ForceMeshUpdate();
            int total = _caption.textInfo.characterCount, visible = 0;
            _caption.maxVisibleCharacters = 0;
            _hint.text = "";
            float t = 0;
            yield return null;
            while (visible < total)
            {
                if (AdvancePressed()) { visible = total; break; }
                t += Time.deltaTime * CharsPerSecond;
                while (t >= 1 && visible < total)
                {
                    t -= 1;
                    char ch = _caption.textInfo.characterInfo[visible].character;
                    visible++;
                    if (ch == '.' || ch == '!' || ch == '?') t -= 7;
                    if (ch != ' ' && visible % 3 == 0) AudioManager.I.Sfx(sp ? "npc_talk" : "ui_move", sp ? 0.25f : 0.08f, UnityEngine.Random.Range(sp ? 0.85f : 1.5f, sp ? 1.0f : 1.7f));
                }
                _caption.maxVisibleCharacters = visible;
                yield return null;
            }
            _caption.maxVisibleCharacters = total;
            _hint.text = "ENTER  >";
            yield return null;
            for (float w = 0; w < hold + total * 0.02f; w += Time.deltaTime)
            {
                if (AdvancePressed()) break;
                yield return null;
            }
            _hint.text = "";
        }

        public void ClearCaption()
        {
            _caption.text = ""; _speaker.text = ""; _hint.text = "";
            _portraitFrame.gameObject.SetActive(false);
        }

        /// <summary>A big centred title card that fades in, holds and fades out.</summary>
        public IEnumerator Title(string title, string sub, Color color, float hold = 2.5f, float size = 18, bool dim = true, float y = 14f)
        {
            _title.text = title; _title.color = color; _title.fontSize = size;
            _title.rectTransform.anchoredPosition = new Vector2(0, y);
            _subtitle.rectTransform.anchoredPosition = new Vector2(0, y - 40f);
            _subtitle.text = sub ?? "";
            for (float t = 0; t < 1f; t += Time.deltaTime)
            {
                _titleGroup.alpha = t;
                if (dim) _dim.color = new Color(0, 0, 0, 0.45f * t);
                yield return null;
            }
            _titleGroup.alpha = 1;
            for (float w = 0; w < hold; w += Time.deltaTime) { if (AdvancePressed() && w > 0.4f) break; yield return null; }
            for (float t = 1; t > 0; t -= Time.deltaTime * 1.4f)
            {
                _titleGroup.alpha = t;
                if (dim) _dim.color = new Color(0, 0, 0, 0.45f * t);
                yield return null;
            }
            _titleGroup.alpha = 0; _dim.color = new Color(0, 0, 0, 0);
        }

        /// <summary>Fade to / from black (cuts between chapters).</summary>
        public IEnumerator Fade(float to, float seconds)
        {
            float from = _black.color.a;
            for (float t = 0; t < seconds; t += Time.unscaledDeltaTime)
            {
                _black.color = new Color(0, 0, 0, Mathf.Lerp(from, to, Mathf.SmoothStep(0, 1, t / seconds)));
                yield return null;
            }
            _black.color = new Color(0, 0, 0, to);
        }

        public void SetBlack(float a) => _black.color = new Color(0, 0, 0, a);

        /// <summary>
        /// The award moment: the screen dims, golden rays turn behind a large medal that pops in,
        /// then the award's name and the recipient appear.
        /// </summary>
        public IEnumerator AwardCard(Sprite[] medal, string award, string recipient, float hold = 3.5f)
        {
            var card = UIKit.Rect("Award", _root).Stretch();
            card.SetSiblingIndex(_black.transform.GetSiblingIndex());
            var group = card.gameObject.AddComponent<CanvasGroup>();
            var dim = UIKit.Image("Dim", card, new Color(0.02f, 0.01f, 0.05f, 0.8f));
            dim.rectTransform.Stretch(-4);
            var rays = UIKit.Image("Rays", card, new Color(1f, 0.82f, 0.35f, 0.35f), Rays);
            rays.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 30), new Vector2(360, 360));
            var glow = UIKit.Image("Glow", card, new Color(1f, 0.8f, 0.3f, 0.5f), SpriteLibrary.Get("FX/glow"));
            glow.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 30), new Vector2(150, 150));
            var m = UIKit.Image("Medal", card, Color.white, medal.Length > 0 ? medal[0] : null);
            m.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 30), new Vector2(96, 144));
            var anim = m.gameObject.AddComponent<SpriteAnimator>();
            anim.Play(medal, 5);
            var title = UIKit.Text("Award", card, award, 16, new Color(1f, 0.86f, 0.4f), TextAlignmentOptions.Center);
            title.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -70), new Vector2(600, 24));
            title.textWrappingMode = TextWrappingModes.NoWrap;
            var who = UIKit.Text("To", card, recipient, 7, DH.Ink, TextAlignmentOptions.Center);
            who.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -94), new Vector2(500, 12));
            AudioManager.I.Sfx("medal", 1f);
            float t = 0;
            while (t < hold + 1.4f)
            {
                t += Time.deltaTime;
                float pop = Mathf.Clamp01(t / 0.45f);
                float s = pop < 1 ? Mathf.Sin(pop * Mathf.PI * 0.5f) * 1.12f : 1f + Mathf.Sin(t * 2f) * 0.02f;
                m.rectTransform.localScale = Vector3.one * s;
                rays.rectTransform.localRotation = Quaternion.Euler(0, 0, -t * 18f);
                glow.rectTransform.localScale = Vector3.one * (1f + Mathf.Sin(t * 3f) * 0.08f);
                float a = Mathf.Clamp01(t / 0.4f);
                if (t > hold + 0.6f) a = Mathf.Clamp01((hold + 1.4f - t) / 0.8f);
                group.alpha = a;
                title.alpha = Mathf.Clamp01((t - 0.5f) / 0.5f);
                who.alpha = Mathf.Clamp01((t - 1.0f) / 0.5f);
                if (AdvancePressed() && t > 1.5f && t < hold + 0.6f) t = hold + 0.6f;
                yield return null;
            }
            Destroy(card.gameObject);
        }

        static Sprite Rays
        {
            get
            {
                if (_rays != null) return _rays;
                const int n = 128;
                var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
                for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float dx = (x + 0.5f) / n * 2 - 1, dy = (y + 0.5f) / n * 2 - 1;
                    float r = Mathf.Sqrt(dx * dx + dy * dy), a = Mathf.Atan2(dy, dx);
                    float beam = Mathf.Pow(Mathf.Max(0, Mathf.Cos(a * 8f)), 6f);
                    tex.SetPixel(x, y, new Color(1, 1, 1, beam * Mathf.Clamp01(1f - r) * Mathf.Clamp01(r * 3f)));
                }
                tex.Apply();
                return _rays = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100);
            }
        }

        /// <summary>A speech bubble over a world position, gone after `seconds`.</summary>
        public void Bubble(string text, Func<Vector3> world, float seconds = 2.6f, Color? accent = null)
        {
            var bg = UIKit.Image("Bubble", _bubbles, new Color(0.04f, 0.02f, 0.09f, 0.88f));
            var rt = bg.rectTransform;
            bool twoLines = text.Length > 28;
            rt.sizeDelta = twoLines ? new Vector2(156, 26) : new Vector2(14 + text.Length * 5.2f, 16);
            rt.pivot = new Vector2(0.5f, 0);
            var o = bg.gameObject.AddComponent<Outline>();
            o.effectColor = accent ?? DH.Cyan; o.effectDistance = new Vector2(1, -1);
            var t = UIKit.Text("Text", rt, text, 5, DH.Ink, TextAlignmentOptions.Center);
            t.rectTransform.Stretch(3);
            _bubbleList.Add((rt, t, world, Time.time + seconds));
        }

        public void SetHint(string text) => _hint.text = text;
    }
}
