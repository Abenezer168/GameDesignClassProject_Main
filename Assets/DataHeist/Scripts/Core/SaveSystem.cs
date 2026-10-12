using System;
using System.Collections.Generic;
using UnityEngine;

namespace DataHeist
{
    [Serializable]
    public class LevelRecord
    {
        public string levelId;
        public int score;
        public string rank;
        public float time;
        public int bytes;
        public int fragments;               // Level 3 data fragments on the best run
    }

    [Serializable]
    public class SaveData
    {
        public int bytes;                                   // wallet (spent in shop / unlocks)
        public List<string> unlocked = new List<string> { "kai" };
        public string selected = "kai";
        public List<string> cleared = new List<string>();
        public List<LevelRecord> records = new List<LevelRecord>();
        public float masterVolume = 1f;
        public float musicVolume = 0.6f;
        public float sfxVolume = 0.8f;
        public int resolutionWidth;                         // 0 = keep the current/native resolution
        public int resolutionHeight;
        public bool fullscreen = true;
        public bool vsync = true;
        public bool storySeen;
        // campaign progress for the ending and the completion screen
        public bool guardianAward;          // set when the final mission (Level 3) is completed
        public int totalDeaths;             // every failed attempt, all levels
        public float totalPlayTime;         // seconds spent in runs, all levels
        public bool endingSeen;             // the final cinematic has played at least once
        // abilities earned in play (PlayerAbilityManager); kept until a new game
        public bool doubleJumpUnlocked;     // reached the double-jump goal in any run
    }

    /// <summary>PlayerPrefs-backed JSON save. One global instance; call Save() after changes.</summary>
    public static class SaveSystem
    {
        const string Key = "dataheist.save.v1";
        static SaveData _data;

        public static SaveData Data
        {
            get
            {
                if (_data != null) return _data;
                try
                {
                    var json = PlayerPrefs.GetString(Key, "");
                    // overwrite a fresh instance so fields added after a save was written keep their defaults
                    _data = new SaveData();
                    if (!string.IsNullOrEmpty(json)) JsonUtility.FromJsonOverwrite(json, _data);
                }
                catch (Exception e)
                {
                    Debug.LogWarning("[DataHeist] Save corrupted, starting fresh: " + e.Message);
                    _data = new SaveData();
                }
                _data.unlocked ??= new List<string>();
                if (!_data.unlocked.Contains("kai")) _data.unlocked.Add("kai");
                _data.cleared ??= new List<string>();
                _data.records ??= new List<LevelRecord>();
                return _data;
            }
        }

        public static void Save()
        {
            PlayerPrefs.SetString(Key, JsonUtility.ToJson(Data));
            PlayerPrefs.Save();
        }

        public static bool IsUnlocked(CharacterDefinition c) => Data.unlocked.Contains(c.id);

        /// <summary>True if the runner can be recruited right now.</summary>
        public static bool CanUnlock(CharacterDefinition c)
        {
            if (IsUnlocked(c)) return false;
            return c.unlock switch
            {
                UnlockType.Free => true,
                UnlockType.Bytes => Data.bytes >= c.unlockCost,
                UnlockType.ClearLevel => Data.cleared.Contains(c.unlockLevelId),
                _ => false,
            };
        }

        public static bool TryUnlock(CharacterDefinition c)
        {
            if (!CanUnlock(c)) return false;
            if (c.unlock == UnlockType.Bytes) Data.bytes -= c.unlockCost;
            Data.unlocked.Add(c.id);
            Save();
            return true;
        }

        public static LevelRecord GetRecord(string levelId) => Data.records.Find(r => r.levelId == levelId);

        /// <summary>Stores a run; returns true if it is a new best score.</summary>
        public static bool SubmitRecord(LevelRecord run)
        {
            var best = GetRecord(run.levelId);
            if (best == null) { Data.records.Add(run); return true; }
            bool better = run.score > best.score;
            if (better) { best.score = run.score; best.time = run.time; best.bytes = run.bytes; }
            best.fragments = Mathf.Max(best.fragments, run.fragments);
            if (RankValue(run.rank) > RankValue(best.rank)) best.rank = run.rank;
            return better;
        }

        public static int RankValue(string r) => "CBAS".IndexOf(r ?? "C", StringComparison.Ordinal);

        /// <summary>Drops the cached save so the next access re-reads PlayerPrefs.</summary>
        public static void Reload() => _data = null;

        public const string PrefsKey = Key;

        public static void ResetAll()
        {
            _data = new SaveData();
            Save();
        }
    }

    /// <summary>Cross-scene hand-off between menus and levels.</summary>
    public static class GameSession
    {
        public static string SelectedCharacter
        {
            get => SaveSystem.Data.selected;
            set { SaveSystem.Data.selected = value; SaveSystem.Save(); }
        }
        public static string CurrentLevel = "level01";
    }
}
