using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace DataHeist.EditorTools
{
    /// <summary>
    /// Play-mode smoke test of the whole front end, following the player: first launch plays the
    /// story, then the home page, options (sliders must change the volume), shop (a purchase must
    /// work), the old Options scene redirect, character select and Level 1. Fails on any console
    /// error and checks that one music track and one audio listener are active on each screen.
    /// The player's save is snapshotted first and restored afterwards.
    /// Menu: Data Heist > Debug > Smoke Test. Headless:
    ///   Unity -batchmode -projectPath . -executeMethod DataHeist.EditorTools.SmokeTest.RunBatch -logFile -
    /// (exits with code 0 on success, 1 on failure).
    /// </summary>
    public static class SmokeTest
    {
        const string PrefKey = "DataHeist.SmokeTest.Running";
        const string BatchKey = "DataHeist.SmokeTest.Batch";

        static readonly List<string> Errors = new List<string>();
        static IEnumerator<float> _steps;
        static double _wakeAt;
        static string _saveBefore;

        [MenuItem("Data Heist/Debug/Smoke Test", priority = 42)]
        public static void Run() => Start(false);

        public static void RunBatch() => Start(true);

        static void Start(bool batch)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            SessionState.SetBool(PrefKey, true);
            SessionState.SetBool(BatchKey, batch);
            EditorSceneManager.OpenScene(SceneBy(DH.SceneMainMenu));
            EditorApplication.isPlaying = true;
        }

        // survives the domain reload that happens when entering play mode
        [InitializeOnLoadMethod]
        static void Resume()
        {
            if (!SessionState.GetBool(PrefKey, false)) return;
            EditorApplication.playModeStateChanged -= OnPlayMode;
            EditorApplication.playModeStateChanged += OnPlayMode;
        }

        static void OnPlayMode(PlayModeStateChange s)
        {
            if (s != PlayModeStateChange.EnteredPlayMode) return;
            Errors.Clear();
            // PlayerPrefs are shared with normal play: snapshot the save and put it back afterwards
            _saveBefore = PlayerPrefs.GetString(SaveSystem.PrefsKey, "");
            Application.logMessageReceived += OnLog;
            _steps = Steps();
            _wakeAt = 0;
            EditorApplication.update += Tick;
        }

        static void OnLog(string msg, string stack, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
                Errors.Add($"{type}: {msg}\n{stack}");
        }

        static void Tick()
        {
            if (EditorApplication.timeSinceStartup < _wakeAt) return;
            bool more;
            try { more = _steps.MoveNext(); }
            catch (Exception e) { Errors.Add("Smoke test crashed: " + e); more = false; }
            if (more) _wakeAt = EditorApplication.timeSinceStartup + _steps.Current;
            else Finish();
        }

        static void Check(bool ok, string what)
        {
            if (ok) Debug.Log("[SmokeTest] OK   " + what);
            else Errors.Add("[SmokeTest] FAIL " + what);
        }

        // ------------------------------------------------------------------ screenshots
        // Overlay canvases are invisible to Camera.Render, so for a capture they are switched to
        // camera space for one frame. Shots land in Logs/Screens (gitignored).
        static readonly List<(Canvas c, int order)> Switched = new List<(Canvas, int)>();

        static void PrepareCapture()
        {
            Switched.Clear();
            var cam = Camera.main;
            if (cam != null) cam.aspect = 1280f / 720f;   // the batch window is 4:3; frame shots like a 16:9 screen
            foreach (var c in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            {
                if (!c.isRootCanvas || c.renderMode != RenderMode.ScreenSpaceOverlay) continue;
                Switched.Add((c, c.sortingOrder));
                c.renderMode = RenderMode.ScreenSpaceCamera;
                c.worldCamera = cam;
                c.planeDistance = 1;
                c.sortingOrder += 1000;
            }
        }

        static void Capture(string name)
        {
            var cam = Camera.main;
            if (cam != null)
            {
                var rt = new RenderTexture(1280, 720, 24);
                var prev = cam.targetTexture;
                cam.targetTexture = rt;
                Canvas.ForceUpdateCanvases();
                cam.Render();
                RenderTexture.active = rt;
                var tex = new Texture2D(1280, 720, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
                tex.Apply();
                RenderTexture.active = null;
                cam.targetTexture = prev;
                cam.ResetAspect();
                System.IO.Directory.CreateDirectory("Logs/Screens");
                System.IO.File.WriteAllBytes($"Logs/Screens/{name}.png", tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                rt.Release();
            }
            foreach (var (c, order) in Switched)
            {
                if (c == null) continue;
                c.renderMode = RenderMode.ScreenSpaceOverlay;
                c.sortingOrder = order;
            }
            Switched.Clear();
        }

        static IEnumerable<float> Shot(string name)
        {
            PrepareCapture();
            yield return 0.15f;
            Capture(name);
        }

        static IEnumerator<float> Steps()
        {
            // --- first launch: the home page hands over to the story intro
            SaveSystem.Data.storySeen = false;
            SaveSystem.Data.bytes = 1000;
            if (SaveSystem.Data.unlocked.Contains("jax")) SaveSystem.Data.unlocked.Remove("jax");
            SaveSystem.Save();
            typeof(MainMenuController).GetField("_started", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static).SetValue(null, false);
            AudioManager.I.StopMusic();
            SceneManager.LoadScene(DH.SceneMainMenu);
            yield return 2f;
            var title = Object.FindAnyObjectByType<MainMenuController>();
            Check(title != null && GameObject.Find("PressStart") != null, "launch shows PRESS ANY KEY on the title");
            AudioAudit("title before any key", 0);
            foreach (var w in Shot("00_press_start")) yield return w;
            title?.SendMessage("PressStart");
            yield return 2f;
            Check(SceneManager.GetActiveScene().name == DH.SceneStory, "first key press on first launch opens the story");
            var story = Object.FindAnyObjectByType<StoryController>();
            Check(story != null, "story intro running");
            AudioAudit("story", 1);
            if (story != null)
            {
                Check(story.ShotCount == 6, $"story has {story.ShotCount} shots");
                for (int i = 0; i < story.ShotCount; i++)
                {
                    story.JumpTo(i);
                    yield return i == 0 ? 0.5f : 3.2f;
                    foreach (var w in Shot($"story_{i + 1}")) yield return w;
                }
                story.Next();               // finish the caption
                story.Next();               // last shot -> title card
                yield return 1.2f;
                foreach (var w in Shot("story_7_titlecard")) yield return w;
                yield return 4f;
                Check(SaveSystem.Data.storySeen, "story marked as seen");
            }

            // --- home page
            Check(SceneManager.GetActiveScene().name == DH.SceneMainMenu, "story ends on the home page");
            yield return 0.5f;
            foreach (var w in Shot("01_title")) yield return w;
            Check(Object.FindAnyObjectByType<MainMenuController>() != null, "home page running in " + DH.SceneMainMenu);
            var demo = Object.FindAnyObjectByType<DemoPlayer>();
            Check(demo != null && demo.Sim != null && demo.Sim.p.x > 3f, $"attract demo is playing Level 1 (x = {(demo != null && demo.Sim != null ? demo.Sim.p.x : 0):F1})");
            Check(demo != null && demo.Sim.stats.hits == 0, "demo run replays cleanly (no hits)");
            AudioAudit("home page", 1);
            Check(GameObject.Find("PressStart") == null, "home page goes straight to the menu after the first press");

            // --- options: sliders must really change the volume
            var settings = Object.FindAnyObjectByType<SettingsPanel>(FindObjectsInactive.Include);
            Check(settings != null, "options panel built");
            if (settings != null)
            {
                settings.Open();
                yield return 0.5f;
                Check(settings.IsOpen, "options opens");
                var sliders = settings.GetComponentsInChildren<UnityEngine.UI.Slider>(true);
                var master = System.Array.Find(sliders, x => x.transform.parent.name == "MASTER");
                var music = System.Array.Find(sliders, x => x.transform.parent.name == "MUSIC");
                float oldMaster = SaveSystem.Data.masterVolume, oldMusic = SaveSystem.Data.musicVolume;
                if (master != null) master.value = 0.3f;
                if (music != null) music.value = 0.2f;
                yield return 0.2f;
                Check(Mathf.Approximately(AudioListener.volume, 0.3f) && Mathf.Approximately(SaveSystem.Data.masterVolume, 0.3f),
                    $"MASTER slider sets the volume (listener {AudioListener.volume:F2})");
                var musicSrc = MusicSource();
                Check(musicSrc != null && Mathf.Abs(musicSrc.volume - 0.2f * 0.55f) < 0.01f,
                    $"MUSIC slider sets the music volume ({(musicSrc != null ? musicSrc.volume : -1):F3})");
                foreach (var w in Shot("02_options_audio")) yield return w;
                if (master != null) master.value = oldMaster;
                if (music != null) music.value = oldMusic;
                settings.SendMessage("ShowTab", 1);
                foreach (var w in Shot("03_options_video")) yield return w;
                settings.SendMessage("ShowTab", 2);
                foreach (var w in Shot("04_options_controls")) yield return w;
                settings.Close();
                yield return 0.3f;
                Check(!settings.IsOpen, "options closes");
            }

            // --- shop: a purchase must spend bytes and unlock the runner
            var shop = Object.FindAnyObjectByType<ShopPanel>(FindObjectsInactive.Include);
            Check(shop != null, "shop panel built");
            if (shop != null)
            {
                shop.Open();
                yield return 0.5f;
                Check(shop.IsOpen, "shop opens");
                foreach (var w in Shot("05_shop")) yield return w;
                var jax = CharacterDefinition.Get("jax");
                int before = SaveSystem.Data.bytes;
                var buy = shop.transform.Find("Box/Inner/Card_jax/Action")?.GetComponent<UnityEngine.UI.Button>();
                Check(buy != null, "shop has a recruit button for Jax");
                buy?.onClick.Invoke();
                yield return 0.3f;
                Check(SaveSystem.IsUnlocked(jax) && SaveSystem.Data.bytes == before - jax.unlockCost,
                    $"buying Jax spends {jax.unlockCost} bytes ({before} -> {SaveSystem.Data.bytes}) and unlocks him");
                Check(GameSession.SelectedCharacter == "jax", "a new recruit is equipped");
                foreach (var w in Shot("06_shop_after_purchase")) yield return w;
                shop.Close();
                yield return 0.3f;
            }

            // --- the team's old Options scene now opens the new options
            SceneManager.LoadScene("OptionsScreenScene");
            yield return 1.5f;
            var rerouted = Object.FindAnyObjectByType<SettingsPanel>(FindObjectsInactive.Include);
            Check(SceneManager.GetActiveScene().name == DH.SceneMainMenu && rerouted != null && rerouted.IsOpen,
                "old OptionsScreenScene opens the new options panel");

            // --- play
            SceneFader.Load(DH.SceneCharacterSelect);
            yield return 2f;
            Check(SceneManager.GetActiveScene().name == DH.SceneCharacterSelect, "character select loads");
            Check(Object.FindAnyObjectByType<CharacterSelectController>() != null, "character select running");
            AudioAudit("character select", 1);
            foreach (var w in Shot("07_character_select")) yield return w;

            SceneFader.Load(DH.SceneLevel01);
            yield return 2.2f;
            AudioAudit("level 1 countdown", 0);
            yield return 3.8f;
            var level = Object.FindAnyObjectByType<LevelController>();
            Check(level != null && level.Sim != null, "level 1 running");
            if (level != null && level.Sim != null) Check(level.Sim.p.x > 5f, $"runner is moving (x = {level.Sim.p.x:F1})");
            AudioAudit("level 1", 1);
            Check(AudioManager.I.CurrentMusic == "music_level01", "level music starts on GO");
            foreach (var w in Shot("08_level01")) yield return w;
        }

        static AudioSource MusicSource()
        {
            foreach (var a in Object.FindObjectsByType<AudioSource>(FindObjectsSortMode.None))
                if (a.loop) return a;
            return null;
        }

        /// <summary>Checks how many music tracks play (never more than one) and that there is one listener.</summary>
        static void AudioAudit(string where, int expectedMusic)
        {
            int music = 0, other = 0;
            var names = new List<string>();
            foreach (var a in Object.FindObjectsByType<AudioSource>(FindObjectsSortMode.None))
            {
                if (a.loop && a.isPlaying && a.volume > 0.001f) { music++; names.Add($"{a.gameObject.name}:{(a.clip != null ? a.clip.name : "-")}"); }
                else if (a.isPlaying) other++;
            }
            int listeners = 0;
            foreach (var l in Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None))
                if (l.enabled && l.gameObject.activeInHierarchy) listeners++;
            Check(music == expectedMusic, $"{where}: {music} music track(s) playing, expected {expectedMusic} [{string.Join(", ", names)}]");
            Check(listeners == 1, $"{where}: {listeners} audio listener(s)");
        }

        static void Finish()
        {
            EditorApplication.update -= Tick;
            Application.logMessageReceived -= OnLog;
            if (string.IsNullOrEmpty(_saveBefore)) PlayerPrefs.DeleteKey(SaveSystem.PrefsKey);
            else PlayerPrefs.SetString(SaveSystem.PrefsKey, _saveBefore);
            PlayerPrefs.Save();
            SaveSystem.Reload();
            GameSettings.ApplyAudio();
            SessionState.SetBool(PrefKey, false);
            bool batch = SessionState.GetBool(BatchKey, false);
            if (Errors.Count == 0) Debug.Log("[SmokeTest] PASSED");
            else Debug.LogError($"[SmokeTest] FAILED with {Errors.Count} problem(s):\n" + string.Join("\n", Errors));
            int code = Errors.Count == 0 ? 0 : 1;
            EditorApplication.isPlaying = false;
            if (batch) EditorApplication.Exit(code);
        }

        static string SceneBy(string name)
        {
            foreach (var s in EditorBuildSettings.scenes)
                if (System.IO.Path.GetFileNameWithoutExtension(s.path) == name) return s.path;
            throw new Exception($"Scene {name} is not in Build Settings. Run Data Heist > Setup Project.");
        }
    }
}
