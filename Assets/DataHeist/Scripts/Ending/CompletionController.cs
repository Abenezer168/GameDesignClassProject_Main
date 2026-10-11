using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DataHeist
{
    /// <summary>
    /// Final victory screen (CompletionScene): MISSION COMPLETE, the Guardian of the City award and
    /// campaign statistics read from the save, over the freed city with fireworks.
    /// Buttons: PLAY AGAIN, REPLAY ENDING, MAIN MENU, CREDITS, QUIT.
    /// </summary>
    public class CompletionController : MonoBehaviour
    {
        Button _first;

        /// <summary>Campaign totals shown on this screen (public so the smoke test can check them).</summary>
        public struct Stats
        {
            public int score, fragments, totalFragments, levelsCleared, levels, deaths;
            public float time;
            public bool award;
        }

        public static Stats Read()
        {
            var d = SaveSystem.Data;
            var s = new Stats { levels = LevelCatalog.All.Count, totalFragments = 3, deaths = d.totalDeaths, time = d.totalPlayTime, award = d.guardianAward };
            foreach (var l in LevelCatalog.All)
            {
                var r = SaveSystem.GetRecord(l.id);
                if (r != null) s.score += r.score;
                if (d.cleared.Contains(l.id)) s.levelsCleared++;
                if (l.id == "level03" && r != null) s.fragments = r.fragments;
            }
            return s;
        }

        void Start()
        {
            var camGo = Camera.main != null ? Camera.main.gameObject : new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            if (!camGo.TryGetComponent<Camera>(out var cam)) cam = camGo.AddComponent<Camera>();
            cam.orthographic = true; cam.orthographicSize = DH.OrthoSize;
            cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = DH.Night;
            if (camGo.GetComponent<AudioListener>() == null) camGo.AddComponent<AudioListener>();
            camGo.transform.position = new Vector3(0, 3.9f, -10);
            var parallax = Parallax.Create(camGo.transform, 0);
            parallax.SetRain(false);
            SR.Tiled("Ledge", transform, SpriteLibrary.Get("Tiles/rooftop_violet"), DH.SortRoofTop, new Vector3(-20, 0.06f, 0), new Vector2(40, 0.5f));
            SR.Tiled("Facade", transform, SpriteLibrary.Get("Tiles/facade_violet_0"), DH.SortFacade, new Vector3(-20, -0.44f, 0), new Vector2(40, 8));
            var hero = Actor.Spawn(GameSession.SelectedCharacter, transform, 7.2f, 0, true);
            hero.FaceRight(false);
            hero.Play("win");
            var fw = Fireworks.Create(transform, new Rect(-9f, 6f, 18f, 2.5f));
            fw.Active = true; fw.rate = 0.7f;
            BuildUI();
            ScreenFX.Add();
            AudioManager.I.Music("music_credits");
            AudioManager.I.Sfx("level_complete", 0.8f);
            SceneFader.FadeIn(0.8f);
        }

        void BuildUI()
        {
            var canvas = UIKit.Canvas("Completion", 10);
            var root = (RectTransform)canvas.transform;
            var st = Read();

            var title = UIKit.Text("Title", root, "MISSION COMPLETE", 20, DH.Cyan, TextAlignmentOptions.Center);
            title.textWrappingMode = TextWrappingModes.NoWrap;
            title.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -20), new Vector2(600, 28));
            var sub = UIKit.Text("Sub", root, "THE DATA HEIST  //  ALL SHARDS DELIVERED", 6, DH.Dim, TextAlignmentOptions.Center);
            sub.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -50), new Vector2(500, 10));

            // award
            var award = UIKit.Panel("Award", root, DH.Pink);
            award.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(-110, -68), new Vector2(200, 200));
            var ar = award.rectTransform;
            var medal = UIKit.Image("Medal", ar, st.award ? Color.white : new Color(0.25f, 0.2f, 0.35f), null);
            medal.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -16), new Vector2(64, 96));
            medal.gameObject.AddComponent<SpriteAnimator>().Play(SpriteLibrary.Anim("Ending/medal"), 5);
            var an = UIKit.Text("AwardName", ar, st.award ? "GUARDIAN OF THE CITY" : "AWARD NOT YET EARNED", 7, st.award ? DH.Amber : DH.Dim, TextAlignmentOptions.Center);
            an.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -122), new Vector2(190, 10));
            var who = UIKit.Text("Who", ar, st.award ? "AWARDED TO " + CharacterDefinition.Get(GameSession.SelectedCharacter).displayName : "COMPLETE LEVEL 3 TO EARN IT",
                5, DH.Ink, TextAlignmentOptions.Center);
            who.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -140), new Vector2(190, 10));
            var quote = UIKit.Text("Quote", ar, "\"Your bravery will never be forgotten.\"", 4, DH.Dim, TextAlignmentOptions.Center);
            quote.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -160), new Vector2(190, 10));

            // statistics
            var stats = UIKit.Panel("Stats", root, DH.Cyan);
            stats.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(110, -68), new Vector2(200, 200));
            var head = UIKit.Text("Head", stats.rectTransform, "CAMPAIGN REPORT", 7, DH.Cyan, TextAlignmentOptions.Center);
            head.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -16), new Vector2(180, 10));
            (string label, string value)[] lines =
            {
                ("FINAL SCORE", st.score.ToString("N0")),
                ("DATA FRAGMENTS", $"{st.fragments} / {st.totalFragments}"),
                ("LEVELS COMPLETED", $"{st.levelsCleared} / {st.levels}"),
                ("TOTAL DEATHS", st.deaths.ToString()),
                ("COMPLETION TIME", FormatLong(st.time)),
            };
            for (int i = 0; i < lines.Length; i++)
            {
                float y = -40 - i * 28;
                var l = UIKit.Text("Label", stats.rectTransform, lines[i].label, 6, DH.Ink);
                l.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(16, y), new Vector2(110, 10));
                var v = UIKit.Text("Value", stats.rectTransform, lines[i].value, 8, DH.Cyan, TextAlignmentOptions.Right);
                v.rectTransform.Place(new Vector2(1, 1), new Vector2(1, 1), new Vector2(-16, y + 1), new Vector2(90, 12));
                var rule = UIKit.Image("Rule", stats.rectTransform, DH.PurpleLine);
                rule.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, y - 16), new Vector2(170, 1));
            }

            // buttons
            var row = UIKit.Rect("Buttons", root);
            row.Place(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 26), new Vector2(560, 22));
            var h = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 8; h.childControlWidth = h.childControlHeight = true; h.childForceExpandWidth = true;
            _first = Btn(row, "PLAY AGAIN", PlayAgain, true);
            Btn(row, "REPLAY ENDING", () => SceneFader.Load(DH.SceneEnding));
            Btn(row, "MAIN MENU", () => SceneFader.Load(DH.SceneMainMenu));
            Btn(row, "CREDITS", () => SceneFader.Load(DH.SceneCredits));
            Btn(row, "QUIT", Quit);
            UIKit.Select(_first);
        }

        static Button Btn(Transform parent, string label, Action a, bool primary = false)
        {
            var b = UIKit.Button(label, parent, label, a, primary);
            b.GetComponentInChildren<TextMeshProUGUI>().fontSize = 6;
            return b;
        }

        static string FormatLong(float seconds)
        {
            var t = TimeSpan.FromSeconds(seconds);
            return t.TotalHours >= 1 ? $"{(int)t.TotalHours}:{t.Minutes:00}:{t.Seconds:00}" : $"{t.Minutes}:{t.Seconds:00}";
        }

        /// <summary>A fresh run of the campaign with the current save (runners, wallet and award are kept).</summary>
        static void PlayAgain()
        {
            GameSession.CurrentLevel = "level01";
            SceneFader.Load(DH.SceneCharacterSelect);
        }

        static void Quit()
        {
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }
    }
}
