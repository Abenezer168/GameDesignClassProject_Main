using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DataHeist
{
    /// <summary>
    /// The Data Heist title screen, built entirely at runtime: living city backdrop, glitching
    /// logo, PLAY / STORY / RUNNERS / SETTINGS / QUIT, wallet and best rank.
    /// It installs itself into MainMenuScreenScene (see MainMenuInstaller), so the team's scene
    /// file never has to be edited.
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        struct Item { public string label, description; public Action action; }

        readonly List<Button> _buttons = new List<Button>();
        readonly List<string> _descriptions = new List<string>();
        TextMeshProUGUI _logo, _logoCyan, _logoPink, _desc;
        SettingsPanel _settings;
        GameObject _menuRoot;
        float _glitchT = 2f, _glitchLeft;
        int _selected;

        void Start()
        {
            var backdrop = MenuBackdrop.Create();
            backdrop.runnerId = GameSession.SelectedCharacter;
            backdrop.runnerScreenX = 0.74f;
            BuildUI();
            AudioManager.I.Music("music_menu");
            SceneFader.FadeIn(0.6f);
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
            var tag = UIKit.Text("Tagline", menu, "STEAL THE SHARDS.  OWN THE NIGHT.", 6, DH.Dim);
            tag.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(34, -86), new Vector2(300, 10));
            var line = UIKit.Image("Rule", menu, DH.Pink);
            line.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(34, -102), new Vector2(64, 2));

            // menu
            var items = new[]
            {
                new Item { label = "PLAY", description = "Choose a runner and hit the rooftops.", action = Play },
                new Item { label = "STORY", description = "How the crew ended up on the run.", action = Story },
                new Item { label = "RUNNERS", description = "Meet the crew and recruit new runners with Data Bytes.", action = () => SceneFader.Load(DH.SceneCharacterSelect) },
                new Item { label = "SETTINGS", description = "Audio, display and key bindings.", action = OpenSettings },
                new Item { label = "QUIT", description = "Log off for the night.", action = Quit },
            };
            for (int i = 0; i < items.Length; i++)
            {
                int index = i;
                var b = MenuButton(menu, items[i].label, new Vector2(34, -126 - i * 24), items[i].action);
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
            var data = SaveSystem.Data;
            var rec = SaveSystem.GetRecord("level01");
            var wallet = UIKit.Text("Wallet", card.rectTransform, $"<color=#2ff3ff>{data.bytes}</color> DATA BYTES", 7, DH.Ink);
            wallet.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(10, -10), new Vector2(150, 10));
            var best = UIKit.Text("Best", card.rectTransform,
                rec != null ? $"LEVEL 1 BEST  <color=#ffd23a>{rec.rank}</color>  {rec.score}" : "LEVEL 1  NOT CLEARED", 5, DH.Dim);
            best.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(10, -26), new Vector2(150, 10));

            var hint = UIKit.Text("Hint", menu, "UP/DOWN  NAVIGATE      ENTER  SELECT", 5, new Color(1, 1, 1, 0.45f));
            hint.rectTransform.Place(new Vector2(0, 0), new Vector2(0, 0), new Vector2(34, 16), new Vector2(300, 10));

            _settings = SettingsPanel.Create(root);
            _settings.OnClosed += () => { _menuRoot.SetActive(true); UIKit.Select(_buttons[3]); };

            UIKit.Select(_buttons[0]);
            OnSelect(0);
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
            // keep a selection when the mouse clicks empty space, so keyboard/gamepad never get lost
            if (_menuRoot.activeSelf && EventSystem.current != null && EventSystem.current.currentSelectedGameObject == null)
                UIKit.Select(_buttons[_selected]);
            UpdateGlitch(Time.unscaledDeltaTime);
        }

        void UpdateGlitch(float dt)
        {
            _glitchT -= dt;
            if (_glitchT <= 0)
            {
                _glitchLeft = UnityEngine.Random.Range(0.08f, 0.22f);
                _glitchT = UnityEngine.Random.Range(2.5f, 6f);
                AudioManager.I.Sfx("glitch", 0.12f, 1.6f);
            }
            float jitter = 0;
            if (_glitchLeft > 0)
            {
                _glitchLeft -= dt;
                jitter = UnityEngine.Random.Range(-3, 4);
            }
            var basePos = new Vector2(32, -48);
            _logoCyan.rectTransform.anchoredPosition = basePos + new Vector2(-1 - jitter, jitter * 0.3f);
            _logoPink.rectTransform.anchoredPosition = basePos + new Vector2(1 + jitter, 0);
            _logo.rectTransform.anchoredPosition = basePos + new Vector2(_glitchLeft > 0 && UnityEngine.Random.value < 0.3f ? jitter : 0, 0);
        }

        // ------------------------------------------------------------------ actions
        void Play()
        {
            bool storyFirst = !SaveSystem.Data.storySeen && Application.CanStreamedLevelBeLoaded(DH.SceneStory);
            SceneFader.Load(storyFirst ? DH.SceneStory : DH.SceneCharacterSelect);
        }

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
    /// Puts the Data Heist title screen into MainMenuScreenScene at load time: the team's old
    /// canvas and its manager are switched off (not deleted), so the scene file stays untouched.
    /// </summary>
    static class MainMenuInstaller
    {
        static readonly string[] LegacyRoots = { "Canvas", "MainMenuManagerScript" };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Hook()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != DH.SceneMainMenu) return;
            if (UnityEngine.Object.FindAnyObjectByType<MainMenuController>() != null) return;
            foreach (var go in scene.GetRootGameObjects())
                if (Array.IndexOf(LegacyRoots, go.name) >= 0) go.SetActive(false);
            new GameObject("DataHeist Main Menu").AddComponent<MainMenuController>();
        }
    }
}
