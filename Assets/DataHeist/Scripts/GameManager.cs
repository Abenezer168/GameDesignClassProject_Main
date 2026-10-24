using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// Controls the state of one level: coins, the Data Shard, pausing, game over and level complete.
public class GameManager : MonoBehaviour
{
    // "instance" lets other scripts reach the GameManager with GameManager.instance
    public static GameManager instance;

    [Header("References")]
    public Transform player;
    public Transform finishLine;
    public UIManager uiManager;
    public AudioSource soundEffects;

    [Header("Level")]
    public string levelName = "Level1";
    public string nextLevelName = "";      // leave empty if this is the last level

    [Header("Sounds")]
    public AudioClip coinSound;
    public AudioClip dataShardSound;
    public AudioClip gameOverSound;
    public AudioClip levelCompleteSound;

    [Header("This run")]
    public int coinCount = 0;              // coins collected in this run
    public bool hasDataShard = false;      // has the Data Shard been collected in this run?
    public bool isGameOver = false;        // true once the run has ended (game over or level complete)

    bool isPaused = false;

    void Awake()
    {
        instance = this;
        Time.timeScale = 1f;               // make sure the game is not still paused from before
    }

    void Update()
    {
        // Escape pauses and unpauses the game
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame && !isGameOver)
        {
            if (isPaused)
            {
                ResumeGame();
            }
            else
            {
                PauseGame();
            }
        }

        // The player reached the end of the level
        if (!isGameOver && player.position.x >= finishLine.position.x)
        {
            if (hasDataShard)
            {
                LevelComplete();
            }
            else
            {
                GameOver("You missed the Data Shard!");
            }
        }
    }

    // Called by Coin.cs when the player touches a coin
    public void AddCoin()
    {
        coinCount = coinCount + 1;
        PlaySound(coinSound);
    }

    // Called by DataShard.cs when the player touches the Data Shard
    public void CollectDataShard()
    {
        hasDataShard = true;
        PlaySound(dataShardSound);
    }

    public void GameOver(string reason)
    {
        if (isGameOver)
        {
            return;                         // the run already ended
        }
        isGameOver = true;
        SaveManager.AddCoins(coinCount);    // save this run's coins once
        PlaySound(gameOverSound);
        uiManager.ShowGameOver(reason, coinCount);
    }

    public void LevelComplete()
    {
        if (isGameOver)
        {
            return;
        }
        isGameOver = true;
        SaveManager.AddCoins(coinCount);    // save this run's coins once
        SaveManager.SaveDataShard(levelName);
        PlaySound(levelCompleteSound);
        uiManager.ShowLevelComplete(coinCount, SaveManager.GetTotalCoins(), hasDataShard, nextLevelName != "");
    }

    // Plays a sound effect at the volume chosen in Options
    public void PlaySound(AudioClip clip)
    {
        if (clip != null)
        {
            soundEffects.PlayOneShot(clip, SaveManager.GetSoundVolume());
        }
    }

    // ---------------- Pause ----------------

    public void PauseGame()
    {
        isPaused = true;
        Time.timeScale = 0f;                // freezes movement and timers
        uiManager.ShowPause(true);
    }

    public void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f;
        uiManager.ShowPause(false);
    }

    // ---------------- Buttons ----------------

    public void RetryLevel()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void LoadNextLevel()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(nextLevelName);
    }

    public void LoadMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }
}
