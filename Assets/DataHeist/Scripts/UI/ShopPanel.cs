using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace DataHeist
{
    /// <summary>
    /// The runner shop: every runner with portrait, stats, ability and price. Recruit with the
    /// Data Bytes banked from runs (SaveSystem.TryUnlock), or equip an owned runner for the next
    /// run (GameSession.SelectedCharacter). Purchases are saved immediately.
    /// </summary>
    public class ShopPanel : MonoBehaviour
    {
        public event Action OnClosed;
        /// <summary>Raised after a purchase or equip so the screen behind can refresh.</summary>
        public event Action OnChanged;

        class Card
        {
            public CharacterDefinition def;
            public Image frame, portrait;
            public Outline outline;
            public TextMeshProUGUI status;
            public Button action;
        }

        readonly List<Card> _cards = new List<Card>();
        TextMeshProUGUI _wallet, _message;
        float _messageT;

        public bool IsOpen => gameObject.activeSelf;

        public static ShopPanel Create(RectTransform root)
        {
            var dim = UIKit.Image("Shop", root, new Color(0.04f, 0.02f, 0.09f, 0.82f));
            dim.raycastTarget = true;
            dim.rectTransform.Stretch();
            var panel = dim.gameObject.AddComponent<ShopPanel>();
            panel.Build(dim.rectTransform);
            dim.gameObject.SetActive(false);
            return panel;
        }

        void Build(RectTransform root)
        {
            var box = UIKit.Panel("Box", root, DH.Pink);
            box.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(584, 300));
            var inner = UIKit.Rect("Inner", box.rectTransform).Stretch(14);

            var title = UIKit.Text("Title", inner, "BLACK MARKET", 14, DH.Pink);
            title.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(4, 0), new Vector2(300, 20));
            var sub = UIKit.Text("Sub", inner, "RECRUIT RUNNERS WITH THE DATA BYTES YOU STEAL", 5, DH.Dim);
            sub.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(4, -20), new Vector2(360, 10));
            _wallet = UIKit.Text("Wallet", inner, "", 9, DH.Cyan, TextAlignmentOptions.Right);
            _wallet.rectTransform.Place(new Vector2(1, 1), new Vector2(1, 1), new Vector2(-4, -2), new Vector2(220, 14));

            var roster = CharacterDefinition.All;
            const float cardW = 132, gap = 8;
            float x0 = (556 - (roster.Count * cardW + (roster.Count - 1) * gap)) / 2;
            for (int i = 0; i < roster.Count; i++)
            {
                var c = roster[i];
                var card = new Card { def = c };
                card.frame = UIKit.Image("Card_" + c.id, inner, DH.Panel);
                var r = card.frame.rectTransform;
                r.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(x0 + i * (cardW + gap), -38), new Vector2(cardW, 196));
                card.outline = card.frame.gameObject.AddComponent<Outline>();
                card.outline.effectDistance = new Vector2(1, -1);

                var glow = UIKit.Image("PortraitBg", r, new Color(c.color.r, c.color.g, c.color.b, 0.12f));
                glow.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -8), new Vector2(84, 84));
                card.portrait = UIKit.Image("Portrait", r, Color.white, SpriteLibrary.Portrait(c.id));
                card.portrait.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -10), new Vector2(80, 80));

                var name = UIKit.Text("Name", r, c.displayName, 7, c.color, TextAlignmentOptions.Center);
                name.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -96), new Vector2(124, 10));
                var role = UIKit.Text("Role", r, c.title.ToUpperInvariant(), 5, DH.Dim, TextAlignmentOptions.Center);
                role.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -108), new Vector2(124, 8));
                var ability = UIKit.Text("Ability", r, $"<color=#ffd23a>{c.abilityName}</color>\n{c.abilityDescription}", 4, DH.Ink, TextAlignmentOptions.Center);
                ability.lineSpacing = 18;
                ability.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -120), new Vector2(120, 34));
                var stats = UIKit.Text("Stats", r, $"HP {c.maxHealth}   SPD {c.speedMultiplier:0.00}x   CD {(c.abilityCooldown > 0 ? c.abilityCooldown + "s" : "-")}", 4, DH.Cyan, TextAlignmentOptions.Center);
                stats.rectTransform.Place(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 30), new Vector2(124, 8));

                card.action = UIKit.Button("Action", r, "", () => Act(card), true);
                card.action.GetComponent<RectTransform>().Place(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 8), new Vector2(116, 18));
                card.action.GetComponentInChildren<TextMeshProUGUI>().fontSize = 6;
                _cards.Add(card);
            }
            // left/right walks the cards, down reaches BACK
            var back = UIKit.Button("Back", inner, "BACK", Close, false);
            back.GetComponent<RectTransform>().Place(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 0), new Vector2(120, 20));
            for (int i = 0; i < _cards.Count; i++)
            {
                _cards[i].action.navigation = new Navigation
                {
                    mode = Navigation.Mode.Explicit,
                    selectOnLeft = _cards[(i - 1 + _cards.Count) % _cards.Count].action,
                    selectOnRight = _cards[(i + 1) % _cards.Count].action,
                    selectOnDown = back,
                };
            }
            back.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnUp = _cards[0].action };

            _message = UIKit.Text("Message", inner, "", 6, DH.Amber, TextAlignmentOptions.Center);
            _message.rectTransform.Place(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 26), new Vector2(540, 10));
        }

        public void Open()
        {
            gameObject.SetActive(true);
            Refresh();
            int equipped = _cards.FindIndex(c => c.def.id == GameSession.SelectedCharacter);
            UIKit.Select(_cards[Mathf.Max(0, equipped)].action);
            Say("");
        }

        public void Close()
        {
            gameObject.SetActive(false);
            AudioManager.I.Sfx("ui_back");
            OnClosed?.Invoke();
        }

        void Update()
        {
            var kb = Keyboard.current;
            var pad = Gamepad.current;
            if ((kb != null && kb.escapeKey.wasPressedThisFrame) || (pad != null && pad.buttonEast.wasPressedThisFrame)) Close();
            if (_messageT > 0) { _messageT -= Time.unscaledDeltaTime; if (_messageT <= 0) _message.text = ""; }
        }

        void Act(Card card)
        {
            var c = card.def;
            if (SaveSystem.IsUnlocked(c))
            {
                if (GameSession.SelectedCharacter == c.id) { Say($"{Short(c)} IS ALREADY EQUIPPED."); return; }
                GameSession.SelectedCharacter = c.id;
                Say($"{Short(c)} EQUIPPED FOR THE NEXT RUN.");
            }
            else if (SaveSystem.TryUnlock(c))
            {
                GameSession.SelectedCharacter = c.id;
                AudioManager.I.Sfx("unlock", 1f);
                Say($"{Short(c)} JOINED THE CREW AND IS EQUIPPED.");
            }
            else
            {
                AudioManager.I.Sfx("denied", 0.6f);
                if (c.unlock == UnlockType.Bytes)
                    Say($"NOT ENOUGH BYTES. YOU NEED {c.unlockCost - SaveSystem.Data.bytes} MORE. COLLECT THEM IN THE LEVELS.");
                else
                    Say($"{Short(c)} ONLY JOINS A CREW THAT HAS CLEARED LEVEL 1.");
                return;
            }
            Refresh();
            OnChanged?.Invoke();
        }

        void Refresh()
        {
            _wallet.text = $"{SaveSystem.Data.bytes} BYTES";
            foreach (var card in _cards)
            {
                var c = card.def;
                bool owned = SaveSystem.IsUnlocked(c);
                bool equipped = owned && GameSession.SelectedCharacter == c.id;
                card.outline.effectColor = equipped ? c.color : DH.PurpleLine;
                card.portrait.color = owned ? Color.white : new Color(0.12f, 0.08f, 0.2f, 1);
                string label;
                if (equipped) label = "EQUIPPED";
                else if (owned) label = "EQUIP";
                else if (c.unlock == UnlockType.Bytes) label = $"RECRUIT  {c.unlockCost}";
                else if (SaveSystem.CanUnlock(c)) label = "RECRUIT  FREE";
                else label = "CLEAR LEVEL 1";
                card.action.Label(label);
                var img = card.action.GetComponent<Image>();
                bool affordable = !owned && SaveSystem.CanUnlock(c);
                img.color = equipped ? c.color : owned || affordable ? DH.Cyan : DH.PurpleLine;
                card.action.GetComponentInChildren<TextMeshProUGUI>().color = owned || affordable ? DH.Night : DH.Dim;
            }
        }

        void Say(string text)
        {
            _message.text = text;
            _messageT = 3.5f;
        }

        static string Short(CharacterDefinition c) => c.displayName.Split(' ')[0];
    }
}
