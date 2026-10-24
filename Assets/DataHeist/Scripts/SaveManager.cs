using UnityEngine;

// Saves and loads the player's progress using PlayerPrefs (Unity's simple built-in save system).
// It is a static class, so any script can call it directly, for example SaveManager.GetTotalCoins().
public static class SaveManager
{
    // ---------------- Coins ----------------

    // Total coins the player owns (shown and spent in the Shop)
    public static int GetTotalCoins()
    {
        return PlayerPrefs.GetInt("TotalCoins", 0);
    }

    // Adds the coins from one run to the saved total
    public static void AddCoins(int amount)
    {
        PlayerPrefs.SetInt("TotalCoins", GetTotalCoins() + amount);
        PlayerPrefs.Save();
    }

    // Takes coins away if the player has enough. Returns true if it worked.
    public static bool SpendCoins(int amount)
    {
        if (GetTotalCoins() < amount)
        {
            return false;
        }
        PlayerPrefs.SetInt("TotalCoins", GetTotalCoins() - amount);
        PlayerPrefs.Save();
        return true;
    }

    // ---------------- Data Shard ----------------

    public static bool HasDataShard(string levelName)
    {
        return PlayerPrefs.GetInt("DataShard_" + levelName, 0) == 1;
    }

    public static void SaveDataShard(string levelName)
    {
        PlayerPrefs.SetInt("DataShard_" + levelName, 1);
        PlayerPrefs.Save();
    }

    // ---------------- Shop: extra hearts ----------------

    public static int GetExtraHearts()
    {
        return PlayerPrefs.GetInt("ExtraHearts", 0);
    }

    public static void AddExtraHeart()
    {
        PlayerPrefs.SetInt("ExtraHearts", GetExtraHearts() + 1);
        PlayerPrefs.Save();
    }

    // ---------------- Options: volume (0 = silent, 1 = full) ----------------

    public static float GetMusicVolume()
    {
        return PlayerPrefs.GetFloat("MusicVolume", 0.6f);
    }

    public static void SetMusicVolume(float volume)
    {
        PlayerPrefs.SetFloat("MusicVolume", volume);
        PlayerPrefs.Save();
    }

    public static float GetSoundVolume()
    {
        return PlayerPrefs.GetFloat("SoundVolume", 0.8f);
    }

    public static void SetSoundVolume(float volume)
    {
        PlayerPrefs.SetFloat("SoundVolume", volume);
        PlayerPrefs.Save();
    }
}
