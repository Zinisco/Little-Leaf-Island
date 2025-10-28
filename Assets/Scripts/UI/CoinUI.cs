using UnityEngine;
using TMPro;

public class CoinUI : MonoBehaviour
{
    [SerializeField] private TMP_Text coinText;

    void Start()
    {
        UpdateCoins(EconomySystem.I.Coins);
        EconomySystem.I.OnCoinsChanged.AddListener(UpdateCoins);
    }

    void UpdateCoins(int amount)
    {
        coinText.text = "$ " + amount.ToString();
    }
}
