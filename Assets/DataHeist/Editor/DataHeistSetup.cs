using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace DataHeist.EditorTools
{
    /// <summary>One-click project wiring for The Data Heist (menu: Data Heist).</summary>
    public static class DataHeistSetup
    {
        const string ScenesDir = "Assets/DataHeist/Scenes/";
        const string DefsDir = "Assets/DataHeist/Resources/DataHeist/CharacterDefs";
        const string ArtDir = "Assets/DataHeist/Resources/DataHeist";

        [MenuItem("Data Heist/Setup Project", priority = 0)]
        public static void SetupProject()
        {
            ReimportArt();
            CreateCharacterAssets();
            CreateMainMenuScene();
            CreateStoryScene();
            foreach (var l in LevelCatalog.All) if (l.number > 1) CreateLevelScene(l.id, l.scene);
            CreateControllerScene<EndingController>(DH.SceneEnding, "Ending");
            CreateControllerScene<CompletionController>(DH.SceneCompletion, "Completion");
            CreateControllerScene<CreditsController>(DH.SceneCredits, "Credits");
            AddScenesToBuild();
            EditorUtility.DisplayDialog("The Data Heist",
                "Setup complete.\n\n- Pixel art re-imported\n- Character definitions created\n- Home page, Story, Character Select, Level 1 and Level 2 in Build Settings\n\nOpen Assets/DataHeist/Scenes/MainMenu and press Play.",
                "Run!");
        }

        [MenuItem("Data Heist/Reimport Art", priority = 1)]
        public static void ReimportArt()
        {
            AssetDatabase.ImportAsset(ArtDir, ImportAssetOptions.ImportRecursive | ImportAssetOptions.ForceUpdate);
            Debug.Log("[DataHeist] Art re-imported with pixel settings.");
        }

        [MenuItem("Data Heist/Create Character Assets", priority = 2)]
        public static void CreateCharacterAssets()
        {
            if (!AssetDatabase.IsValidFolder(DefsDir))
            {
                Directory.CreateDirectory(DefsDir);
                AssetDatabase.Refresh();
            }
            foreach (var def in CharacterRoster.CreateDefaults())
            {
                string path = $"{DefsDir}/{def.id}.asset";
                if (AssetDatabase.LoadAssetAtPath<CharacterDefinition>(path) != null) continue;
                AssetDatabase.CreateAsset(def, path);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[DataHeist] Character definitions ready in " + DefsDir + " (tweak stats in the Inspector).");
        }

        /// <summary>
        /// Creates the MainMenu home page scene (one GameObject with MainMenuController) and retires
        /// the team's old MainMenuScreenScene, which it replaces.
        /// </summary>
        [MenuItem("Data Heist/Create Main Menu Scene", priority = 3)]
        public static void CreateMainMenuScene()
        {
            string path = ScenesDir + DH.SceneMainMenu + ".unity";
            if (!File.Exists(path))
            {
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                new GameObject("MainMenu").AddComponent<MainMenuController>();
                EditorSceneManager.SaveScene(scene, path);
                Debug.Log("[DataHeist] Created " + path);
            }
            if (File.Exists(LegacyMainMenu))
            {
                AssetDatabase.DeleteAsset(LegacyMainMenu);
                Debug.Log("[DataHeist] Deleted the old " + LegacyMainMenu + " (replaced by " + path + ").");
            }
        }

        const string LegacyMainMenu = "Assets/Scenes/MainMenuScreenScene.unity";

        /// <summary>Creates a scene holding one GameObject with the given controller (if it doesn't exist yet).</summary>
        public static void CreateControllerScene<T>(string sceneName, string objectName) where T : Component
        {
            string path = ScenesDir + sceneName + ".unity";
            if (File.Exists(path)) return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            new GameObject(objectName).AddComponent<T>();
            EditorSceneManager.SaveScene(scene, path);
            Debug.Log("[DataHeist] Created " + path);
        }

        /// <summary>Creates a level scene: one GameObject with a LevelController set to that level id.</summary>
        public static void CreateLevelScene(string levelId, string sceneName)
        {
            string path = ScenesDir + sceneName + ".unity";
            if (File.Exists(path)) { Debug.Log("[DataHeist] " + path + " already exists."); return; }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var level = new GameObject("Level").AddComponent<LevelController>();
            level.levelId = levelId;
            EditorSceneManager.SaveScene(scene, path);
            Debug.Log("[DataHeist] Created " + path);
        }

        /// <summary>Creates StoryScene (one GameObject with StoryController) if it does not exist yet.</summary>
        [MenuItem("Data Heist/Create Story Scene", priority = 4)]
        public static void CreateStoryScene()
        {
            string path = ScenesDir + DH.SceneStory + ".unity";
            if (File.Exists(path)) { Debug.Log("[DataHeist] " + path + " already exists."); return; }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            new GameObject("Story").AddComponent<StoryController>();
            EditorSceneManager.SaveScene(scene, path);
            Debug.Log("[DataHeist] Created " + path);
        }

        [MenuItem("Data Heist/Add Scenes To Build Settings", priority = 5)]
        public static void AddScenesToBuild()
        {
            var scenes = EditorBuildSettings.scenes.ToList();
            void Ensure(string path, int index = -1)
            {
                if (!File.Exists(path)) { Debug.LogWarning("[DataHeist] Missing scene " + path); return; }
                scenes.RemoveAll(s => s.path == path);
                var entry = new EditorBuildSettingsScene(path, true);
                if (index >= 0 && index <= scenes.Count) scenes.Insert(index, entry); else scenes.Add(entry);
            }
            // the home page must stay first so the build boots into it; missing scenes are dropped
            scenes.RemoveAll(s => !File.Exists(s.path));
            Ensure(ScenesDir + DH.SceneMainMenu + ".unity", 0);
            Ensure(ScenesDir + DH.SceneStory + ".unity", 1);
            Ensure(ScenesDir + DH.SceneCharacterSelect + ".unity", 2);
            Ensure(ScenesDir + DH.SceneLevel01 + ".unity", 3);
            Ensure(ScenesDir + DH.SceneLevel02 + ".unity", 4);
            Ensure(ScenesDir + DH.SceneLevel03 + ".unity", 5);
            Ensure(ScenesDir + DH.SceneEnding + ".unity", 6);
            Ensure(ScenesDir + DH.SceneCompletion + ".unity", 7);
            Ensure(ScenesDir + DH.SceneCredits + ".unity", 8);
            EditorBuildSettings.scenes = scenes.ToArray();
            Debug.Log("[DataHeist] Build Settings: " + string.Join(", ", scenes.Select(s => Path.GetFileNameWithoutExtension(s.path))));
        }

        [MenuItem("Data Heist/Play Level 1", priority = 10)]
        public static void PlayLevel1()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(ScenesDir + DH.SceneLevel01 + ".unity");
            EditorApplication.isPlaying = true;
        }

        [MenuItem("Data Heist/Debug/Unlock All Runners, Levels + 1000 Bytes", priority = 40)]
        public static void UnlockAll()
        {
            var d = SaveSystem.Data;
            foreach (var c in CharacterRoster.CreateDefaults()) if (!d.unlocked.Contains(c.id)) d.unlocked.Add(c.id);
            foreach (var l in LevelCatalog.All) if (!d.cleared.Contains(l.id)) d.cleared.Add(l.id);
            d.bytes += 1000;
            SaveSystem.Save();
            Debug.Log("[DataHeist] All runners unlocked.");
        }

        [MenuItem("Data Heist/Play Level 2", priority = 11)]
        public static void PlayLevel2()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(ScenesDir + DH.SceneLevel02 + ".unity");
            EditorApplication.isPlaying = true;
        }

        [MenuItem("Data Heist/Play Level 3", priority = 12)]
        public static void PlayLevel3()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(ScenesDir + DH.SceneLevel03 + ".unity");
            EditorApplication.isPlaying = true;
        }

        [MenuItem("Data Heist/Play Ending", priority = 14)]
        public static void PlayEnding()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(ScenesDir + DH.SceneEnding + ".unity");
            EditorApplication.isPlaying = true;
        }

        [MenuItem("Data Heist/Play Story", priority = 13)]
        public static void PlayStory()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(ScenesDir + DH.SceneStory + ".unity");
            EditorApplication.isPlaying = true;
        }

        [MenuItem("Data Heist/Debug/Reset Save Data", priority = 41)]
        public static void ResetSave()
        {
            if (EditorUtility.DisplayDialog("Reset save?", "Clears wallet, unlocks and records.", "Reset", "Cancel"))
            {
                SaveSystem.ResetAll();
                PlayerPrefs.DeleteKey("dataheist.bindings");
                Debug.Log("[DataHeist] Save data reset.");
            }
        }
    }
}
