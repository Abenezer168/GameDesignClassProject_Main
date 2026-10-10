using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace DataHeist
{
    /// <summary>Pause (with volume + key rebinding), Signal Lost (game over) and Extraction Complete (results) screens.</summary>
    public class LevelMenus : MonoBehaviour
    {
        public event Action OnResume, OnRestart, OnRetryCheckpoint, OnQuitToRunners, OnQuitToMenu, OnNextLevel;
        public event Action OnBindingsChanged;

        RunnerInput _input;
        GameObject _pause, _controls, _over, _results;
        Button _resumeBtn, _retryBtn, _continueBtn, _nextBtn, _controlsBack;
        TextMeshProUGUI _overCause, _rank, _rows, _unlocks, _wallet;
        readonly List<(InputAction action, Button button)> _bindButtons = new List<(InputAction, Button)>();

        public bool AnyOpen => _pause.activeSelf || _controls.activeSelf || _over.activeSelf || _results.activeSelf;
        public bool PauseOpen => _pause.activeSelf || _controls.activeSelf;

        public void Init(RunnerInput input)
        {
            _input = input;
            var canvas = UIKit.Canvas("Menus", 20);
            canvas.transform.SetParent(transform, false);
            var root = (RectTransform)canvas.transform;
            BuildPause(root);
            BuildControls(root);
            BuildOver(root);
            BuildResults(root);
            HideAll();
        }

        RectTransform Modal(string name, RectTransform root, Color accent, Vector2 size, out GameObject screen)
        {
            var dim = UIKit.Image(name, root, new Color(0.04f, 0.02f, 0.09f, 0.72f));
            dim.raycastTarget = true;
            dim.rectTransform.Stretch();
            screen = dim.gameObject;
            var box = UIKit.Panel("Box", dim.rectTransform, accent);
            box.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, size);
            var inner = UIKit.Rect("Inner", box.rectTransform).Stretch(16);
            UIKit.VStack(inner, 6);
            return inner;
        }

        TextMeshProUGUI Title(Transform parent, string text, Color c)
        {
            var t = UIKit.Text("Title", parent, text, 14, c, TextAlignmentOptions.Center);
            UIKit.Size(t, 24);
            return t;
        }

        Button Btn(Transform parent, string label, Action a, bool primary = false)
        {
            var b = UIKit.Button(label, parent, label, a, primary);
            UIKit.Size(b, 22);
            return b;
        }

        void BuildPause(RectTransform root)
        {
            var box = Modal("Pause", root, DH.Cyan, new Vector2(280, 272), out _pause);
            Title(box, "PAUSED", DH.Cyan);
            var d = SaveSystem.Data;
            UIKit.Slider(box, "MASTER", d.masterVolume, v => { d.masterVolume = v; GameSettings.ApplyAudio(); SaveSystem.Save(); });
            UIKit.Slider(box, "MUSIC", d.musicVolume, v => { d.musicVolume = v; GameSettings.ApplyAudio(); SaveSystem.Save(); });
            UIKit.Slider(box, "SOUND FX", d.sfxVolume, v => { d.sfxVolume = v; SaveSystem.Save(); });
            _resumeBtn = Btn(box, "RESUME", () => OnResume?.Invoke(), true);
            Btn(box, "CONTROLS", ShowControls);
            Btn(box, "RESTART LEVEL", () => OnRestart?.Invoke());
            Btn(box, "QUIT TO RUNNERS", () => OnQuitToRunners?.Invoke());
            Btn(box, "MAIN MENU", () => OnQuitToMenu?.Invoke());
        }

        void BuildControls(RectTransform root)
        {
            var box = Modal("Controls", root, DH.Cyan, new Vector2(300, 250), out _controls);
            Title(box, "CONTROLS", DH.Cyan);
            var hint = UIKit.Text("Hint", box, "Select an action, then press the new key. ESC cancels.", 6, DH.Dim, TextAlignmentOptions.Center);
            UIKit.Size(hint, 18);
            foreach (var a in new[] { _input.Jump, _input.Slide, _input.Attack, _input.Ability, _input.Pause })
            {
                var action = a;
                Button b = null;
                b = Btn(box, "", () => StartRebind(action, b));
                _bindButtons.Add((action, b));
            }
            Btn(box, "RESET DEFAULTS", () => { _input.ResetOverrides(); RefreshBindings(); OnBindingsChanged?.Invoke(); });
            _controlsBack = Btn(box, "BACK", ShowPause, true);
            RefreshBindings();
        }

        void StartRebind(InputAction action, Button b)
        {
            b.Label($"{action.name.ToUpperInvariant()}  ...PRESS A KEY");
            _input.Rebind(action, () => { RefreshBindings(); OnBindingsChanged?.Invoke(); });
        }

        void RefreshBindings()
        {
            foreach (var (a, b) in _bindButtons) b.Label($"{a.name.ToUpperInvariant(),-8}  {RunnerInput.KeyLabel(a)}");
        }

        void BuildOver(RectTransform root)
        {
            var box = Modal("SignalLost", root, DH.Pink, new Vector2(280, 190), out _over);
            Title(box, "SIGNAL LOST", DH.Red);
            _overCause = UIKit.Text("Cause", box, "", 8, DH.Ink, TextAlignmentOptions.Center);
            UIKit.Size(_overCause, 22);
            _retryBtn = Btn(box, "RETRY FROM CHECKPOINT", () => OnRetryCheckpoint?.Invoke(), true);
            Btn(box, "RESTART LEVEL", () => OnRestart?.Invoke());
            Btn(box, "QUIT TO RUNNERS", () => OnQuitToRunners?.Invoke());
        }

        void BuildResults(RectTransform root)
        {
            var box = Modal("Results", root, DH.Cyan, new Vector2(420, 274), out _results);
            Title(box, "EXTRACTION COMPLETE", DH.Cyan);
            var row = UIKit.Rect("Row", box);
            UIKit.Size(row, 130);
            _rank = UIKit.Text("Rank", row, "A", 64, DH.Amber, TextAlignmentOptions.Center);
            _rank.rectTransform.Place(new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0), new Vector2(110, 110));
            _rank.outlineWidth = 0.15f; _rank.outlineColor = new Color32(90, 31, 94, 255);
            _rows = UIKit.Text("Rows", row, "", 7, DH.Ink);
            _rows.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(120, 0), new Vector2(260, 80));
            _rows.lineSpacing = 30;
            _unlocks = UIKit.Text("Unlocks", row, "", 7, DH.Amber);
            _unlocks.rectTransform.Place(new Vector2(0, 0), new Vector2(0, 0), new Vector2(120, 14), new Vector2(260, 34));
            _wallet = UIKit.Text("Wallet", row, "", 7, DH.Dim);
            _wallet.rectTransform.Place(new Vector2(0, 0), new Vector2(0, 0), new Vector2(120, 0), new Vector2(260, 12));
            _nextBtn = Btn(box, "NEXT LEVEL", () => OnNextLevel?.Invoke(), true);
            _continueBtn = Btn(box, "CHANGE RUNNER", () => OnQuitToRunners?.Invoke());
            Btn(box, "RUN IT AGAIN", () => OnRestart?.Invoke());
        }

        public void HideAll()
        {
            _pause.SetActive(false); _controls.SetActive(false); _over.SetActive(false); _results.SetActive(false);
        }

        public void ShowPause() { HideAll(); _pause.SetActive(true); UIKit.Select(_resumeBtn); }
        void ShowControls() { HideAll(); _controls.SetActive(true); RefreshBindings(); UIKit.Select(_controlsBack); }

        public void ShowOver(string cause, bool hasCheckpoint)
        {
            HideAll();
            _overCause.text = cause switch
            {
                "fall" => "You fell.",
                "vault" => "The vault stayed sealed: it needs all 3 data fragments.\nRetry takes you back before the one you missed.",
                "lockdown" => "The facility locked down before you got out.",
                _ => "Corporate security took you down.",
            };
            _retryBtn.Label(hasCheckpoint ? "RETRY FROM CHECKPOINT" : "RETRY");
            _over.SetActive(true);
            UIKit.Select(_retryBtn);
        }

        /// <param name="nextLevelName">Shown on the NEXT LEVEL button; null hides it (end of the campaign).</param>
        public void ShowResults(string rank, string rowsText, string unlocks, int wallet, string nextLevelName = null)
        {
            HideAll();
            _rank.text = rank;
            _rank.color = rank == "S" ? DH.Pink : DH.Amber;
            _rows.text = rowsText;
            _unlocks.text = unlocks;
            _wallet.text = $"WALLET  {wallet} BYTES";
            _results.SetActive(true);
            _nextBtn.gameObject.SetActive(nextLevelName != null);
            if (nextLevelName != null) _nextBtn.Label("NEXT LEVEL:  " + nextLevelName);
            _continueBtn.Label(nextLevelName != null ? "CHANGE RUNNER" : "CONTINUE");
            UIKit.Select(nextLevelName != null ? _nextBtn : _continueBtn);
        }
    }
}
