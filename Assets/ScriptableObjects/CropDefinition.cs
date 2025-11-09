using UnityEngine;

[CreateAssetMenu(menuName = "LLI/Crop Definition")]
public class CropDefinition : ScriptableObject
{
    [Header("Basic Data")]
    public string id;
    public string displayName = "Unnamed";

    [Header("Growth Stages")]
    public GameObject[] stagePrefabs;

    [Header("Growth Rules")]
    public int daysToGrow = 3;
    public bool regrows = false;

    [Header("Harvest Output")]
    public ItemDefinition outputItem;     // normal item produced
    public int outputQuantity = 1;

    [Header("Rare Harvest")]
    public bool hasRareHarvest = false;
    public ItemDefinition rareItem;     // e.g., GoldenCarrot
    public bool rareReplaces = true;    // true = replace main item, false = add to it
    [Range(0, 100)] public float rareChance = 5f;  // % per harvest

    public int StageCount => stagePrefabs?.Length ?? 0;
}
