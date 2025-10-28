using UnityEngine;

[CreateAssetMenu(menuName = "LLI/Crop Definition")]
public class CropDefinition : ScriptableObject
{
    [Header("Basic Data")]
    public string id;                         // "carrot"
    public string displayName = "Unnamed";    // UI-friendly name

    [Header("Visual Growth Stages")]
    public GameObject[] stagePrefabs;         // Prefabs for each growth stage

    [Header("Growth Rules")]
    public int daysToGrow = 3;                // Real-world midnights required
    public bool regrows = false;              // If it regrows after harvest

    [Header("Economy")]
    public int sellPrice = 5;                 // Coins per harvest

    public int StageCount => stagePrefabs?.Length ?? 0;
}
