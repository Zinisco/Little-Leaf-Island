using UnityEngine;
using UnityEngine.Events;

public class EconomySystem : MonoBehaviour
{
    public static EconomySystem I;

    [Header("Player Economy")]
    [SerializeField] private int coins = 0;

    public UnityEvent<int> OnCoinsChanged; // For UI to listen in

    void Awake()
    {
        I = this;
    }

    public int Coins => coins; // public getter

    public void AddCoins(int amount)
    {
        if (amount <= 0) return;
        coins += amount;
        OnCoinsChanged?.Invoke(coins);
        Debug.Log($"Coins +{amount} -> {coins}");
    }

    public bool SpendCoins(int amount)
    {
        if (amount <= 0) return true;
        if (coins < amount)
        {
            Debug.Log("Not enough coins!");
            return false;
        }

        coins -= amount;
        OnCoinsChanged?.Invoke(coins);
        Debug.Log($"Coins -{amount} -> {coins}");
        return true;
    }
}
