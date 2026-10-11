using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace DataHeist
{
    /// <summary>
    /// Cinematic story intro (StoryScene). Six staged shots play under a letterboxed caption bar
    /// with typewriter text. NEXT (Enter / Space / click / pad A) completes the line, then advances;
    /// SKIP (Esc / pad Start) jumps to the end. Ends on the game's title card, marks the story as
    /// seen and continues to the home page.
    /// Put this on one GameObject in an empty scene; everything else is built at runtime.
    /// </summary>
    public class StoryController : MonoBehaviour
    {
        // the intro always lands on the home page (not serialized, so scene files can't go stale)
        const string NextScene = DH.SceneMainMenu;
        // tuning lives in code, not the scene file, so saved scenes can't override it
        const float CharsPerSecond = 42f;
        const float AutoAdvanceDelay = 0f;   // seconds before a finished caption moves on by itself (0 = wait for the player)

        /// <summary>A UI caption pinned to a world position (keeps the pixel font crisp).</summary>
        public class WorldLabel
        {
            public TextMeshProUGUI text;
            public Func<Vector3> world;
            public bool Visible { get => text.gameObject.activeSelf; set => text.gameObject.SetActive(value); }
        }

        readonly List<StoryShot> _shots = new List<StoryShot>();
        readonly List<WorldLabel> _labels = new List<WorldLabel>();
        readonly List<Image> _pips = new List<Image>();
        Transform _cam;
        RectTransform _root, _labelLayer, _top, _bottom;
        TextMeshProUGUI _chapter, _body, _nextHint;
        RectTransform _rule;
        Image _portrait, _portraitFrame;
        Image _cut, _alarm;
        RectTransform[] _glitchBars;
        int _index = -1, _visible, _total;
        float _shotT, _typeT, _doneT, _intro;
        bool _busy, _finished;

        public int ShotIndex => _index;
        public int ShotCount => _shots.Count;
        public bool Typing => _visible < _total;
        public bool Alarm { get; set; }

        void Start()
        {
            SetupCamera();
            BuildUI();
            AddShot<ShotCity>("SECTOR 7  //  2089",
                "Sector 7, 2089. The rain never stops and the city never sleeps. It just stopped asking questions.");
            AddShot<ShotTower>("HALCYON CORP",
                "Halcyon Corp owns the power grid, the police drones and every byte of data anyone ever made. Yours included.");
            AddShot<ShotVault>("THE SHARDS",
                "Deep in its vaults sit five Data Shards: the city's memory, and the proof of everything Halcyon has buried.");
            AddShot<ShotCourier>("THE COURIER",
                "Kai Okafor runs packages across the rooftops. Fastest courier in the sector. Tonight a stranger paid him to steal the truth.");
            _shots[_shots.Count - 1].portrait = "kai";
            AddShot<ShotCrew>("THE CREW",
                "He can't do it alone. Nova runs. Jax breaks. Lin vanishes. Together they can crack a vault no one has ever cracked.");
            AddShot<ShotRun>("THE HEIST",
                "Three shards, then the archive vault. Get the proof out to every screen in the city before Halcyon locks it down.");
            BuildPips();
            AudioManager.I.MusicOr("music_story", "music_menu");
            SceneFader.FadeIn(0.8f);
            Show(0);
            StartCoroutine(RevealFirst());
        }

        IEnumerator RevealFirst()
        {
            yield return new WaitForSecondsRealtime(0.8f);   // after the fade-in
            _shots[0].Revealed();
        }

        void AddShot<T>(string chapter, string text) where T : StoryShot, new()
        {
            var s = new T { chapter = chapter, text = text };
            s.Init(this, _shots.Count);
            _shots.Add(s);
        }

        void SetupCamera()
        {
            var camGo = Camera.main != null ? Camera.main.gameObject : new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            if (!camGo.TryGetComponent<Camera>(out var cam)) cam = camGo.AddComponent<Camera>();
            cam.orthographic = true; cam.orthographicSize = DH.OrthoSize;
            cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = DH.Night;
            cam.nearClipPlane = -50; cam.farClipPlane = 50;
            if (camGo.GetComponent<AudioListener>() == null) camGo.AddComponent<AudioListener>();
            _cam = camGo.transform;
            Parallax.Create(_cam, 0);
        }

        // ------------------------------------------------------------------ UI
        void BuildUI()
        {
            var canvas = UIKit.Canvas("Story", 20);
            _root = (RectTransform)canvas.transform;

            // clicking anywhere advances (the skip button sits above this)
            var catcher = UIKit.Image("ClickCatcher", _root, new Color(0, 0, 0, 0));
            catcher.rectTransform.Stretch();
            catcher.raycastTarget = true;
            catcher.gameObject.AddComponent<ClickCatcher>().onClick = Next;

            _alarm = UIKit.Image("Alarm", _root, new Color(DH.Red.r, DH.Red.g, DH.Red.b, 0));
            _alarm.rectTransform.Stretch();
            _labelLayer = UIKit.Rect("Labels", _root).Stretch();

            var bar = new Color(0.02f, 0.01f, 0.05f, 1f);
            _top = UIKit.Image("TopBar", _root, bar).rectTransform;
            _top.anchorMin = new Vector2(0, 1); _top.anchorMax = new Vector2(1, 1); _top.pivot = new Vector2(0.5f, 1);
            _top.sizeDelta = new Vector2(0, 0);
            _bottom = UIKit.Image("BottomBar", _root, bar).rectTransform;
            _bottom.anchorMin = new Vector2(0, 0); _bottom.anchorMax = new Vector2(1, 0); _bottom.pivot = new Vector2(0.5f, 0);
            _bottom.sizeDelta = new Vector2(0, 0);

            var brand = UIKit.Text("Brand", _top, "THE DATA HEIST", 5, DH.Dim);
            brand.rectTransform.Place(new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(24, 0), new Vector2(160, 8));
            var skip = UIKit.Button("Skip", _top, "SKIP  [ESC]", Skip, false);
            skip.GetComponent<RectTransform>().Place(new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-16, 0), new Vector2(84, 16));
            var nav = skip.navigation; nav.mode = Navigation.Mode.None; skip.navigation = nav;
            skip.GetComponentInChildren<TextMeshProUGUI>().fontSize = 6;

            // speaker portrait, rising out of the caption bar
            _portraitFrame = UIKit.Image("PortraitFrame", _bottom, DH.Panel);
            _portraitFrame.rectTransform.Place(new Vector2(0, 0), new Vector2(0, 0), new Vector2(24, 8), new Vector2(68, 68));
            var frameLine = _portraitFrame.gameObject.AddComponent<Outline>();
            frameLine.effectColor = DH.Cyan; frameLine.effectDistance = new Vector2(1, -1);
            _portrait = UIKit.Image("Portrait", _portraitFrame.rectTransform, Color.white);
            _portrait.rectTransform.Stretch(2);
            _portrait.preserveAspect = true;
            _portraitFrame.gameObject.SetActive(false);

            var rule = UIKit.Image("Rule", _bottom, DH.Pink);
            rule.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(40, -14), new Vector2(3, 9));
            _rule = rule.rectTransform;
            _chapter = UIKit.Text("Chapter", _bottom, "", 6, DH.Pink);
            _chapter.characterSpacing = 20;
            _chapter.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(50, -14), new Vector2(400, 10));
            _body = UIKit.Text("Body", _bottom, "", 8, DH.Ink);
            _body.lineSpacing = 45;
            _body.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(40, -30), new Vector2(520, 40));
            _nextHint = UIKit.Text("Next", _bottom, "NEXT  >", 6, DH.Cyan, TextAlignmentOptions.Right);
            _nextHint.rectTransform.Place(new Vector2(1, 0), new Vector2(1, 0), new Vector2(-24, 10), new Vector2(120, 10));

            // shot transition: black flash with glitch slices
            _cut = UIKit.Image("Cut", _root, new Color(0, 0, 0, 0));
            _cut.rectTransform.Stretch();
            _glitchBars = new RectTransform[6];
            for (int i = 0; i < _glitchBars.Length; i++)
            {
                var g = UIKit.Image("Glitch", _root, i % 2 == 0 ? DH.Pink : DH.Cyan);
                g.rectTransform.anchorMin = new Vector2(0, 0.5f); g.rectTransform.anchorMax = new Vector2(1, 0.5f);
                g.gameObject.SetActive(false);
                _glitchBars[i] = g.rectTransform;
            }
            ScreenFX.Add(60, 0.7f, 0.08f);
        }

        void BuildPips()
        {
            var row = UIKit.Rect("Progress", _top);
            row.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(_shots.Count * 12, 4));
            for (int i = 0; i < _shots.Count; i++)
            {
                var p = UIKit.Image("Pip", row, DH.PurpleLine);
                p.rectTransform.Place(new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(i * 12, 0), new Vector2(8, 2));
                _pips.Add(p);
            }
        }

        public WorldLabel AddLabel(string text, float size, Color color, Func<Vector3> world)
        {
            var t = UIKit.Text("Label", _labelLayer, text, size, color, TextAlignmentOptions.Center);
            t.rectTransform.sizeDelta = new Vector2(140, 30);
            t.lineSpacing = 10;
            var l = new WorldLabel { text = t, world = world };
            _labels.Add(l);
            return l;
        }

        // ------------------------------------------------------------------ flow
        void Show(int i)
        {
            if (_index >= 0) { _shots[_index].Exit(); _shots[_index].SetActive(false); }
            _index = i;
            var s = _shots[i];
            s.SetActive(true);
            _shotT = 0;
            s.Enter();
            _chapter.text = $"{i + 1:00}  /  {s.chapter}";
            bool speaker = !string.IsNullOrEmpty(s.portrait);
            _portraitFrame.gameObject.SetActive(speaker);
            if (speaker)
            {
                var c = CharacterDefinition.Get(s.portrait);
                _portrait.sprite = SpriteLibrary.Portrait(s.portrait);
                if (c != null) _portraitFrame.GetComponent<Outline>().effectColor = c.color;
            }
            float left = speaker ? 108 : 40;
            _rule.anchoredPosition = new Vector2(left, -14);
            _chapter.rectTransform.anchoredPosition = new Vector2(left + 10, -14);
            _body.rectTransform.anchoredPosition = new Vector2(left, -30);
            _body.rectTransform.sizeDelta = new Vector2(speaker ? 460 : 520, 40);
            _body.text = s.text;
            _body.ForceMeshUpdate();
            _total = _body.textInfo.characterCount;
            _visible = 0; _typeT = 0; _doneT = 0;
            _body.maxVisibleCharacters = 0;
            for (int k = 0; k < _pips.Count; k++)
                _pips[k].color = k < i ? DH.Dim : k == i ? DH.Cyan : DH.PurpleLine;
            ApplyCamera();
        }

        /// <summary>Completes the caption, or moves to the next shot when it is already complete.</summary>
        public void Next()
        {
            if (_busy || _finished) return;
            if (Typing) { _visible = _total; _body.maxVisibleCharacters = _total; return; }
            AudioManager.I.Sfx("ui_select", 0.5f);
            if (_index + 1 < _shots.Count) StartCoroutine(CutTo(_index + 1));
            else StartCoroutine(TitleCard());
        }

        public void Skip()
        {
            if (_finished) return;
            AudioManager.I.Sfx("ui_back");
            Finish();
        }

        void Finish()
        {
            _finished = true;
            SaveSystem.Data.storySeen = true;
            SaveSystem.Save();
            SceneFader.Load(NextScene, 0.5f, 0.5f);
        }

        IEnumerator CutTo(int i)
        {
            _busy = true;
            AudioManager.I.Sfx("glitch", 0.25f, 1.3f);
            yield return Glitch(0.16f, 0, 1);
            Show(i);
            yield return Glitch(0.28f, 1, 0);
            _shots[i].Revealed();
            _busy = false;
        }

        IEnumerator Glitch(float seconds, float from, float to)
        {
            for (float t = 0; t < seconds; t += Time.unscaledDeltaTime)
            {
                float k = Mathf.Lerp(from, to, t / seconds);
                _cut.color = new Color(0, 0, 0, k);
                foreach (var g in _glitchBars)
                {
                    bool on = UnityEngine.Random.value < 0.6f * Mathf.Sin(k * Mathf.PI);
                    g.gameObject.SetActive(on);
                    if (!on) continue;
                    g.anchoredPosition = new Vector2(UnityEngine.Random.Range(-40, 40), UnityEngine.Random.Range(-170, 170));
                    g.sizeDelta = new Vector2(0, UnityEngine.Random.Range(2, 9));
                    g.GetComponent<Image>().color = new Color(g.GetComponent<Image>().color.r, g.GetComponent<Image>().color.g, g.GetComponent<Image>().color.b, 0.55f);
                }
                yield return null;
            }
            _cut.color = new Color(0, 0, 0, to);
            foreach (var g in _glitchBars) g.gameObject.SetActive(false);
        }

        IEnumerator TitleCard()
        {
            _busy = true;
            _finished = true;
            SaveSystem.Data.storySeen = true;
            SaveSystem.Save();
            AudioManager.I.Sfx("glitch", 0.3f, 0.9f);
            yield return Glitch(0.25f, 0, 1);
            _shots[_index].Exit();
            var card = UIKit.Rect("TitleCard", _root).Stretch();
            card.SetAsLastSibling();
            var group = card.gameObject.AddComponent<CanvasGroup>();
            var sub = UIKit.Text("Shard", card, "THE", 7, DH.Amber, TextAlignmentOptions.Center);
            sub.characterSpacing = 40;
            sub.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 28), new Vector2(400, 12));
            var title = UIKit.Text("Level", card, "DATA HEIST", 26, DH.Cyan, TextAlignmentOptions.Center);
            title.textWrappingMode = TextWrappingModes.NoWrap;
            title.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(600, 30));
            var where = UIKit.Text("Where", card, "STEAL THE SHARDS.  OWN THE NIGHT.", 6, DH.Dim, TextAlignmentOptions.Center);
            where.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -28), new Vector2(500, 10));
            AudioManager.I.Sfx("go", 0.6f);
            for (float t = 0; t < 0.5f; t += Time.unscaledDeltaTime) { group.alpha = t / 0.5f; yield return null; }
            group.alpha = 1;
            yield return new WaitForSecondsRealtime(2.2f);
            SceneFader.Load(NextScene, 0.6f, 0.5f);
        }

        // ------------------------------------------------------------------ loop
        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            _intro = Mathf.Min(1, _intro + dt / 0.9f);
            float e = Mathf.SmoothStep(0, 1, _intro);
            _top.sizeDelta = new Vector2(0, 28 * e);
            _bottom.sizeDelta = new Vector2(0, 76 * e);
            _alarm.color = new Color(DH.Red.r, DH.Red.g, DH.Red.b, Alarm ? 0.05f + 0.05f * Mathf.Sin(Time.unscaledTime * 6f) : 0);
            if (_index < 0 || _finished && !_busy) return;

            _shotT += dt;
            var shot = _shots[_index];
            shot.Tick(_shotT, dt);
            ApplyCamera();
            Typewriter(dt);
            HandleInput();
        }

        void LateUpdate()
        {
            var cam = Camera.main;
            if (cam == null) return;
            // viewport maths works in any canvas mode and at any resolution
            var size = _labelLayer.rect.size;
            foreach (var l in _labels)
            {
                if (!l.Visible) continue;
                var vp = cam.WorldToViewportPoint(l.world());
                l.text.rectTransform.anchoredPosition = new Vector2(Mathf.Round((vp.x - 0.5f) * size.x), Mathf.Round((vp.y - 0.5f) * size.y));
            }
        }

        void ApplyCamera()
        {
            var c = _shots[_index].CameraAt(_shotT);
            _cam.position = new Vector3(SR.Snap(c.x), SR.Snap(c.y), -10);
        }

        void Typewriter(float dt)
        {
            if (_intro < 1 || _busy) return;
            if (Typing)
            {
                _typeT += dt * CharsPerSecond;
                while (_typeT >= 1 && Typing)
                {
                    _typeT -= 1;
                    char ch = _body.textInfo.characterInfo[_visible].character;
                    _visible++;
                    if (ch == '.' || ch == ':') _typeT -= 9;       // beat after a sentence
                    else if (ch == ',') _typeT -= 4;
                    if (ch != ' ' && _visible % 3 == 0) AudioManager.I.Sfx("ui_move", 0.08f, UnityEngine.Random.Range(1.5f, 1.7f));
                }
                _body.maxVisibleCharacters = _visible;
                _nextHint.alpha = 0;
                return;
            }
            _doneT += dt;
            _nextHint.alpha = Mathf.Repeat(Time.unscaledTime, 1f) < 0.6f ? 1 : 0.25f;
            if (AutoAdvanceDelay > 0 && _doneT >= AutoAdvanceDelay) Next();
        }

        void HandleInput()
        {
            if (_busy) return;
            var kb = Keyboard.current;
            var pad = Gamepad.current;
            if ((kb != null && kb.escapeKey.wasPressedThisFrame) || (pad != null && pad.startButton.wasPressedThisFrame)) { Skip(); return; }
            bool next = (kb != null && (kb.enterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame ||
                                        kb.numpadEnterKey.wasPressedThisFrame || kb.rightArrowKey.wasPressedThisFrame))
                        || (pad != null && pad.buttonSouth.wasPressedThisFrame);
            if (next) Next();
        }

        /// <summary>Test hook: jump straight to a shot (used by the smoke test).</summary>
        public void JumpTo(int i)
        {
            StopAllCoroutines();
            _busy = false; _finished = false;
            _cut.color = new Color(0, 0, 0, 0);
            Show(Mathf.Clamp(i, 0, _shots.Count - 1));
        }
    }

    /// <summary>Raises onClick for clicks on an otherwise non-interactive graphic (never takes keyboard focus).</summary>
    public class ClickCatcher : MonoBehaviour, IPointerClickHandler
    {
        public Action onClick;
        public void OnPointerClick(PointerEventData e) => onClick?.Invoke();
    }
}
