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
        const string MuteKey = "DataHeist.SmokeTest.PrevMute";

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
            // the test plays through every screen in seconds: never let that reach the speakers
            // (master mute silences output without touching the volume values the test checks)
            SessionState.SetBool(MuteKey, EditorUtility.audioMasterMute);
            EditorUtility.audioMasterMute = true;
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
            EditorUtility.audioMasterMute = true;      // re-assert after the domain reload
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
            // a missing clip or sprite is only a warning in game, but a broken build here
            else if (msg.StartsWith("[DataHeist] Missing")) Errors.Add("Missing asset: " + msg);
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

        /// <summary>Screen point at fraction t (0..1) along a slider's bar, at its vertical middle.</summary>
        static Vector2 SliderPoint(UnityEngine.UI.Slider s, float t)
        {
            var rt = (RectTransform)s.transform;
            var c = new Vector3[4];
            rt.GetWorldCorners(c);   // bottom-left, top-left, top-right, bottom-right
            var world = Vector3.Lerp((c[0] + c[1]) * 0.5f, (c[2] + c[3]) * 0.5f, t);
            var canvas = s.GetComponentInParent<Canvas>().rootCanvas;
            var cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            return RectTransformUtility.WorldToScreenPoint(cam, world);
        }

        /// <summary>The topmost UI raycast hit at a screen point, as the EventSystem would pick for a click.</summary>
        static UnityEngine.EventSystems.RaycastResult PointerHit(Vector2 screen)
        {
            var es = UnityEngine.EventSystems.EventSystem.current;
            if (es == null) return default;
            var hits = new List<UnityEngine.EventSystems.RaycastResult>();
            es.RaycastAll(new UnityEngine.EventSystems.PointerEventData(es) { position = screen }, hits);
            return hits.Count > 0 ? hits[0] : default;
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
            Check(EditorUtility.audioMasterMute, "smoke test runs muted (nothing reaches the speakers)");

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
            Check(title != null && GameObject.Find("KeyArtSplash") != null && GameObject.Find("PressStart") != null, "launch shows the key-art splash with PRESS START");
            Check(Object.FindAnyObjectByType<DemoPlayer>() == null, "nothing heavy is built before the player presses start");
            AudioAudit("splash before any key", 0);
            foreach (var w in Shot("00_splash")) yield return w;
            title?.SendMessage("PressStart");
            yield return 0.9f;
            Check(LoadingScreen.Active, "PRESS START opens the loading screen");
            foreach (var w in Shot("00b_loading")) yield return w;
            foreach (var w in Until(() => SceneManager.GetActiveScene().name == DH.SceneStory && !LoadingScreen.Active, 10f)) yield return w;
            Check(SceneManager.GetActiveScene().name == DH.SceneStory, "first launch: loading screen continues to the story");
            var story = Object.FindAnyObjectByType<StoryController>();
            Check(story != null, "story intro running");
            AudioAudit("story", 1);
            Check(AudioManager.I.CurrentMusic == "music_story", $"story plays its own track ({AudioManager.I.CurrentMusic})");
            yield return 1f;   // crossfade
            Check(AudioManager.I.CurrentMusic == "music_story", $"story plays its own track ({AudioManager.I.CurrentMusic})");
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
            Check(AudioManager.I.CurrentMusic == "music_menu", "home page plays the menu track");
            // every sound effect the game can play must load
            int sfx = 0;
            foreach (var clip in Resources.LoadAll<AudioClip>(DH.Root + "Audio/SFX")) { sfx++; Check(clip.length > 0.005f, $"{clip.name} loads ({clip.length * 1000:F0} ms)"); }
            Check(sfx >= 28, $"{sfx} sound effects in Resources");
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

                // the mouse must reach the bars too (their images once had raycasts off, so clicks did nothing)
                var sfxSlider = System.Array.Find(sliders, x => x.transform.parent.name == "SOUND FX");
                foreach (var s in new[] { master, music, sfxSlider })
                {
                    if (s == null) { Check(false, "audio slider missing"); continue; }
                    var hit = PointerHit(SliderPoint(s, 0.5f));
                    var target = hit.gameObject != null ? UnityEngine.EventSystems.ExecuteEvents.GetEventHandler<UnityEngine.EventSystems.IPointerDownHandler>(hit.gameObject) : null;
                    Check(target == s.gameObject, $"mouse reaches the {s.transform.parent.name} slider (hit {(hit.gameObject != null ? hit.gameObject.name : "nothing")})");
                }
                if (sfxSlider != null)
                {
                    float oldSfx = SaveSystem.Data.sfxVolume;
                    var pos = SliderPoint(sfxSlider, 0.25f);
                    var ped = new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current)
                    {
                        position = pos, pressPosition = pos, button = UnityEngine.EventSystems.PointerEventData.InputButton.Left,
                        pointerPressRaycast = PointerHit(pos), pointerCurrentRaycast = PointerHit(pos),
                    };
                    UnityEngine.EventSystems.ExecuteEvents.ExecuteHierarchy(ped.pointerPressRaycast.gameObject, ped, UnityEngine.EventSystems.ExecuteEvents.pointerDownHandler);
                    yield return 0.1f;
                    Check(Mathf.Abs(SaveSystem.Data.sfxVolume - 0.25f) < 0.05f, $"clicking a quarter along SOUND FX sets it to {SaveSystem.Data.sfxVolume:F2}");
                    sfxSlider.value = oldSfx;
                }
                settings.SendMessage("ShowTab", 1);
                foreach (var w in Shot("03_options_video")) yield return w;
                settings.SendMessage("ShowTab", 2);
                foreach (var w in Shot("04_options_controls")) yield return w;
                settings.SendMessage("ShowTab", 3);
                foreach (var w in Shot("04b_options_credits")) yield return w;
                settings.SendMessage("ShowTab", 3);
                foreach (var w in Shot("04b_options_credits")) yield return w;
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

            // --- returning player: splash -> loading -> home page built under the loading screen
            typeof(MainMenuController).GetField("_started", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static).SetValue(null, false);
            AudioManager.I.StopMusic();
            SceneManager.LoadScene(DH.SceneMainMenu);
            yield return 1.5f;
            Object.FindAnyObjectByType<MainMenuController>()?.SendMessage("PressStart");
            yield return 1.2f;
            foreach (var w in Shot("00c_loading_home")) yield return w;
            foreach (var w in Until(() => !LoadingScreen.Active, 10f)) yield return w;
            yield return 0.8f;
            Check(Object.FindAnyObjectByType<DemoPlayer>() != null && GameObject.Find("KeyArtSplash") == null,
                "returning player: loading screen builds the home page and removes the splash");
            AudioAudit("home page after loading", 1);

            // --- play
            SceneFader.Load(DH.SceneCharacterSelect);
            yield return 2.5f;
            Check(SceneManager.GetActiveScene().name == DH.SceneCharacterSelect, "character select loads");
            Check(Object.FindAnyObjectByType<CharacterSelectController>() != null, "character select running");
            AudioAudit("character select", 1);
            foreach (var w in Shot("07_character_select")) yield return w;

            Object.FindAnyObjectByType<CharacterSelectController>()?.SendMessage("Confirm");   // RUN AS ...
            yield return 1.0f;
            Check(LoadingScreen.Active, "starting a run shows the loading screen");
            foreach (var w in Shot("07b_loading_level")) yield return w;
            foreach (var w in Until(() => Object.FindAnyObjectByType<LevelController>() != null, 10f)) yield return w;
            yield return 1.0f;
            AudioAudit("level 1 countdown", 0);
            yield return 4.5f;
            var level = Object.FindAnyObjectByType<LevelController>();
            Check(level != null && level.Sim != null, "level 1 running");
            if (level != null && level.Sim != null) Check(level.Sim.p.x > 5f, $"runner is moving (x = {level.Sim.p.x:F1})");
            AudioAudit("level 1", 1);
            Check(AudioManager.I.CurrentMusic == "music_level01", "level music starts on GO");
            foreach (var w in Shot("08_level01")) yield return w;

            // --- finish Level 1 -> results -> NEXT LEVEL -> Level 2
            if (level != null && level.Sim != null)
            {
                var sim = level.Sim;
                Place(sim, sim.L.exitX - 1f);
                yield return 3f;
                var next = GameObject.Find("NEXT LEVEL")?.GetComponent<UnityEngine.UI.Button>();
                Check(next != null && next.gameObject.activeInHierarchy, "Level 1 results offer NEXT LEVEL");
                Check(SaveSystem.Data.cleared.Contains("level01") && LevelCatalog.IsUnlocked("level02"), "clearing Level 1 unlocks Level 2");
                foreach (var w in Shot("09_results_next_level")) yield return w;
                next?.onClick.Invoke();
                yield return 1.0f;
                Check(LoadingScreen.Active, "NEXT LEVEL goes through the loading screen");
                foreach (var w in Until(() => SceneManager.GetActiveScene().name == DH.SceneLevel02, 10f)) yield return w;
                yield return 5f;
                var l2 = Object.FindAnyObjectByType<LevelController>();
                Check(l2 != null && l2.levelId == "level02" && l2.Sim != null && l2.Sim.p.x > 5f, "Level 2 loads and runs");
                Check(AudioManager.I.CurrentMusic == "music_level02", $"Level 2 plays its own track ({AudioManager.I.CurrentMusic})");
                if (l2 != null && l2.Sim != null)
                {
                    var s2 = l2.Sim;
                    Place(s2, 88f);          // live wires at 94
                    yield return 0.5f;
                    foreach (var w in Shot("10_level02_live_wires")) yield return w;
                    Place(s2, 232f);         // falling sign at 244
                    yield return 0.75f;
                    foreach (var w in Shot("11_level02_falling_sign")) yield return w;
                    Place(s2, 302f);         // sniper at 318
                    yield return 0.3f;
                    Check(System.Array.Exists(s2.ents, e => e.t == "sniper" && e.st >= 1), "sniper aims its laser as the runner closes in");
                    foreach (var w in Shot("12_level02_sniper")) yield return w;
                }
            }

            // --- Level 2 -> results -> NEXT LEVEL -> Level 3
            var l2end = Object.FindAnyObjectByType<LevelController>();
            if (l2end != null && l2end.Sim != null && l2end.levelId == "level02")
            {
                Place(l2end.Sim, l2end.Sim.L.exitX - 1f);
                yield return 3f;
                var next3 = GameObject.Find("NEXT LEVEL")?.GetComponent<UnityEngine.UI.Button>();
                Check(next3 != null && next3.gameObject.activeInHierarchy && LevelCatalog.IsUnlocked("level03"), "Level 2 results offer NEXT LEVEL and unlock Level 3");
                next3?.onClick.Invoke();
                foreach (var w in Until(() => SceneManager.GetActiveScene().name == DH.SceneLevel03 && !LoadingScreen.Active, 12f)) yield return w;
            }
            else SceneManager.LoadScene(DH.SceneLevel03);

            // --- Level 3: environment and hazards
            yield return 5f;
            var l3 = Object.FindAnyObjectByType<LevelController>();
            Check(l3 != null && l3.levelId == "level03" && l3.Sim != null && l3.Sim.p.x > 3f, "Level 3 loads and runs");
            Check(Object.FindAnyObjectByType<VaultAmbience>() != null, "Level 3 uses the underground vault theme");
            Check(AudioManager.I.CurrentMusic == "music_level03", $"Level 3 plays its own track ({AudioManager.I.CurrentMusic})");
            foreach (var w in Shot("13_level03_vault")) yield return w;
            if (l3 != null && l3.Sim != null)
            {
                var s3 = l3.Sim;
                foreach (var (x, wait, name) in new[] { (103f, 0.35f, "14_level03_turret"), (210f, 1.4f, "15_level03_laser"),
                                                        (316f, 1.6f, "16_level03_press"), (350f, 0.5f, "17_level03_machine") })
                {
                    Place(s3, x);
                    yield return wait;
                    foreach (var w in Shot(name)) yield return w;
                }
                Check(System.Array.Exists(s3.ents, e => e.t == "press" && e.st == 1), "crushers run their cycle");
                Check(System.Array.Exists(s3.ents, e => e.t == "turret" && e.st > 0), "turrets wake up and aim");

                // fragments: hand them over, then the vault door must open as the runner arrives
                foreach (var e in s3.ents) if (e.t == "fragment" && e.alive) { e.alive = false; s3.stats.fragments++; }
                Place(s3, 420f);
                yield return 1.2f;
                Check(System.Array.Exists(s3.ents, e => e.t == "vault" && e.st == 1), "the vault opens with all 3 fragments");
                foreach (var w in Shot("18_level03_vault_open")) yield return w;

                // the Guardian: the runner holds position in the arena
                Place(s3, 446f);
                yield return 2.6f;
                var boss = s3.Boss;
                Check(s3.ArenaBoss != null && s3.SpeedNow() == 0f && Mathf.Abs(s3.p.x - boss.w) < 0.05f, "the runner holds position in the Guardian's arena");
                Check(AudioManager.I.CurrentMusic == "music_boss", $"the boss fight has its own track ({AudioManager.I.CurrentMusic})");
                foreach (var w in Shot("19_level03_guardian")) yield return w;
                yield return 1.0f;
                foreach (var w in Shot("20_level03_guardian_attack")) yield return w;

                // land the final blow with a reflected shot
                boss.hp = 1;
                s3.p.inv = 10f;
                s3.bolts.Add(new SimBolt { x = boss.x - 2.5f, y = boss.y + 1.5f, vx = RunnerSim.BossReflectSpeed, kind = 2, reflected = true });
                yield return 0.6f;
                Check(boss.hp == 0, "a reflected shot damages the Guardian");
                foreach (var w in Shot("21_level03_guardian_destroyed")) yield return w;
                yield return 3.5f;
                Check(s3.ArenaBoss == null && s3.p.x > boss.w + 3f, "the arena releases the runner after the Guardian falls");
                foreach (var w in Until(() => s3.escaping, 4f)) yield return w;
                Check(s3.escaping && s3.stats.evidence, "picking up the evidence starts the lockdown escape");
                yield return 1.5f;
                Check(AudioManager.I.CurrentMusic == "music_escape", $"the escape has its own track ({AudioManager.I.CurrentMusic})");
                foreach (var w in Shot("22_level03_escape")) yield return w;

                // reach the exit: Level 3 complete, award saved, and the escape rolls into the ending
                Place(s3, s3.L.exitX - 1f);
                yield return 3f;
                Check(SaveSystem.Data.cleared.Contains("level03") && SaveSystem.Data.guardianAward, "clearing Level 3 saves completion and the Guardian of the City award");
                foreach (var w in Until(() => SceneManager.GetActiveScene().name == DH.SceneEnding && !LoadingScreen.Active, 15f)) yield return w;
                Check(SceneManager.GetActiveScene().name == DH.SceneEnding, "Level 3's escape rolls straight into the ending");
            }

            // --- the ending: six chapters (3x speed, the hero walks himself), then completion and credits
            var ending = Object.FindAnyObjectByType<EndingController>();
            if (ending != null)
            {
                EndingController.AutoWalk = true;
                Time.timeScale = 3f;
                string[] names = { "30_ending_truth", "31_ending_transform", "32_ending_walk", "33_ending_ceremony", "34_ending_statue", "35_ending_finale" };
                bool citizensReacted = false, fireworks = false;
                for (int c = 0; c < EndingController.Chapters; c++)
                {
                    foreach (var w in Until(() => ending == null || ending.Chapter >= c, 120f)) yield return w;
                    if (ending == null) break;
                    Check(ending.Chapter == c, $"ending chapter {c + 1} plays");
                    yield return c == 3 ? 7f : c == 1 ? 4.5f : 3f;
                    foreach (var w in Shot(names[c] + "_a")) yield return w;
                    yield return c == 3 ? 6f : 3f;
                    if (ending != null && ending.Chapter == c) foreach (var w in Shot(names[c] + "_b")) yield return w;
                    if (c == 2) citizensReacted = System.Array.Exists(Object.FindObjectsByType<Actor>(FindObjectsSortMode.None), a => a.Talked);
                    if (c == 5) fireworks = Object.FindAnyObjectByType<Fireworks>()?.Active == true;
                }
                Check(citizensReacted, "citizens notice and thank the hero in the walk");
                Check(fireworks, "fireworks fill the sky in the finale");
                foreach (var w in Until(() => SceneManager.GetActiveScene().name == DH.SceneCompletion, 90f)) yield return w;
                Time.timeScale = 1f;
                EndingController.AutoWalk = false;
                Check(SceneManager.GetActiveScene().name == DH.SceneCompletion, "the ending fades into the completion screen");
                Check(SaveSystem.Data.endingSeen, "the ending is saved as seen");
                yield return 2f;
                var st = CompletionController.Read();
                Check(st.award && st.levelsCleared == LevelCatalog.All.Count && st.score > 0,
                    $"completion stats: award {st.award}, levels {st.levelsCleared}/{st.levels}, score {st.score}, fragments {st.fragments}, deaths {st.deaths}");
                foreach (var w in Shot("36_completion")) yield return w;
                foreach (var label in new[] { "PLAY AGAIN", "REPLAY ENDING", "MAIN MENU", "CREDITS", "QUIT" })
                    Check(GameObject.Find(label)?.GetComponent<UnityEngine.UI.Button>() != null, $"completion screen has {label}");
                GameObject.Find("CREDITS").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
                foreach (var w in Until(() => SceneManager.GetActiveScene().name == DH.SceneCredits, 6f)) yield return w;
                yield return 2.5f;
                var credits = CreditsController.Compose();
                Check(SceneManager.GetActiveScene().name == DH.SceneCredits && Object.FindAnyObjectByType<CreditsController>() != null && credits.Contains("GAME DEVELOPMENT") && credits.Contains("Abenezer Ghiday") && credits.Contains("Kenney"),
                    "credits list the team file and every third-party credit");
                foreach (var w in Shot("37_credits")) yield return w;
                var back = System.Array.Find(Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsSortMode.None),
                    b => b.GetComponentInChildren<TMPro.TextMeshProUGUI>()?.text == "BACK TO MAIN MENU");
                Check(back != null, $"credits screen has BACK TO MAIN MENU (scene {SceneManager.GetActiveScene().name})");
                back?.onClick.Invoke();
                foreach (var w in Until(() => SceneManager.GetActiveScene().name == DH.SceneMainMenu, 6f)) yield return w;
                yield return 2f;
                Check(GameObject.Find("MissionReport") != null, "after the campaign the home page offers MISSION REPORT");
                GameObject.Find("MissionReport").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
                foreach (var w in Until(() => SceneManager.GetActiveScene().name == DH.SceneCompletion, 6f)) yield return w;
                yield return 1.5f;
                GameObject.Find("REPLAY ENDING").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
                foreach (var w in Until(() => SceneManager.GetActiveScene().name == DH.SceneEnding, 6f)) yield return w;
                yield return 2f;
                Check(Object.FindAnyObjectByType<EndingController>()?.Chapter == 0, "REPLAY ENDING plays the ending from the start");
            }
            Time.timeScale = 1f;
            EndingController.AutoWalk = false;

            // --- pure sim: missing a fragment must never soft-lock the run
            var lvl3 = LevelData.Load("level03");
            var vs = new RunnerSim(lvl3, CharacterDefinition.Get("kai"));
            vs.checkpointX = 294f;                       // already past checkpoint 1, but fragment 1 (x 160) was missed
            vs.p.x = 425f; vs.p.y = vs.GroundAt(425f); vs.p.onGround = true;
            for (int i = 0; i < 240 && vs.state == SimState.Running; i++) { vs.Step(DH.SimStep, default); vs.events.Clear(); }
            Check(vs.state == SimState.Dead, "the sealed vault door stops a runner without all fragments");
            vs.Respawn();
            Check(vs.p.x < 160f, $"retry goes back before the missed fragment (respawned at x = {vs.p.x:F0})");
        }

        /// <summary>Test helper: drop the runner on the roof at x, briefly invulnerable.</summary>
        static void Place(RunnerSim sim, float x)
        {
            sim.p.x = x;
            sim.p.y = sim.GroundAt(x);
            sim.p.vy = 0; sim.p.onGround = true; sim.p.sliding = false;
            sim.p.inv = 5f;
        }

        static IEnumerable<float> Until(System.Func<bool> done, float timeout)
        {
            for (float t = 0; t < timeout && !done(); t += 0.1f) yield return 0.1f;
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
            EditorUtility.audioMasterMute = SessionState.GetBool(MuteKey, false);
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
