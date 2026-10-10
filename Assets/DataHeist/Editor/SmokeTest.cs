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
    /// Play-mode smoke test of the whole front end: title menu, settings, character select and
    /// Level 1 must load and run with no errors or exceptions in the console.
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
            yield return 1.5f;
            foreach (var w in Shot("01_title")) yield return w;
            var menu = Object.FindAnyObjectByType<MainMenuController>();
            Check(menu != null, "title menu installed into " + DH.SceneMainMenu);
            var legacy = GameObject.Find("Canvas");
            Check(legacy == null || !legacy.activeInHierarchy, "legacy main menu canvas hidden");

            var settings = Object.FindAnyObjectByType<SettingsPanel>(FindObjectsInactive.Include);
            Check(settings != null, "settings panel built");
            if (settings != null)
            {
                settings.Open();
                yield return 0.5f;
                Check(settings.IsOpen, "settings opens");
                foreach (var w in Shot("02_settings_audio")) yield return w;
                settings.SendMessage("ShowTab", 1);
                foreach (var w in Shot("03_settings_video")) yield return w;
                settings.SendMessage("ShowTab", 2);
                foreach (var w in Shot("04_settings_controls")) yield return w;
                settings.Close();
                yield return 0.3f;
                Check(!settings.IsOpen, "settings closes");
            }

            SceneFader.Load(DH.SceneCharacterSelect);
            yield return 2f;
            Check(SceneManager.GetActiveScene().name == DH.SceneCharacterSelect, "character select loads");
            Check(Object.FindAnyObjectByType<CharacterSelectController>() != null, "character select running");
            foreach (var w in Shot("05_character_select")) yield return w;

            SceneFader.Load(DH.SceneLevel01);
            yield return 6f;
            var level = Object.FindAnyObjectByType<LevelController>();
            Check(level != null && level.Sim != null, "level 1 running");
            if (level != null && level.Sim != null) Check(level.Sim.p.x > 5f, $"runner is moving (x = {level.Sim.p.x:F1})");
            foreach (var w in Shot("06_level01")) yield return w;
        }

        static void Finish()
        {
            EditorApplication.update -= Tick;
            Application.logMessageReceived -= OnLog;
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
