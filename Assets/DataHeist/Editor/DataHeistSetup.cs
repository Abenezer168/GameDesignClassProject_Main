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
            AddScenesToBuild();
            WireMainMenu();
            EditorUtility.DisplayDialog("The Data Heist",
                "Setup complete.\n\n- Pixel art re-imported\n- Character definitions created\n- Character Select + Level 1 added to Build Settings\n- Main Menu PLAY now opens Character Select\n\nOpen MainMenuScreenScene and press Play.",
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

        [MenuItem("Data Heist/Add Scenes To Build Settings", priority = 3)]
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
            // Main menu must stay first so the build boots into it
            var main = scenes.FirstOrDefault(s => s.path.EndsWith(DH.SceneMainMenu + ".unity"));
            if (main != null) { scenes.Remove(main); main.enabled = true; scenes.Insert(0, main); }
            Ensure(ScenesDir + DH.SceneCharacterSelect + ".unity", 1);
            Ensure(ScenesDir + DH.SceneLevel01 + ".unity", 2);
            EditorBuildSettings.scenes = scenes.ToArray();
            Debug.Log("[DataHeist] Build Settings: " + string.Join(", ", scenes.Select(s => Path.GetFileNameWithoutExtension(s.path))));
        }

        /// <summary>Points the existing Main Menu PLAY button at Character Select.</summary>
        [MenuItem("Data Heist/Wire Main Menu Play Button", priority = 4)]
        public static void WireMainMenu()
        {
            var path = EditorBuildSettings.scenes.Select(s => s.path).FirstOrDefault(p => p.EndsWith(DH.SceneMainMenu + ".unity"));
            if (path == null) { Debug.LogWarning("[DataHeist] MainMenuScreenScene not found in Build Settings."); return; }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            int changed = 0;
            foreach (var b in Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var so = new SerializedObject(b);
                var calls = so.FindProperty("m_OnClick.m_PersistentCalls.m_Calls");
                for (int i = 0; i < calls.arraySize; i++)
                {
                    var arg = calls.GetArrayElementAtIndex(i).FindPropertyRelative("m_Arguments.m_StringArgument");
                    if (arg != null && arg.stringValue == "GameScreenScene")
                    {
                        arg.stringValue = DH.SceneCharacterSelect;
                        changed++;
                    }
                }
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            if (changed > 0) { EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); }
            Debug.Log($"[DataHeist] Main menu wired ({changed} button(s) now load {DH.SceneCharacterSelect}).");
        }

        [MenuItem("Data Heist/Play Level 1", priority = 10)]
        public static void PlayLevel1()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(ScenesDir + DH.SceneLevel01 + ".unity");
            EditorApplication.isPlaying = true;
        }

        [MenuItem("Data Heist/Debug/Unlock All Runners + 1000 Bytes", priority = 40)]
        public static void UnlockAll()
        {
            var d = SaveSystem.Data;
            foreach (var c in CharacterRoster.CreateDefaults()) if (!d.unlocked.Contains(c.id)) d.unlocked.Add(c.id);
            d.bytes += 1000;
            SaveSystem.Save();
            Debug.Log("[DataHeist] All runners unlocked.");
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
