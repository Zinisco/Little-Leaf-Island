using System;
using UnityEngine;

public class XPSystem : MonoBehaviour
{
    public static XPSystem I;

    [Header("Progression")]
    [SerializeField] int baseXPToLevel = 50;
    [SerializeField] float xpGrowth = 1.25f; // next level costs more
    [SerializeField] int maxLevel = 100;

    public int Level { get; private set; } = 1;
    public int CurrentXP { get; private set; } = 0;
    public int XPToNext => Mathf.CeilToInt(baseXPToLevel * Mathf.Pow(xpGrowth, Level - 1));

    public event Action<int, int> OnXPChanged; // (xp, xpToNext)
    public event Action<int> OnLevelUp;        // (newLevel)

    void Awake()
    {
        if (I != null && I != this) { Destroy(gameObject); return; }
        I = this;
    }

    public void AddXP(int amount)
    {
        if (amount <= 0) return;

        CurrentXP += amount;

        while (Level < maxLevel && CurrentXP >= XPToNext)
        {
            CurrentXP -= XPToNext;
            Level++;
            OnLevelUp?.Invoke(Level);
        }

        OnXPChanged?.Invoke(CurrentXP, XPToNext);

        Debug.Log($"AddXP({amount}) -> XP {CurrentXP}/{XPToNext}, Level {Level}");
    }

    // Optional helpers (nice for loading / debugging)
    public void SetLevel(int level)
    {
        Level = Mathf.Clamp(level, 1, maxLevel);
        OnXPChanged?.Invoke(CurrentXP, XPToNext);
    }

    public void SetXP(int xp)
    {
        CurrentXP = Mathf.Max(0, xp);
        OnXPChanged?.Invoke(CurrentXP, XPToNext);
    }
}
