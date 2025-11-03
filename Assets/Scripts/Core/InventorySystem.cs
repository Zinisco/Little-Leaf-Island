using UnityEngine;

public static class InventorySystem
{
    public static int Wood { get; private set; }
    public static int Stone { get; private set; }

    public static void AddWood(int amount)
    {
        Wood += amount;
        Debug.Log($"+{amount} Wood (Total: {Wood})");
    }

    public static void AddStone(int amount)
    {
        Stone += amount;
        Debug.Log($"+{amount} Stone (Total: {Stone})");
    }
}
