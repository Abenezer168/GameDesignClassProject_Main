using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DataHeist
{
    /// <summary>
    /// The Data Heist home page (MainMenu scene), built entirely at runtime. Behind the menu the
    /// real Level 1 plays itself (DemoPlayer replays a recorded clean run through the actual
    /// simulation), a "HOW TO RUN" card lights up each control as the demo runner uses it, and the
    /// Halcyon tower looms on the skyline: the screen shows what the game is before you press a key.
    /// Menu: PLAY / STORY / SHOP / OPTIONS / QUIT, plus wallet and best rank.
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        struct Item { public string label, description; public Action action; }

        /// <summary>Panel to open as soon as the home page appears ("options" or "shop").</summary>
        public static string OpenOnStart;

        // the key-art splash is shown once per launch; nothing makes a sound before the first press
        static bool _started;
        GameObject _splash;
        TextMeshProUGUI _pressStart, _hint, _splashLogo, _splashCyan, _splashPink;

        readonly List<Button> _buttons = new List<Button>();
        readonly List<string> _descriptions = new List<string>();
        TextMeshProUGUI _logo, _logoCyan, _logoPink, _desc;
        SettingsPanel _settings;
        ShopPanel _shop;
        MenuBackdrop _backdrop;          // fallback when no demo route is recorded
        DemoPlayer _demo;
        RunnerInput _input;              // only for showing the player's key bindings
        TextMeshProUGUI _live;
        readonly List<(SimEventType[] events, Image key, TextMeshProUGUI keyText, TextMeshProUGUI verb, InputAction action)> _controls =
            new List<(SimEventType[], Image, TextMeshProUGUI, TextMeshProUGUI, InputAction)>();
        readonly Dictionary<Image, float> _flash = new Dictionary<Image, float>();
        TextMeshProUGUI _walletText, _bestText;
        GameObject _menuRoot;
        float _glitchT = 2f, _glitchLeft;
        int _selected;

        void Start()
        {
            _input = new RunnerInput();
            _input.Map.Disable();
            if (_started || OpenOnStart != null)
            {
                // coming back from a level, the story or an old scene: straight to the home page
                _started = true;
                BuildHome();
                EnterMenu();
                SceneFader.FadeIn(0.6f);
            }
            else
            {
                BuildSplash();
                SceneFader.FadeIn(0.8f);
            }
        }

        /// <summary>Demo world + menu UI. On launch this runs under the loading screen.</summary>
        void BuildHome()
        {
            // runner sits right of the menu column with 12 units of track visible ahead
            _demo = DemoPlayer.Create(GameSession.SelectedCharacter, 8f);
            if (_demo != null)
            {
                HalcyonSkyline.Create(Camera.main.transform, 0.68f);
                _demo.OnSimEvent += OnDemoEvent;
            }
            else
            {
                _backdrop = MenuBackdrop.Create();
                _backdrop.runnerId = GameSession.SelectedCharacter;
                _backdrop.runnerScreenX = 0.74f;
                _backdrop.AddTower(0.66f);
            }
            BuildUI();
            ScreenFX.Add();
        }

        // ------------------------------------------------------------------ key-art splash
        void BuildSplash()
        {
            // a plain camera so the screen isn't empty before the demo world exists
            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true; cam.orthographicSize = DH.OrthoSize;
            cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = DH.Night;
            cam.nearClipPlane = -50; cam.farClipPlane = 50;
            camGo.AddComponent<AudioListener>();

            var canvas = UIKit.Canvas("KeyArtSplash", 100);
            _splash = canvas.gameObject;
            var root = (RectTransform)canvas.transform;
            var art = UIKit.Image("KeyArt", root, Color.white, SpriteLibrary.Get("Story/keyart"));
            art.rectTransform.Stretch();
            art.raycastTarget = true;

            var pre = UIKit.Text("The", root, "THE", 8, DH.Amber, TextAlignmentOptions.Center);
            pre.characterSpacing = 40;
            pre.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -12), new Vector2(200, 10));
            _splashCyan = SplashLogo(root, DH.Cyan, -1);
            _splashPink = SplashLogo(root, DH.Pink, 1);
            _splashLogo = SplashLogo(root, DH.Ink, 0);

            _pressStart = UIKit.Text("PressStart", root, "PRESS START", 10, DH.Cyan, TextAlignmentOptions.Center);
            _pressStart.characterSpacing = 20;
            _pressStart.rectTransform.Place(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 30), new Vector2(300, 14));
            var footer = UIKit.Text("Footer", root, "ANY KEY  /  CLICK  /  PAD A", 5, DH.Dim, TextAlignmentOptions.Center);
            footer.rectTransform.Place(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 16), new Vector2(300, 8));
            var version = UIKit.Text("Version", root, "v0.6  //  GAME DESIGN CLASS PROJECT", 4, new Color(1, 1, 1, 0.35f), TextAlignmentOptions.Right);
            version.rectTransform.Place(new Vector2(1, 0), new Vector2(1, 0), new Vector2(-10, 6), new Vector2(240, 8));
        }

        TextMeshProUGUI SplashLogo(RectTransform root, Color c, float offset)
        {
            var t = UIKit.Text("Logo", root, "DATA HEIST", 24, c, TextAlignmentOptions.Center);
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(offset, -24), new Vector2(400, 30));
            if (offset != 0) t.alpha = 0.7f;
            return t;
        }

        void UpdateSplash(float dt)
        {
            _pressStart.alpha = Mathf.Repeat(Time.unscaledTime, 1.2f) < 0.8f ? 1f : 0.2f;
            UpdateGlitchTimer(dt);
            float j = _glitchLeft > 0 ? UnityEngine.Random.Range(-3, 4) : 0;
            _splashCyan.rectTransform.anchoredPosition = new Vector2(-1 - j, -24);
            _splashPink.rectTransform.anchoredPosition = new Vector2(1 + j, -24);
        }

        void BuildUI()
        {
            var canvas = UIKit.Canvas("MainMenu", 10);
            var root = (RectTransform)canvas.transform;

            // readability gradient behind the left column
            var shade = UIKit.Image("Shade", root, new Color(DH.Night.r, DH.Night.g, DH.Night.b, 0.92f), UIKit.FadeLeftSprite);
            shade.type = Image.Type.Simple;
            shade.rectTransform.anchorMin = new Vector2(0, 0); shade.rectTransform.anchorMax = new Vector2(0, 1);
            shade.rectTransform.pivot = new Vector2(0, 0.5f);
            shade.rectTransform.offsetMin = Vector2.zero; shade.rectTransform.offsetMax = new Vector2(360, 0);

            _menuRoot = UIKit.Rect("Menu", root).Stretch().gameObject;
            var menu = (RectTransform)_menuRoot.transform;

            // logo: two offset colour ghosts behind the main text give a chromatic, glitchable title
            var pre = UIKit.Text("LogoThe", menu, "THE", 9, DH.Amber);
            pre.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(34, -34), new Vector2(200, 12));
            pre.characterSpacing = 30;
            _logoCyan = LogoLayer(menu, DH.Cyan, new Vector2(-1, 0));
            _logoPink = LogoLayer(menu, DH.Pink, new Vector2(1, 0));
            _logo = LogoLayer(menu, DH.Ink, Vector2.zero);
            var tag = UIKit.Text("Tagline", menu, "STEAL THE SHARDS.  OWN THE NIGHT.", 6, DH.Ink);
            tag.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(34, -84), new Vector2(300, 10));
            var genre = UIKit.Text("Genre", menu, "A CYBERPUNK ROOFTOP AUTO-RUNNER", 5, DH.Dim);
            genre.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(34, -96), new Vector2(300, 8));
            var line = UIKit.Image("Rule", menu, DH.Pink);
            line.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(34, -110), new Vector2(64, 2));

            // menu
            var items = new[]
            {
                new Item { label = "PLAY", description = "Choose a runner and hit the rooftops.", action = Play },
                new Item { label = "STORY", description = "Watch the intro again: how the crew ended up on the run.", action = Story },
                new Item { label = "SHOP", description = "Spend your Data Bytes to recruit and equip runners.", action = OpenShop },
                new Item { label = "OPTIONS", description = "Volume, display and key bindings.", action = OpenSettings },
                new Item { label = "QUIT", description = "Log off for the night.", action = Quit },
            };
            for (int i = 0; i < items.Length; i++)
            {
                int index = i;
                var b = MenuButton(menu, items[i].label, new Vector2(34, -128 - i * 24), items[i].action);
                b.gameObject.AddComponent<MenuItemFx>().OnSelected = () => OnSelect(index);
                _buttons.Add(b);
                _descriptions.Add(items[i].description);
            }
            for (int i = 0; i < _buttons.Count; i++)
            {
                var nav = new Navigation
                {
                    mode = Navigation.Mode.Explicit,
                    selectOnUp = _buttons[(i - 1 + _buttons.Count) % _buttons.Count],
                    selectOnDown = _buttons[(i + 1) % _buttons.Count],
                };
                _buttons[i].navigation = nav;
            }

            _desc = UIKit.Text("Description", menu, "", 6, DH.Ink);
            _desc.rectTransform.Place(new Vector2(0, 0), new Vector2(0, 0), new Vector2(34, 44), new Vector2(300, 20));

            // wallet / progress card
            var card = UIKit.Panel("Progress", menu, DH.Pink);
            card.rectTransform.Place(new Vector2(1, 1), new Vector2(1, 1), new Vector2(-16, -16), new Vector2(170, 46));
            _walletText = UIKit.Text("Wallet", card.rectTransform, "", 7, DH.Ink);
            _walletText.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(10, -10), new Vector2(150, 10));
            _bestText = UIKit.Text("Best", card.rectTransform, "", 5, DH.Dim);
            _bestText.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(10, -26), new Vector2(150, 10));
            RefreshProgress();
            if (SaveSystem.Data.guardianAward)
            {
                // the campaign is done: the mission report (stats, replay the ending, credits)
                var report = UIKit.Button("MissionReport", menu, "MISSION REPORT  >", () => SceneFader.Load(DH.SceneCompletion), false);
                report.GetComponent<RectTransform>().Place(new Vector2(1, 1), new Vector2(1, 1), new Vector2(-16, -66), new Vector2(170, 16));
                report.GetComponentInChildren<TextMeshProUGUI>().fontSize = 6;
                var rn = report.navigation; rn.mode = Navigation.Mode.None; report.navigation = rn;
            }

            _hint = UIKit.Text("Hint", menu, "UP/DOWN  NAVIGATE      ENTER  SELECT", 5, new Color(1, 1, 1, 0.45f));
            _hint.rectTransform.Place(new Vector2(0, 0), new Vector2(0, 0), new Vector2(34, 16), new Vector2(300, 10));

            BuildShowcase(menu);

            _settings = SettingsPanel.Create(root);
            _settings.OnClosed += () => { _menuRoot.SetActive(true); _input.LoadOverrides(); RefreshKeys(); UIKit.Select(_buttons[3]); };
            _shop = ShopPanel.Create(root);
            _shop.OnClosed += () => { _menuRoot.SetActive(true); UIKit.Select(_buttons[2]); };
            _shop.OnChanged += () =>
            {
                RefreshProgress();
                if (_demo != null) _demo.SetCharacter(GameSession.SelectedCharacter);
                else _backdrop.runnerId = GameSession.SelectedCharacter;
            };

        }

        // ------------------------------------------------------------------ showcase
        /// <summary>"LIVE" feed tag and the HOW TO RUN card that mirrors the demo runner's moves.</summary>
        void BuildShowcase(RectTransform menu)
        {
            _live = UIKit.Text("LiveTag", menu, "", 5, DH.Ink, TextAlignmentOptions.Center);
            _live.textWrappingMode = TextWrappingModes.NoWrap;
            _live.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -14), new Vector2(240, 8));
            _live.gameObject.SetActive(_demo != null);

            var card = UIKit.Panel("HowToRun", menu, DH.Cyan);
            card.rectTransform.Place(new Vector2(1, 0), new Vector2(1, 0), new Vector2(-16, 16), new Vector2(150, 92));
            var r = card.rectTransform;
            var head = UIKit.Text("Head", r, "HOW TO RUN", 6, DH.Cyan);
            head.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(10, -9), new Vector2(130, 8));
            var sub = UIKit.Text("Sub", r, "YOU RUN ON YOUR OWN. YOU CONTROL:", 4, DH.Dim);
            sub.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(10, -19), new Vector2(136, 6));
            void Row(int i, InputAction action, string verb, params SimEventType[] events)
            {
                float y = -30 - i * 14;
                var key = UIKit.Image("Key", r, DH.PurpleLine);
                key.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(10, y), new Vector2(40, 11));
                var keyText = UIKit.Text("KeyLabel", key.rectTransform, "", 5, DH.Ink, TextAlignmentOptions.Center);
                keyText.rectTransform.Stretch();
                var v = UIKit.Text("Verb", r, verb, 5, DH.Ink);
                v.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(56, y - 2), new Vector2(90, 8));
                _controls.Add((events, key, keyText, v, action));
            }
            Row(0, _input.Jump, "JUMP GAPS", SimEventType.Jump, SimEventType.AirJump);
            Row(1, _input.Slide, "SLIDE UNDER", SimEventType.Slide, SimEventType.Dive);
            Row(2, _input.Attack, "SLASH DRONES", SimEventType.Attack, SimEventType.Deflect);
            Row(3, _input.Ability, "RUNNER ABILITY", SimEventType.Dash, SimEventType.Emp, SimEventType.Glitch);
            RefreshKeys();
        }

        void RefreshKeys()
        {
            foreach (var c in _controls) c.keyText.text = RunnerInput.KeyLabel(c.action);
        }

        void OnDemoEvent(SimEventType type)
        {
            foreach (var c in _controls)
                if (Array.IndexOf(c.events, type) >= 0) _flash[c.key] = 1f;
        }

        void UpdateShowcase(float dt)
        {
            if (_live == null) return;
            foreach (var c in _controls)
            {
                _flash.TryGetValue(c.key, out float k);
                k = Mathf.Max(0, k - dt / 0.45f);
                _flash[c.key] = k;
                c.key.color = Color.Lerp(DH.PurpleLine, DH.Cyan, k);
                c.keyText.color = Color.Lerp(DH.Ink, DH.Night, k);
                c.verb.color = Color.Lerp(DH.Ink, DH.Cyan, k);
            }
            if (_demo != null && _demo.Sim != null)
            {
                bool rec = Mathf.Repeat(Time.unscaledTime, 1f) < 0.55f;
                int metres = Mathf.Max(0, Mathf.RoundToInt(_demo.Sim.p.x));
                _live.text = $"<color=#ff2d55>{(rec ? "REC" : "   ")}</color>  LIVE  //  {_demo.Level.name}  //  {metres} M";
            }
        }

        void OnDestroy() => _input?.Dispose();

        /// <summary>
        /// PRESS START on the splash: the loading screen takes over and does the real loading.
        /// First launch continues to the story; otherwise the home page is built under it.
        /// </summary>
        void PressStart()
        {
            if (_started || LoadingScreen.Active) return;
            _started = true;
            AudioManager.I.Sfx("ui_select", 0.8f);
            bool story = !SaveSystem.Data.storySeen && Application.CanStreamedLevelBeLoaded(DH.SceneStory);
            if (story)
            {
                LoadingScreen.Run(DH.SceneStory, GamePreloader.Steps(story: true), sceneLabel: "ROLLING THE ARCHIVE FOOTAGE");
                return;
            }
            var steps = new List<(string, Action)>(GamePreloader.Steps(story: false))
            {
                ("BOOTING THE LIVE FEED", () => { BuildHome(); Destroy(_splash); }),
            };
            LoadingScreen.Run(null, steps, EnterMenu);
        }

        void EnterMenu()
        {
            AudioManager.I.Music("music_menu");
            UIKit.Select(_buttons[0]);
            OnSelect(0);
            var open = OpenOnStart;
            OpenOnStart = null;
            if (open == "options") { OnSelect(3); OpenSettings(); }
            else if (open == "shop") { OnSelect(2); OpenShop(); }
        }

        void RefreshProgress()
        {
            var rec = SaveSystem.GetRecord("level01");
            _walletText.text = $"<color=#2ff3ff>{SaveSystem.Data.bytes}</color> DATA BYTES";
            _bestText.text = rec != null ? $"LEVEL 1 BEST  <color=#ffd23a>{rec.rank}</color>  {rec.score}" : "LEVEL 1  NOT CLEARED";
        }

        TextMeshProUGUI LogoLayer(RectTransform parent, Color c, Vector2 offset)
        {
            var t = UIKit.Text("Logo", parent, "DATA HEIST", 26, c);
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(32, -48) + offset, new Vector2(320, 32));
            if (offset != Vector2.zero) t.alpha = 0.75f;
            return t;
        }

        static Button MenuButton(RectTransform parent, string label, Vector2 pos, Action onClick)
        {
            var b = UIKit.Button(label, parent, label, onClick, false);
            var r = b.GetComponent<RectTransform>();
            r.Place(new Vector2(0, 1), new Vector2(0, 1), pos, new Vector2(170, 20));
            // a flat, text-led look: no box, the selection marker does the work
            UnityEngine.Object.Destroy(b.GetComponent<Outline>());
            var img = b.GetComponent<Image>();
            img.color = new Color(1, 1, 1, 0);
            b.transition = Selectable.Transition.None;
            var t = b.GetComponentInChildren<TextMeshProUGUI>();
            t.fontSize = 11;
            t.alignment = TextAlignmentOptions.Left;
            t.rectTransform.offsetMin = new Vector2(14, 0);
            var marker = UIKit.Image("Marker", r, DH.Pink);
            marker.rectTransform.Place(new Vector2(0, 0.5f), new Vector2(0, 0.5f), Vector2.zero, new Vector2(4, 12));
            var bar = UIKit.Image("Bar", r, new Color(DH.Cyan.r, DH.Cyan.g, DH.Cyan.b, 0.12f));
            bar.rectTransform.Stretch();
            bar.transform.SetAsFirstSibling();
            return b;
        }

        void OnSelect(int index)
        {
            _selected = index;
            _desc.text = _descriptions[index];
            for (int i = 0; i < _buttons.Count; i++) _buttons[i].GetComponent<MenuItemFx>().Selected = i == index;
        }

        void Update()
        {
            if (_splash != null)
            {
                UpdateSplash(Time.unscaledDeltaTime);
                if (!_started && AnyPress()) PressStart();
                return;
            }
            if (_menuRoot == null) return;
            UpdateShowcase(Time.unscaledDeltaTime);
            if (LoadingScreen.Active)
            {
                // nothing underneath the loading screen may take keyboard input
                if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
                return;
            }
            // keep a selection when the mouse clicks empty space, so keyboard/gamepad never get lost
            if (_menuRoot.activeSelf && EventSystem.current != null && EventSystem.current.currentSelectedGameObject == null)
                UIKit.Select(_buttons[_selected]);
            UpdateGlitch(Time.unscaledDeltaTime);
        }

        void UpdateGlitchTimer(float dt)
        {
            _glitchT -= dt;
            if (_glitchT <= 0)
            {
                _glitchLeft = UnityEngine.Random.Range(0.08f, 0.22f);
                _glitchT = UnityEngine.Random.Range(2.5f, 6f);
            }
            if (_glitchLeft > 0) _glitchLeft -= dt;
        }

        void UpdateGlitch(float dt)
        {
            UpdateGlitchTimer(dt);
            float jitter = _glitchLeft > 0 ? UnityEngine.Random.Range(-3, 4) : 0;
            var basePos = new Vector2(32, -48);
            _logoCyan.rectTransform.anchoredPosition = basePos + new Vector2(-1 - jitter, jitter * 0.3f);
            _logoPink.rectTransform.anchoredPosition = basePos + new Vector2(1 + jitter, 0);
            _logo.rectTransform.anchoredPosition = basePos + new Vector2(_glitchLeft > 0 && UnityEngine.Random.value < 0.3f ? jitter : 0, 0);
        }

        static bool AnyPress()
        {
            var kb = UnityEngine.InputSystem.Keyboard.current;
            var mouse = UnityEngine.InputSystem.Mouse.current;
            var pad = UnityEngine.InputSystem.Gamepad.current;
            return (kb != null && kb.anyKey.wasPressedThisFrame)
                   || (mouse != null && mouse.leftButton.wasPressedThisFrame)
                   || (pad != null && (pad.buttonSouth.wasPressedThisFrame || pad.startButton.wasPressedThisFrame));
        }

        // ------------------------------------------------------------------ actions
        static void Play() => SceneFader.Load(DH.SceneCharacterSelect);

        void Story()
        {
            if (Application.CanStreamedLevelBeLoaded(DH.SceneStory)) SceneFader.Load(DH.SceneStory);
            else
            {
                AudioManager.I.Sfx("denied", 0.5f);
                _desc.text = "<color=#ff2d55>Story scene not installed yet. Run Data Heist > Setup Project.</color>";
            }
        }

        void OpenSettings()
        {
            _menuRoot.SetActive(false);
            _settings.Open();
        }

        void OpenShop()
        {
            _menuRoot.SetActive(false);
            _shop.Open();
        }

        static void Quit()
        {
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }
    }

    /// <summary>Selection styling for a title-menu entry: marker, tint, slide-in and hover-to-select.</summary>
    public class MenuItemFx : MonoBehaviour, ISelectHandler, IPointerEnterHandler
    {
        public Action OnSelected;
        public bool Selected;
        TextMeshProUGUI _label;
        RectTransform _text;
        Image _marker, _bar;
        float _k;

        void Awake()
        {
            _label = GetComponentInChildren<TextMeshProUGUI>();
            _text = _label.rectTransform;
        }

        void Start()
        {
            _marker = transform.Find("Marker").GetComponent<Image>();
            _bar = transform.Find("Bar").GetComponent<Image>();
        }

        public void OnSelect(BaseEventData e) => OnSelected?.Invoke();

        public void OnPointerEnter(PointerEventData e)
        {
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(gameObject);
        }

        void Update()
        {
            if (_marker == null) return;
            _k = Mathf.MoveTowards(_k, Selected ? 1 : 0, Time.unscaledDeltaTime * 8f);
            float e = Mathf.SmoothStep(0, 1, _k);
            _text.offsetMin = new Vector2(14 + 8 * e, 0);
            _label.color = Color.Lerp(DH.Dim, DH.Cyan, e);
            _marker.color = new Color(DH.Pink.r, DH.Pink.g, DH.Pink.b, e);
            _marker.rectTransform.sizeDelta = new Vector2(4, 12 * e);
            _bar.color = new Color(DH.Cyan.r, DH.Cyan.g, DH.Cyan.b, 0.12f * e);
        }
    }

    /// <summary>
    /// The team's original Options / Shop / GameScreen / GameOver scenes now live inside the home
    /// page; if one of them is loaded, send the player to the new screen instead of dead buttons.
    /// </summary>
    static class LegacySceneRouter
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Hook()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            switch (scene.name)
            {
                case "OptionsScreenScene": Reroute(scene, DH.SceneMainMenu, "options"); break;
                case "ShopScreenScene": Reroute(scene, DH.SceneMainMenu, "shop"); break;
                case "GameScreenScene": Reroute(scene, DH.SceneCharacterSelect, null); break;
                case "GameOverScreenScene": Reroute(scene, DH.SceneMainMenu, null); break;
            }
        }

        static void Reroute(Scene from, string to, string openPanel)
        {
            foreach (var go in from.GetRootGameObjects()) go.SetActive(false);
            MainMenuController.OpenOnStart = openPanel;
            SceneManager.LoadScene(to);
        }
    }
}
