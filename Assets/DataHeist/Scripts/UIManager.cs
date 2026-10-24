using UnityEngine;
using UnityEngine.UI;

// Shows the text at the top of the screen and the Pause, Game Over and Level Complete screens.
public class UIManager : MonoBehaviour
{
    [Header("Top of the screen")]
    public Text statusText;                 // "Coins: 25     Health: 3     Data Shard: 0/1"
    public PlayerHealth playerHealth;

    [Header("Pause screen")]
    public GameObject pausePanel;

    [Header("Game Over screen")]
    public GameObject gameOverPanel;
    public Text gameOverReasonText;
    public Text gameOverCoinsText;

    [Header("Level Complete screen")]
    public GameObject levelCompletePanel;
    public Text levelCompleteCoinsText;
    public Text levelCompleteTotalText;
    public Text levelCompleteShardText;
    public GameObject nextLevelButton;

    void Start()
    {
        pausePanel.SetActive(false);
        gameOverPanel.SetActive(false);
        levelCompletePanel.SetActive(false);
    }

    void Update()
    {
        int shards = 0;
        if (GameManager.instance.hasDataShard)
        {
            shards = 1;
        }
        statusText.text = "Coins: " + GameManager.instance.coinCount
                        + "     Health: " + playerHealth.playerHealth
                        + "     Data Shard: " + shards + "/1";
    }

    public void ShowPause(bool show)
    {
        pausePanel.SetActive(show);
    }

    public void ShowGameOver(string reason, int coins)
    {
        gameOverReasonText.text = reason;
        gameOverCoinsText.text = "Coins Collected: " + coins;
        gameOverPanel.SetActive(true);
    }

    public void ShowLevelComplete(int coins, int totalCoins, bool dataShard, bool hasNextLevel)
    {
        levelCompleteCoinsText.text = "Coins Collected: " + coins;
        levelCompleteTotalText.text = "Total Coins: " + totalCoins;
        if (dataShard)
        {
            levelCompleteShardText.text = "Data Shard Collected: Yes";
        }
        else
        {
            levelCompleteShardText.text = "Data Shard Collected: No";
        }
        nextLevelButton.SetActive(hasNextLevel);
        levelCompletePanel.SetActive(true);
    }
}
