using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "LLI/Item Database")]
public class ItemDatabase : ScriptableObject
{
    public static ItemDatabase I;

    [Tooltip("All items in the game. Drag ItemDefinitions here.")]
    public List<ItemDefinition> items = new List<ItemDefinition>();

    // Build lookup dictionary at runtime
    Dictionary<string, ItemDefinition> lookup;

    void OnEnable()
    {
        I = this;

        lookup = new Dictionary<string, ItemDefinition>();
        foreach (var item in items)
        {
            if (item == null || string.IsNullOrEmpty(item.itemID))
                continue;

            if (!lookup.ContainsKey(item.itemID))
                lookup.Add(item.itemID, item);
            else
                Debug.LogWarning($"Duplicate itemID detected: {item.itemID}");
        }
    }

    public ItemDefinition GetItemByID(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        lookup.TryGetValue(id, out var result);
        return result;
    }
}
