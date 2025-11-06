using UnityEngine;

[CreateAssetMenu(menuName = "LLI/Item Definition")]
public class ItemDefinition : ScriptableObject
{
    public string itemID;        // must be unique, e.g. "wood", "stone", "carrot"
    public string displayName;

    [Header("Stacking")]
    public int maxStack = 99;

    [Header("Visuals")]
    public Sprite icon;  // for UI inventory
}
