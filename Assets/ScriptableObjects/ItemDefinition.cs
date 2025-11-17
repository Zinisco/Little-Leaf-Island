using UnityEngine;

[CreateAssetMenu(menuName = "LLI/Item Definition")]
public class ItemDefinition : ScriptableObject
{
    [Header("Identification")]
    public string itemID;        // must be unique, e.g. "wood", "stone", "carrot"
    public string displayName;

    [Header("Stacking")]
    public int maxStack = 99;

    [Header("Visuals")]
    public Sprite icon;

    [Header("Economy")]
    public int sellPrice = 1;     // coins when sold (per unit)

    [Header("Placeable Settings")]
    public bool isPlaceable = false;
    public GameObject placeablePrefab;  // e.g., your StorageChest prefab
    public float placementYOffset = 0.02f;
}
