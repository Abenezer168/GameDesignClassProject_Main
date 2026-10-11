using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DataHeist
{
    /// <summary>
    /// Scrolling credits (CreditsScene). Team credits come from Resources/DataHeist/Credits/team.txt
    /// (editable, placeholders until the team fills them in); third-party audio credits are appended
    /// from Audio/CREDITS.txt. Hold ENTER / pad A to speed up; ESC or BACK returns to the menu.
    /// </summary>
    public class CreditsController : MonoBehaviour
    {
        RectTransform _scroll;
        float _height, _y;

        void Start()
        {
            var camGo = Camera.main != null ? Camera.main.gameObject : new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            if (!camGo.TryGetComponent<Camera>(out var cam)) cam = camGo.AddComponent<Camera>();
            cam.orthographic = true; cam.orthographicSize = DH.OrthoSize;
            cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = DH.Night;
            if (camGo.GetComponent<AudioListener>() == null) camGo.AddComponent<AudioListener>();
            camGo.transform.position = new Vector3(0, 3.9f, -10);
            Parallax.Create(camGo.transform, 0).SetTint(new Color(0.5f, 0.45f, 0.6f), new Color(0.45f, 0.4f, 0.55f));

            var canvas = UIKit.Canvas("Credits", 10);
            var root = (RectTransform)canvas.transform;
            var shade = UIKit.Image("Shade", root, new Color(0.02f, 0.01f, 0.05f, 0.55f));
            shade.rectTransform.Stretch(-4);
            var mask = UIKit.Rect("Viewport", root);
            mask.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 6), new Vector2(560, 300));
            mask.gameObject.AddComponent<RectMask2D>();
            var text = UIKit.Text("Text", mask, Compose(), 7, DH.Ink, TextAlignmentOptions.Top);
            text.lineSpacing = 30;
            _scroll = text.rectTransform;
            _scroll.anchorMin = new Vector2(0.5f, 0.5f); _scroll.anchorMax = new Vector2(0.5f, 0.5f);
            _scroll.pivot = new Vector2(0.5f, 1);
            _scroll.sizeDelta = new Vector2(540, 2000);
            text.ForceMeshUpdate();
            _height = text.preferredHeight;
            _scroll.sizeDelta = new Vector2(540, _height + 20);
            _y = -170;
            var back = UIKit.Button("Back", root, "BACK TO MAIN MENU", () => SceneFader.Load(DH.SceneMainMenu), true);
            back.GetComponent<RectTransform>().Place(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 10), new Vector2(170, 20));
            back.GetComponentInChildren<TextMeshProUGUI>().fontSize = 6;
            // no keyboard focus on BACK: ENTER speeds the scroll up, ESC leaves
            var nav = back.navigation; nav.mode = Navigation.Mode.None; back.navigation = nav;
            var hint = UIKit.Text("Hint", root, "HOLD ENTER TO SPEED UP", 5, new Color(1, 1, 1, 0.4f), TextAlignmentOptions.Right);
            hint.rectTransform.Place(new Vector2(1, 0), new Vector2(1, 0), new Vector2(-12, 14), new Vector2(200, 8));
            ScreenFX.Add();
            AudioManager.I.Music("music_credits");
            SceneFader.FadeIn(0.8f);
        }

        /// <summary>The full credits text: the team file, then every third-party asset credit.</summary>
        public static string Compose()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("<size=16><color=#2ff3ff>THE DATA HEIST</color></size>");
            sb.AppendLine("<color=#8a7fb0>A GAME DESIGN AND DEVELOPMENT CLASS PROJECT</color>\n\n");
            var team = Resources.Load<TextAsset>(DH.Root + "Credits/team");
            if (team != null)
                foreach (var raw in team.text.Split('\n'))
                {
                    var line = raw.Trim();
                    if (line.Length == 0 || (line.StartsWith("#") && !line.StartsWith("## "))) continue;
                    if (line.StartsWith("## ")) sb.AppendLine("\n<color=#ff3fa4>" + line.Substring(3) + "</color>");
                    else sb.AppendLine(line);
                }
            sb.AppendLine("\n<color=#ff3fa4>THIRD-PARTY MUSIC AND SOUND (CC0)</color>");
            var audio = Resources.Load<TextAsset>(DH.Root + "Audio/CREDITS");
            if (audio != null)
                foreach (var raw in audio.text.Split('\n'))
                    if (raw.Trim().Length > 0) sb.AppendLine("<size=5>" + raw.Trim().Replace("Sound effects: ", "").Replace("Music: ", "") + "</size>");
            sb.AppendLine("\n<color=#ff3fa4>FONT</color>\nPress Start 2P by CodeMan38 (SIL Open Font License)");
            sb.AppendLine("\n<color=#ff3fa4>ENGINE</color>\nMade with Unity");
            sb.AppendLine("\n\n\n<color=#ffd23a>THANK YOU FOR PLAYING</color>\n<color=#8a7fb0>The city is free.</color>");
            return sb.ToString();
        }

        void Update()
        {
            var kb = UnityEngine.InputSystem.Keyboard.current;
            var pad = UnityEngine.InputSystem.Gamepad.current;
            bool fast = (kb != null && (kb.enterKey.isPressed || kb.spaceKey.isPressed)) || (pad != null && pad.buttonSouth.isPressed);
            _y += Time.deltaTime * (fast ? 90f : 22f);
            if (_y > _height + 160f) _y = -170f;                    // loop
            _scroll.anchoredPosition = new Vector2(0, Mathf.Round(_y + 150f));
            if (kb != null && kb.escapeKey.wasPressedThisFrame) SceneFader.Load(DH.SceneMainMenu);
        }
    }
}
