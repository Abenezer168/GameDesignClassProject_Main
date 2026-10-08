using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DataHeist
{
    /// <summary>
    /// GDD heads-up display: health bar, Data Byte counter, Data Shard indicator, control
    /// reminder - plus level progress, timer, ability cooldown, tutorial prompts and countdown.
    /// </summary>
    public class LevelHUD : MonoBehaviour
    {
        RunnerSim _sim;
        RunnerInput _input;
        CharacterDefinition _ch;
        Canvas _canvas;
        readonly List<Image> _hearts = new List<Image>();
        Sprite _heartFull, _heartEmpty;
        TextMeshProUGUI _bytes, _timer, _pct, _abilityName, _abilityKey, _controls, _countdown, _countSub;
        Image _shard, _progressFill, _abilityIcon, _abilityCover, _abilityFrame;
        RectTransform _progressBar;
        readonly List<(Image img, SimEntity e)> _cpTicks = new List<(Image, SimEntity)>();
        // prompt card
        CanvasGroup _promptGroup;
        Image _promptLine1, _promptLine2;
        TextMeshProUGUI _promptKey, _promptTitle, _promptBody;
        // overlays
        Image _alarm, _flash, _glitch;
        public bool Alarm;
        float _flashT;
        public float GlitchT;

        public void Init(RunnerSim sim, RunnerInput input)
        {
            _sim = sim; _input = input; _ch = sim.ch;
            _canvas = UIKit.Canvas("HUD", 10);
            _canvas.transform.SetParent(transform, false);
            var root = (RectTransform)_canvas.transform;

            // overlays first (behind HUD widgets)
            _alarm = UIKit.Image("Alarm", root, new Color(1, 0.18f, 0.33f, 0), MakeVignette());
            _alarm.rectTransform.Stretch();
            _glitch = UIKit.Image("Glitch", root, new Color(0.5f, 1, 0.4f, 0), MakeScanlines());
            _glitch.type = Image.Type.Tiled;
            _glitch.rectTransform.Stretch();
            _flash = UIKit.Image("Flash", root, new Color(1, 1, 1, 0));
            _flash.rectTransform.Stretch();

            // hearts
            _heartFull = SpriteLibrary.Get("UI/heart_full");
            _heartEmpty = SpriteLibrary.Get("UI/heart_empty");
            for (int i = 0; i < _ch.maxHealth; i++)
            {
                var h = UIKit.Image("Heart" + i, root, Color.white, _heartFull);
                h.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(12 + i * 18, -12), new Vector2(16, 16));
                _hearts.Add(h);
            }
            // bytes + shard
            var bi = UIKit.Image("ByteIcon", root, Color.white, SpriteLibrary.Get("UI/icon_byte"));
            bi.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(12, -34), new Vector2(16, 16));
            _bytes = UIKit.Text("Bytes", root, "000", 10, DH.Cyan);
            _bytes.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(32, -36), new Vector2(60, 14));
            _shard = UIKit.Image("Shard", root, new Color(1, 1, 1, 0.3f), SpriteLibrary.Get("UI/icon_shard"));
            _shard.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(78, -28), new Vector2(16, 24));

            // progress
            var bg = UIKit.Image("ProgressBg", root, new Color(0.07f, 0.04f, 0.14f, 0.8f));
            bg.rectTransform.Place(new Vector2(1, 1), new Vector2(1, 1), new Vector2(-16, -12), new Vector2(164, 10));
            var bar = UIKit.Image("ProgressBar", bg.rectTransform, DH.PurpleLine);
            _progressBar = bar.rectTransform.Place(new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(2, 0), new Vector2(160, 6));
            _progressFill = UIKit.Image("Fill", _progressBar, DH.Cyan);
            _progressFill.rectTransform.Place(new Vector2(0, 0.5f), new Vector2(0, 0.5f), Vector2.zero, new Vector2(0, 6));
            foreach (var e in sim.ents)
            {
                if (e.t != "checkpoint") continue;
                var tick = UIKit.Image("CP", _progressBar, DH.Dim);
                tick.rectTransform.Place(new Vector2(0, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(160 * e.x / sim.L.exitX, 0), new Vector2(2, 12));
                _cpTicks.Add((tick, e));
            }
            var end = UIKit.Image("Exit", _progressBar, DH.Pink);
            end.rectTransform.Place(new Vector2(1, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(3, 12));
            _timer = UIKit.Text("Timer", root, "0:00.00", 8, DH.Ink);
            _timer.rectTransform.Place(new Vector2(1, 1), new Vector2(1, 1), new Vector2(-84, -28), new Vector2(96, 12));
            _pct = UIKit.Text("Pct", root, "0%", 8, DH.Ink, TextAlignmentOptions.Right);
            _pct.rectTransform.Place(new Vector2(1, 1), new Vector2(1, 1), new Vector2(-16, -28), new Vector2(60, 12));

            // ability
            var abBg = UIKit.Image("AbilityBg", root, new Color(0.07f, 0.04f, 0.14f, 0.85f));
            abBg.rectTransform.Place(new Vector2(0, 0), new Vector2(0, 0), new Vector2(10, 10), new Vector2(36, 36));
            _abilityFrame = UIKit.Image("AbilityFrame", abBg.rectTransform, _ch.color);
            _abilityFrame.rectTransform.Stretch(-1);
            _abilityFrame.transform.SetAsFirstSibling();
            _abilityIcon = UIKit.Image("AbilityIcon", abBg.rectTransform, Color.white, SpriteLibrary.Get("UI/ability_" + _ch.id));
            _abilityIcon.rectTransform.Stretch(2);
            _abilityCover = UIKit.Image("Cooldown", abBg.rectTransform, new Color(0.07f, 0.04f, 0.14f, 0.75f));
            _abilityCover.rectTransform.Stretch(2);
            _abilityCover.type = Image.Type.Filled;
            _abilityCover.fillMethod = Image.FillMethod.Vertical;
            _abilityCover.fillOrigin = (int)Image.OriginVertical.Top;
            _abilityKey = UIKit.Text("AbilityKey", root, "W", 8, DH.Amber);
            _abilityKey.rectTransform.Place(new Vector2(0, 0), new Vector2(0, 0), new Vector2(52, 30), new Vector2(160, 12));
            _abilityName = UIKit.Text("AbilityName", root, _ch.abilityName, 8, _ch.color);
            _abilityName.rectTransform.Place(new Vector2(0, 0), new Vector2(0, 0), new Vector2(52, 14), new Vector2(200, 12));

            _controls = UIKit.Text("Controls", root, "", 7, new Color(DH.Ink.r, DH.Ink.g, DH.Ink.b, 0.55f), TextAlignmentOptions.Right);
            _controls.rectTransform.Place(new Vector2(1, 0), new Vector2(1, 0), new Vector2(-12, 10), new Vector2(420, 12));
            RefreshControls();

            // tutorial prompt card
            var card = UIKit.Rect("Prompt", root);
            card.Place(new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -34), new Vector2(380, 54));
            _promptGroup = card.gameObject.AddComponent<CanvasGroup>();
            var cbg = UIKit.Image("Bg", card, new Color(0.07f, 0.04f, 0.14f, 0.9f)); cbg.rectTransform.Stretch();
            _promptLine1 = UIKit.Image("Top", card, DH.Cyan);
            _promptLine1.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0.5f, 1), Vector2.zero, new Vector2(380, 2));
            _promptLine2 = UIKit.Image("Bottom", card, DH.Cyan);
            _promptLine2.rectTransform.Place(new Vector2(0.5f, 0), new Vector2(0.5f, 0), Vector2.zero, new Vector2(380, 2));
            var keycap = UIKit.Image("Key", card, DH.Hex("5a4e80"));
            keycap.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(10, -10), new Vector2(54, 26));
            var keyIn = UIKit.Image("KeyIn", keycap.rectTransform, DH.Hex("2c2640"));
            keyIn.rectTransform.Stretch(1);
            _promptKey = UIKit.Text("KeyText", keycap.rectTransform, "SPACE", 7, Color.white, TextAlignmentOptions.Center);
            _promptKey.rectTransform.Stretch();
            _promptTitle = UIKit.Text("Title", card, "", 10, DH.Amber);
            _promptTitle.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(74, -10), new Vector2(296, 14));
            _promptBody = UIKit.Text("Body", card, "", 7, DH.Ink);
            _promptBody.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(74, -27), new Vector2(296, 26));
            _promptGroup.alpha = 0;

            _countdown = UIKit.Text("Countdown", root, "", 32, DH.Cyan, TextAlignmentOptions.Center);
            _countdown.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 10), new Vector2(400, 48));
            _countdown.outlineWidth = 0.2f; _countdown.outlineColor = new Color32(18, 10, 26, 255);
            _countSub = UIKit.Text("CountSub", root, "", 8, DH.Amber, TextAlignmentOptions.Center);
            _countSub.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -24), new Vector2(560, 14));
        }

        public void RefreshControls()
        {
            _controls.text = $"{RunnerInput.KeyLabel(_input.Jump)} JUMP   {RunnerInput.KeyLabel(_input.Slide)} SLIDE   " +
                             $"{RunnerInput.KeyLabel(_input.Attack)} ATTACK   {RunnerInput.KeyLabel(_input.Ability)} ABILITY";
            _abilityKey.text = RunnerInput.KeyLabel(_input.Ability);
        }

        public void Flash(float t) => _flashT = Mathf.Max(_flashT, t);

        public void SetCountdown(float remaining, bool firstRun, LevelData L)
        {
            if (remaining <= 0) { _countdown.text = ""; _countSub.text = ""; return; }
            int c = Mathf.CeilToInt(remaining - 0.2f);
            _countdown.text = c > 0 ? c.ToString() : "RUN!";
            _countdown.color = c > 0 ? DH.Cyan : DH.Pink;
            _countSub.text = firstRun && remaining > 1 ? $"{L.name}  -  {L.subtitle.ToUpperInvariant()}" : "";
        }

        public void SetVisible(bool v) => _canvas.enabled = v;

        void Update()
        {
            if (_sim == null) return;
            var p = _sim.p;
            for (int i = 0; i < _hearts.Count; i++) _hearts[i].sprite = i < p.hp ? _heartFull : _heartEmpty;
            _bytes.text = _sim.stats.bytes.ToString("000");
            _shard.color = new Color(1, 1, 1, _sim.stats.shard ? 1 : 0.3f);

            float prog = Mathf.Clamp01(p.x / _sim.L.exitX);
            _progressFill.rectTransform.sizeDelta = new Vector2(160 * prog, 6);
            _progressFill.color = Alarm ? DH.Red : DH.Cyan;
            foreach (var (img, e) in _cpTicks) img.color = e.alive ? DH.Dim : DH.Green;
            _timer.text = DH.FormatTime(_sim.time);
            _pct.text = Mathf.RoundToInt(prog * 100) + "%";

            bool ready = _sim.AbilityReady;
            float k = _ch.abilityCooldown > 0 ? p.abilityCD / _ch.abilityCooldown : (ready ? 0 : 1);
            _abilityCover.fillAmount = ready ? 0 : Mathf.Clamp01(k);
            _abilityFrame.enabled = ready;
            _abilityName.color = ready ? _ch.color : DH.Dim;

            UpdatePrompt();

            // overlays
            float t = Time.unscaledTime;
            _alarm.color = new Color(1, 0.18f, 0.33f, Alarm && _sim.state == SimState.Running ? 0.25f + 0.2f * Mathf.Sin(t * 6) : 0);
            _glitch.color = new Color(0.5f, 1, 0.4f, GlitchT > 0 ? 0.18f : 0);
            if (_flashT > 0) _flashT -= Time.unscaledDeltaTime;
            _flash.color = new Color(1, 1, 1, Mathf.Max(0, _flashT * 2));
        }

        void UpdatePrompt()
        {
            var p = _sim.p;
            PromptData pr = null;
            foreach (var q in _sim.L.prompts) if (p.x >= q.x - 9 && p.x <= q.x + 4) pr = q;
            if (pr == null) { _promptGroup.alpha = 0; return; }
            bool danger = pr.key == "!";
            _promptTitle.text = pr.ability ? _ch.abilityName : pr.title;
            _promptBody.text = pr.ability ? _ch.abilityDescription : pr.body;
            _promptKey.text = pr.ability ? RunnerInput.KeyLabel(_input.Ability) : KeyFor(pr.key);
            _promptTitle.color = danger ? DH.Red : DH.Amber;
            _promptLine1.color = _promptLine2.color = danger ? DH.Red : DH.Cyan;
            _promptGroup.alpha = Mathf.Clamp01(Mathf.Min((p.x - (pr.x - 9)) * 2f, (pr.x + 4 - p.x) * 1.5f));
        }

        string KeyFor(string key) => key switch
        {
            "SPACE" => RunnerInput.KeyLabel(_input.Jump),
            "S" => RunnerInput.KeyLabel(_input.Slide),
            "LMB" => RunnerInput.KeyLabel(_input.Attack),
            "W" => RunnerInput.KeyLabel(_input.Ability),
            _ => key,
        };

        // --- generated overlay textures --------------------------------------
        static Sprite MakeVignette()
        {
            const int n = 64;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float dx = (x - n / 2f + 0.5f) / (n / 2f), dy = (y - n / 2f + 0.5f) / (n / 2f);
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    tex.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01((d - 0.55f) / 0.6f)));
                }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100);
        }

        static Sprite MakeScanlines()
        {
            var tex = new Texture2D(1, 3, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Repeat };
            tex.SetPixel(0, 0, new Color(1, 1, 1, 1));
            tex.SetPixel(0, 1, new Color(1, 1, 1, 0.2f));
            tex.SetPixel(0, 2, new Color(1, 1, 1, 0.2f));
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 1, 3), new Vector2(0.5f, 0.5f), 100, 0, SpriteMeshType.FullRect);
        }
    }
}
