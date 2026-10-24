using UnityEngine;
using UnityEngine.UI;

// The Shop page: spend saved coins on extra hearts (more starting health).
public class ShopMenu : MonoBehaviour
{
    public Text coinsText;
    public Text heartsText;
    public Text messageText;
    public int extraHeartPrice = 100;
    public int maxExtraHearts = 2;
    public AudioSource soundEffects;
    public AudioClip buySound;

    void Start()
    {
        messageText.text = "";
        UpdateText();
    }

    // Called by the Buy button
    public void BuyExtraHeart()
    {
        if (SaveManager.GetExtraHearts() >= maxExtraHearts)
        {
            messageText.text = "You already have the most hearts!";
        }
        else if (SaveManager.SpendCoins(extraHeartPrice))
        {
            SaveManager.AddExtraHeart();
            messageText.text = "Bought an extra heart!";
            soundEffects.PlayOneShot(buySound, SaveManager.GetSoundVolume());
        }
        else
        {
            messageText.text = "Not enough coins.";
        }
        UpdateText();
    }

    void UpdateText()
    {
        coinsText.text = "Coins: " + SaveManager.GetTotalCoins();
        heartsText.text = "Extra hearts: " + SaveManager.GetExtraHearts() + " / " + maxExtraHearts;
    }
}
