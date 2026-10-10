using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace DataHeist
{
    /// <summary>
    /// Settings modal with AUDIO / VIDEO / CONTROLS / CREDITS tabs. Every change is applied and saved
    /// immediately (SaveSystem + GameSettings; key bindings via RunnerInput's PlayerPrefs overrides,
    /// which the levels load on start).
    /// </summary>
    public class SettingsPanel : MonoBehaviour
    {
        public event Action OnClosed;

        RunnerInput _input;
        GameObject _audio, _video, _controls, _credits;
        Button _audioTab, _videoTab, _controlsTab, _creditsTab, _back;
        Selectable _audioFirst, _videoFirst, _controlsFirst;
        readonly List<(InputAction action, Button button)> _bindButtons = new List<(InputAction, Button)>();
        InputActionRebindingExtensions.RebindingOperation _rebind;
        TextMeshProUGUI _hint;
        int _rebindEndFrame = -1;

        public bool IsOpen => gameObject.activeSelf;

        public static SettingsPanel Create(RectTransform root)
        {
            var dim = UIKit.Image("Settings", root, new Color(0.04f, 0.02f, 0.09f, 0.78f));
            dim.raycastTarget = true;
            dim.rectTransform.Stretch();
            var panel = dim.gameObject.AddComponent<SettingsPanel>();
            panel.Build(dim.rectTransform);
            dim.gameObject.SetActive(false);
            return panel;
        }

        void Build(RectTransform root)
        {
            _input = new RunnerInput();
            _input.Map.Disable();   // only used for rebinding here; gameplay is not running

            var box = UIKit.Panel("Box", root, DH.Cyan);
            box.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(380, 270));
            var inner = UIKit.Rect("Inner", box.rectTransform).Stretch(16);

            var title = UIKit.Text("Title", inner, "SETTINGS", 14, DH.Cyan, TextAlignmentOptions.Center);
            title.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0.5f, 1), Vector2.zero, new Vector2(340, 20));

            var tabs = UIKit.Rect("Tabs", inner);
            tabs.Place(new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -26), new Vector2(340, 20));
            var h = tabs.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 6; h.childControlWidth = h.childControlHeight = true; h.childForceExpandWidth = true;
            _audioTab = UIKit.Button("AudioTab", tabs, "AUDIO", () => ShowTab(0), false);
            _videoTab = UIKit.Button("VideoTab", tabs, "VIDEO", () => ShowTab(1), false);
            _controlsTab = UIKit.Button("ControlsTab", tabs, "CONTROLS", () => ShowTab(2), false);
            _creditsTab = UIKit.Button("CreditsTab", tabs, "CREDITS", () => ShowTab(3), false);

            RectTransform Page(string name, out GameObject go)
            {
                var r = UIKit.Rect(name, inner);
                r.anchorMin = new Vector2(0, 0); r.anchorMax = new Vector2(1, 1);
                r.offsetMin = new Vector2(0, 30); r.offsetMax = new Vector2(0, -54);
                UIKit.VStack(r, 4);
                go = r.gameObject;
                return r;
            }

            // AUDIO ------------------------------------------------------------
            var a = Page("Audio", out _audio);
            var d = SaveSystem.Data;
            _audioFirst = UIKit.Slider(a, "MASTER", d.masterVolume, v => { d.masterVolume = v; GameSettings.ApplyAudio(); SaveSystem.Save(); });
            UIKit.Slider(a, "MUSIC", d.musicVolume, v => { d.musicVolume = v; GameSettings.ApplyAudio(); SaveSystem.Save(); });
            UIKit.Slider(a, "SOUND FX", d.sfxVolume, v =>
            {
                d.sfxVolume = v; SaveSystem.Save();
                AudioManager.I.Sfx("byte", 0.6f);   // instant feedback at the new level
            });

            // VIDEO ------------------------------------------------------------
            var vp = Page("Video", out _video);
            var res = GameSettings.Resolutions();
            var cur = GameSettings.CurrentResolution();
            int resIndex = Mathf.Max(0, res.FindIndex(r => r == cur));
            if (res.FindIndex(r => r == cur) < 0) { res.Add(cur); resIndex = res.Count - 1; }
            var resNames = res.ConvertAll(r => $"{r.x} x {r.y}").ToArray();
            _videoFirst = UIKit.Stepper(vp, "RESOLUTION", resNames, resIndex, i =>
            {
                d.resolutionWidth = res[i].x; d.resolutionHeight = res[i].y;
                GameSettings.ApplyDisplay(); SaveSystem.Save();
            });
            UIKit.Stepper(vp, "DISPLAY", new[] { "WINDOWED", "FULLSCREEN" }, d.fullscreen ? 1 : 0, i =>
            {
                d.fullscreen = i == 1; GameSettings.ApplyDisplay(); SaveSystem.Save();
            });
            UIKit.Stepper(vp, "VSYNC", new[] { "OFF", "ON" }, d.vsync ? 1 : 0, i =>
            {
                d.vsync = i == 1; GameSettings.ApplyDisplay(); SaveSystem.Save();
            });
            var note = UIKit.Text("Note", vp, Application.isEditor ? "Resolution and display mode apply in the built game." : "", 5, DH.Dim, TextAlignmentOptions.Center);
            UIKit.Size(note, 12);

            // CONTROLS ---------------------------------------------------------
            var c = Page("Controls", out _controls);
            foreach (var action in new[] { _input.Jump, _input.Slide, _input.Attack, _input.Ability, _input.Pause })
            {
                var act = action;
                Button b = null;
                b = UIKit.Button(act.name, c, "", () => StartRebind(act, b), false);
                UIKit.Size(b, 17);
                b.GetComponentInChildren<TextMeshProUGUI>().margin = new Vector4(12, 0, 12, 0);
                _bindButtons.Add((act, b));
                _controlsFirst ??= b;
            }
            var reset = UIKit.Button("Reset", c, "RESET DEFAULTS", () => { CancelRebind(); _input.ResetOverrides(); RefreshBindings(); }, false);
            UIKit.Size(reset, 17);

            // CREDITS ----------------------------------------------------------
            var cr = Page("Credits", out _credits);
            var credits = UIKit.Text("Text", cr, CreditsText(), 5, DH.Ink, TextAlignmentOptions.Center);
            credits.lineSpacing = 14;
            UIKit.Size(credits, 150);

            _hint = UIKit.Text("Hint", inner, "", 5, DH.Dim, TextAlignmentOptions.Center);
            _hint.rectTransform.Place(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 26), new Vector2(340, 10));
            _back = UIKit.Button("Back", inner, "BACK", Close, true);
            _back.GetComponent<RectTransform>().Place(new Vector2(0.5f, 0), new Vector2(0.5f, 0), Vector2.zero, new Vector2(120, 22));
            RefreshBindings();
        }

        public void Open()
        {
            gameObject.SetActive(true);
            ShowTab(0);
        }

        public void Close()
        {
            CancelRebind();
            gameObject.SetActive(false);
            AudioManager.I.Sfx("ui_back");
            OnClosed?.Invoke();
        }

        void Update()
        {
            // ESC cancels a rebind instead of closing (the cancel lands earlier in the same frame)
            if (_rebind != null || Time.frameCount <= _rebindEndFrame + 1) return;
            var kb = Keyboard.current;
            if (kb != null && kb.escapeKey.wasPressedThisFrame) Close();
            var pad = Gamepad.current;
            if (pad != null && pad.buttonEast.wasPressedThisFrame) Close();
        }

        void ShowTab(int i)
        {
            CancelRebind();
            _audio.SetActive(i == 0); _video.SetActive(i == 1); _controls.SetActive(i == 2); _credits.SetActive(i == 3);
            Mark(_audioTab, i == 0); Mark(_videoTab, i == 1); Mark(_controlsTab, i == 2); Mark(_creditsTab, i == 3);
            _hint.text = i == 2 ? "SELECT AN ACTION, THEN PRESS THE NEW KEY.  ESC CANCELS." : i == 3 ? "ESC BACK" : "< > CHANGE      ESC BACK";
            UIKit.Select(i == 0 ? _audioFirst : i == 1 ? _videoFirst : i == 2 ? _controlsFirst : _creditsTab);
        }

        /// <summary>Credits screen; the audio lines come from Audio/CREDITS.txt written by build_audio.py.</summary>
        static string CreditsText()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("<color=#2ff3ff>THE DATA HEIST</color>");
            sb.AppendLine("<color=#8a7fb0>A GAME DESIGN CLASS PROJECT</color>");
            sb.AppendLine();
            sb.AppendLine("<color=#ff3fa4>GAME DESIGN, CODE AND PIXEL ART</color>");
            sb.AppendLine("THE DATA HEIST CREW");
            sb.AppendLine();
            sb.AppendLine("<color=#ff3fa4>MUSIC AND SOUND</color>");
            var audio = Resources.Load<TextAsset>(DH.Root + "Audio/CREDITS");
            if (audio != null)
                foreach (var line in audio.text.Split('\n'))
                    if (line.Trim().Length > 0) sb.AppendLine(line.Trim().Replace("Sound effects: ", "").Replace("Music: ", ""));
            sb.AppendLine();
            sb.AppendLine("<color=#ff3fa4>FONT</color>");
            sb.AppendLine("Press Start 2P by CodeMan38 (SIL Open Font License)");
            return sb.ToString();
        }

        static void Mark(Button tab, bool on)
        {
            var img = tab.GetComponent<Image>();
            img.color = on ? new Color(DH.Cyan.r, DH.Cyan.g, DH.Cyan.b, 0.35f) : new Color(1, 1, 1, 0.06f);
        }

        void StartRebind(InputAction action, Button b)
        {
            CancelRebind();
            b.Label($"<align=left>{action.name.ToUpperInvariant()}<line-height=0>\n<align=right><color=#ffd23a>PRESS A KEY...</color>");
            _rebind = _input.Rebind(action, () =>
            {
                _rebind = null;
                _rebindEndFrame = Time.frameCount;
                _input.Map.Disable();
                RefreshBindings();
            });
        }

        void CancelRebind()
        {
            if (_rebind == null) return;
            var op = _rebind;
            _rebind = null;
            op.Cancel();
        }

        void RefreshBindings()
        {
            foreach (var (act, b) in _bindButtons)
                b.Label($"<align=left>{act.name.ToUpperInvariant()}<line-height=0>\n<align=right><color=#2ff3ff>{RunnerInput.KeyLabel(act)}</color>");
        }

        void OnDestroy()
        {
            CancelRebind();
            _input?.Dispose();
        }
    }
}
