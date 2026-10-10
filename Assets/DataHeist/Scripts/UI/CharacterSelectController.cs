using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace DataHeist
{
    /// <summary>
    /// "Choose your runner" screen: browse the four runners, see stats and ability,
    /// recruit locked ones with Data Bytes, then launch Level 1.
    /// Put this on one GameObject in an empty scene (CharacterSelectScene).
    /// </summary>
    public class CharacterSelectController : MonoBehaviour
    {
        public string backScene = DH.SceneMainMenu;

        class Card
        {
            public CharacterDefinition c;
            public Button button;
            public Image bg, sprite;
            public Outline frame;
            public SpriteAnimator anim;
            public TextMeshProUGUI label;
        }

        readonly List<Card> _cards = new List<Card>();
        int _index;
        TextMeshProUGUI _wallet, _name, _title, _bio, _abName, _abText, _lock, _best;
        Image _abIcon;
        readonly List<(Image fill, TextMeshProUGUI value)> _stats = new List<(Image, TextMeshProUGUI)>();
        Button _go, _back;
        MenuBackdrop _backdrop;
        // mission picker (LevelCatalog order); a level unlocks when the previous one is cleared
        readonly List<(Button button, Image bg, TextMeshProUGUI label)> _missions = new List<(Button, Image, TextMeshProUGUI)>();
        int _mission;

        void Start()
        {
            var roster = CharacterDefinition.All;
            _index = Mathf.Max(0, IndexOf(GameSession.SelectedCharacter));
            int cur = LevelCatalog.IndexOf(GameSession.CurrentLevel);
            _mission = LevelCatalog.IsUnlocked(LevelCatalog.All[cur].id) ? cur : 0;
            BuildBackdrop();
            BuildUI(roster);
            ScreenFX.Add(50, 0.55f, 0.07f);
            Refresh();
            AudioManager.I.Music("music_menu");
        }

        int IndexOf(string id)
        {
            var all = CharacterDefinition.All;
            for (int i = 0; i < all.Count; i++) if (all[i].id == id) return i;
            return 0;
        }

        // ---------------------------------------------------------------- world backdrop
        void BuildBackdrop()
        {
            _backdrop = MenuBackdrop.Create();
            _backdrop.runnerScreenX = 0.18f;
        }

        void Update()
        {
            var c = CharacterDefinition.All[_index];
            _backdrop.runnerId = c.id;
            _backdrop.runnerTint = SaveSystem.IsUnlocked(c) ? Color.white : new Color(0, 0, 0, 0.85f);

            var kb = Keyboard.current;
            if (kb == null || LoadingScreen.Active) return;
            if (kb.rightArrowKey.wasPressedThisFrame || kb.dKey.wasPressedThisFrame) Move(1);
            if (kb.leftArrowKey.wasPressedThisFrame || kb.aKey.wasPressedThisFrame) Move(-1);
            if (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame) Confirm();
            if (kb.escapeKey.wasPressedThisFrame) Back();
            if (kb.downArrowKey.wasPressedThisFrame || kb.sKey.wasPressedThisFrame) PickMission(_mission + 1);
            if (kb.upArrowKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame) PickMission(_mission - 1);
        }

        /// <summary>Selects a mission; locked ones refuse with a sound and stay unselected.</summary>
        void PickMission(int i)
        {
            if (i < 0 || i >= LevelCatalog.All.Count || i == _mission) return;
            if (!LevelCatalog.IsUnlocked(LevelCatalog.All[i].id)) { AudioManager.I.Sfx("denied", 0.5f); return; }
            _mission = i;
            AudioManager.I.Sfx("ui_move", 0.6f);
            Refresh();
        }

        void Move(int d)
        {
            int n = CharacterDefinition.All.Count;
            _index = (_index + d + n) % n;
            AudioManager.I.Sfx("ui_move", 0.6f);
            Refresh();
        }

        // ---------------------------------------------------------------- UI
        void BuildUI(IReadOnlyList<CharacterDefinition> roster)
        {
            var canvas = UIKit.Canvas("CharacterSelect", 10);
            var root = (RectTransform)canvas.transform;

            var head = UIKit.Text("Header", root, "CHOOSE YOUR RUNNER", 10, DH.Amber);
            head.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(16, -14), new Vector2(240, 14));
            _wallet = UIKit.Text("Wallet", root, "", 8, DH.Cyan, TextAlignmentOptions.Right);
            _wallet.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(236, -16), new Vector2(120, 12));

            for (int i = 0; i < roster.Count; i++)
            {
                int idx = i;
                var c = roster[i];
                var card = new Card { c = c };
                var r = UIKit.Rect("Card_" + c.id, root);
                r.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(16 + i * 86, -36), new Vector2(80, 132));
                card.bg = r.gameObject.AddComponent<Image>(); card.bg.color = DH.Panel;
                card.frame = r.gameObject.AddComponent<Outline>();
                card.frame.effectColor = c.color; card.frame.effectDistance = new Vector2(2, -2);
                card.button = r.gameObject.AddComponent<Button>();
                card.button.targetGraphic = card.bg;
                card.button.onClick.AddListener(() => { _index = idx; AudioManager.I.Sfx("ui_move", 0.6f); Refresh(); });
                card.sprite = UIKit.Image("Sprite", r, Color.white);
                card.sprite.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -4), new Vector2(72, 72));
                card.sprite.preserveAspect = true;
                card.anim = card.sprite.gameObject.AddComponent<SpriteAnimator>();
                var nm = UIKit.Text("Name", r, c.displayName.Replace(" ", "\n"), 6, c.color, TextAlignmentOptions.Center);
                nm.rectTransform.Place(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 30), new Vector2(76, 22));
                card.label = UIKit.Text("Ability", r, "", 5, DH.Ink, TextAlignmentOptions.Center);
                card.label.rectTransform.Place(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 8), new Vector2(76, 16));
                var lbg = UIKit.Image("AbilityBg", r, DH.Hex("241a3c"));
                lbg.rectTransform.Place(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 6), new Vector2(76, 18));
                lbg.transform.SetSiblingIndex(card.label.transform.GetSiblingIndex());
                _cards.Add(card);
            }

            _best = UIKit.Text("Best", root, "", 6, DH.Dim);
            _best.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(16, -230), new Vector2(340, 12));

            var mh = UIKit.Text("MissionHead", root, "MISSION", 6, DH.Amber);
            mh.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(16, -178), new Vector2(120, 10));
            for (int i = 0; i < LevelCatalog.All.Count; i++)
            {
                int idx = i;
                var r = UIKit.Rect("Mission_" + LevelCatalog.All[i].id, root);
                r.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(16 + i * 172, -190), new Vector2(166, 30));
                var bg = r.gameObject.AddComponent<Image>();
                bg.color = DH.Panel;
                var b = r.gameObject.AddComponent<Button>();
                b.targetGraphic = bg;
                var nav = b.navigation; nav.mode = Navigation.Mode.None; b.navigation = nav;
                b.onClick.AddListener(() => PickMission(idx));
                var label = UIKit.Text("Label", r, "", 6, DH.Ink);
                label.rectTransform.Stretch(6);
                label.lineSpacing = 16;
                _missions.Add((b, bg, label));
            }

            // detail panel
            var panel = UIKit.Panel("Detail", root, DH.Cyan);
            panel.rectTransform.Place(new Vector2(1, 1), new Vector2(1, 1), new Vector2(-12, -10), new Vector2(256, 300));
            var pr = panel.rectTransform;
            _name = UIKit.Text("Name", pr, "", 11, DH.Cyan);
            _name.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(14, -14), new Vector2(228, 14));
            _title = UIKit.Text("Title", pr, "", 6, DH.Dim);
            _title.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(14, -32), new Vector2(228, 10));
            _bio = UIKit.Text("Bio", pr, "", 6, DH.Ink);
            _bio.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(14, -46), new Vector2(228, 30));
            var ab = UIKit.Image("AbilityBox", pr, DH.Hex("0f0820"));
            ab.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(12, -80), new Vector2(232, 60));
            _abIcon = UIKit.Image("Icon", ab.rectTransform, Color.white);
            _abIcon.rectTransform.Place(new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(6, 0), new Vector2(32, 32));
            _abName = UIKit.Text("AbName", ab.rectTransform, "", 7, DH.Amber);
            _abName.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(46, -6), new Vector2(180, 10));
            _abText = UIKit.Text("AbText", ab.rectTransform, "", 5, DH.Ink);
            _abText.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(46, -20), new Vector2(180, 36));
            string[] labels = { "HEALTH", "SPEED", "REACH", "COOLDOWN" };
            for (int i = 0; i < labels.Length; i++)
            {
                var l = UIKit.Text(labels[i], pr, labels[i], 5, DH.Ink);
                l.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(14, -150 - i * 14), new Vector2(60, 8));
                var bar = UIKit.Image("Bar", pr, DH.Hex("241a3c"));
                bar.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(78, -150 - i * 14), new Vector2(110, 6));
                var fill = UIKit.Image("Fill", bar.rectTransform, DH.Pink);
                fill.rectTransform.Place(new Vector2(0, 0.5f), new Vector2(0, 0.5f), Vector2.zero, new Vector2(0, 6));
                var v = UIKit.Text("Value", pr, "", 5, DH.Cyan, TextAlignmentOptions.Right);
                v.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(190, -150 - i * 14), new Vector2(52, 8));
                _stats.Add((fill, v));
            }
            _lock = UIKit.Text("Lock", pr, "", 6, DH.Red);
            _lock.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(14, -210), new Vector2(228, 24));
            _go = UIKit.Button("Go", pr, "RUN", Confirm, true);
            _go.GetComponent<RectTransform>().Place(new Vector2(0, 0), new Vector2(0, 0), new Vector2(14, 14), new Vector2(150, 26));
            _back = UIKit.Button("Back", pr, "BACK", Back, false);
            _back.GetComponent<RectTransform>().Place(new Vector2(1, 0), new Vector2(1, 0), new Vector2(-14, 14), new Vector2(64, 26));

            var hint = UIKit.Text("Hint", root, "< > RUNNER    UP/DOWN MISSION    ENTER RUN    ESC BACK", 6, new Color(1, 1, 1, 0.5f));
            hint.rectTransform.Place(new Vector2(0, 0), new Vector2(0, 0), new Vector2(16, 10), new Vector2(340, 10));
            UIKit.Select(_go);
        }

        void Refresh()
        {
            var data = SaveSystem.Data;
            _wallet.text = $"{data.bytes} BYTES";
            for (int i = 0; i < _cards.Count; i++)
            {
                var card = _cards[i];
                bool un = SaveSystem.IsUnlocked(card.c);
                bool sel = i == _index;
                card.frame.enabled = sel;
                card.anim.Play(SpriteLibrary.CharacterAnim(card.c.id, sel ? "run" : "idle"), sel ? 12 : 4);
                card.sprite.color = un ? Color.white : new Color(0.05f, 0.03f, 0.1f, 0.9f);
                card.label.text = un ? card.c.abilityName : "LOCKED";
                card.label.color = un ? DH.Ink : DH.Red;
            }
            var c = CharacterDefinition.All[_index];
            bool unlocked = SaveSystem.IsUnlocked(c);
            _name.text = c.displayName; _name.color = c.color;
            _title.text = c.title.ToUpperInvariant();
            _bio.text = c.bio;
            _abIcon.sprite = SpriteLibrary.Get("UI/ability_" + c.id);
            _abName.text = "W  " + c.abilityName;
            _abText.text = c.abilityDescription;
            SetStat(0, c.maxHealth / 4f, c.maxHealth.ToString());
            SetStat(1, (c.speedMultiplier - 0.8f) / 0.3f, c.speedMultiplier.ToString("0.00") + "x");
            SetStat(2, (c.attackReach - 1.4f) / 1f, c.attackReach.ToString("0.0") + "m");
            SetStat(3, c.abilityCooldown > 0 ? (11f - c.abilityCooldown) / 11f : 1f, c.abilityCooldown > 0 ? c.abilityCooldown + "s" : "NONE");

            if (unlocked)
            {
                _go.Label("RUN AS " + c.displayName.Split(' ')[0]);
                _go.interactable = true;
                _lock.text = "";
            }
            else if (c.unlock == UnlockType.Bytes)
            {
                _go.Label($"RECRUIT  {c.unlockCost}");
                _go.interactable = data.bytes >= c.unlockCost;
                _lock.text = $"Spend {c.unlockCost} Data Bytes to recruit. You have {data.bytes}.";
            }
            else
            {
                bool ready = SaveSystem.CanUnlock(c);
                _go.Label(ready ? "RECRUIT" : c.UnlockLabel);
                _go.interactable = ready;
                _lock.text = ready ? "" : "Clear Level 1 to recruit.";
            }
            var lvl = LevelCatalog.All[_mission];
            var rec = SaveSystem.GetRecord(lvl.id);
            _best.text = rec != null ? $"LEVEL {lvl.number} BEST  {rec.rank}   {rec.score} PTS   {DH.FormatTime(rec.time)}" : $"LEVEL {lvl.number}: NO RECORD YET";
            for (int i = 0; i < _missions.Count; i++)
            {
                var m = LevelCatalog.All[i];
                bool open = LevelCatalog.IsUnlocked(m.id), sel = i == _mission;
                var (_, bg, label) = _missions[i];
                bg.color = sel ? new Color(DH.Pink.r, DH.Pink.g, DH.Pink.b, 0.35f) : DH.Panel;
                label.text = open
                    ? $"<color=#{(sel ? "ffd23a" : "8a7fb0")}>LEVEL {m.number:00}</color>\n{m.name}"
                    : $"<color=#ff2d55>LEVEL {m.number:00}  LOCKED</color>\nCLEAR LEVEL {m.number - 1} FIRST";
                label.color = open ? DH.Ink : DH.Dim;
            }
        }

        void SetStat(int i, float k, string value)
        {
            _stats[i].fill.rectTransform.sizeDelta = new Vector2(110 * Mathf.Clamp01(k), 6);
            _stats[i].value.text = value;
        }

        void Confirm()
        {
            var c = CharacterDefinition.All[_index];
            if (SaveSystem.IsUnlocked(c))
            {
                GameSession.SelectedCharacter = c.id;
                var lvl = LevelCatalog.All[_mission];
                GameSession.CurrentLevel = lvl.id;
                AudioManager.I.Sfx("ui_select", 0.8f);
                AudioManager.I.StopMusic();
                LoadingScreen.Run(lvl.scene, GamePreloader.Steps(story: false, levelId: lvl.id, onlyRunner: c.id),
                    sceneLabel: "DROPPING INTO " + lvl.name);
            }
            else if (SaveSystem.TryUnlock(c))
            {
                AudioManager.I.Sfx("unlock", 1f);
                Refresh();
            }
            else AudioManager.I.Sfx("denied", 0.5f);
        }

        void Back()
        {
            AudioManager.I.Sfx("ui_back");
            Load(backScene);
        }

        static void Load(string scene) => SceneFader.Load(scene);
    }
}
