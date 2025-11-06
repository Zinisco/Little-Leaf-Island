using System;
using System.Collections.Generic;
using UnityEngine;

public class InventorySystem : MonoBehaviour
{
    public enum ResourceType { Wood, Stone, Carrot }

    public static InventorySystem I;

    [Header("Slots")]
    [SerializeField] int startingSlots = 36;
    [SerializeField] List<InventorySlot> slots = new List<InventorySlot>();

    [Header("Enum -> Item mapping")]
    [SerializeField] private ItemDefinition woodItem;
    [SerializeField] private ItemDefinition stoneItem;
    [SerializeField] private ItemDefinition carrotItem;

    // Events
    public static event Action<ResourceType, int> OnResourceChanged;
    public static event Action OnInventoryChanged;

    void Awake()
    {
        I = this;
        if (slots.Count == 0)
        {
            for (int i = 0; i < startingSlots; i++)
                slots.Add(new InventorySlot());
        }
    }

    // ---------- Legacy static API kept intact ----------
    public static int Get(ResourceType type)
    {
        if (I == null)
        {
            Debug.LogWarning("InventorySystem accessed before initialization.");
            return 0;
        }

        var item = I.Resolve(type);
        return I.CountOf(item);
    }


    public static void Add(ResourceType type, int amount)
    {
        var item = I.Resolve(type);
        I.AddItem(item, amount);
        OnResourceChanged?.Invoke(type, Get(type));
        OnInventoryChanged?.Invoke();
        Debug.Log($"+{amount} {type} (Total: {Get(type)})");
    }

    public static bool TrySpend(ResourceType type, int amount)
    {
        var item = I.Resolve(type);
        if (!I.CanRemove(item, amount)) return false;

        I.RemoveItem(item, amount);
        OnResourceChanged?.Invoke(type, Get(type));
        OnInventoryChanged?.Invoke();
        return true;
    }

    // Save/load helpers if you want to serialize exact slot contents later
    public Dictionary<string, int> ExportAll()
    {
        // Flatten counts by itemID
        var dict = new Dictionary<string, int>();
        foreach (var slot in slots)
        {
            if (slot.IsEmpty) continue;
            string id = slot.item.itemID;
            if (!dict.ContainsKey(id)) dict[id] = 0;
            dict[id] += slot.quantity;
        }
        return dict;
    }

    public void ImportAll(Dictionary<string, int> flatCounts, Func<string, ItemDefinition> idToItem)
    {
        // Clear inventory
        foreach (var s in slots) s.Clear();

        if (flatCounts != null)
        {
            foreach (var kvp in flatCounts)
            {
                var def = idToItem?.Invoke(kvp.Key);
                if (def == null) continue;
                AddItem(def, kvp.Value);
            }
        }

        // Fire legacy resource changed for the mapped items
        OnResourceChanged?.Invoke(ResourceType.Wood, Get(ResourceType.Wood));
        OnResourceChanged?.Invoke(ResourceType.Stone, Get(ResourceType.Stone));
        OnResourceChanged?.Invoke(ResourceType.Carrot, Get(ResourceType.Carrot));
        OnInventoryChanged?.Invoke();
    }

    // ---------- New slot-based engine ----------
    ItemDefinition Resolve(ResourceType type)
    {
        switch (type)
        {
            case ResourceType.Wood: return woodItem;
            case ResourceType.Stone: return stoneItem;
            case ResourceType.Carrot: return carrotItem;
        }
        return null;
    }

    public int CountOf(ItemDefinition item)
    {
        if (item == null) return 0;
        int total = 0;
        foreach (var slot in slots)
            if (!slot.IsEmpty && slot.item == item)
                total += slot.quantity;
        return total;
    }

    public bool AddItem(ItemDefinition item, int amount)
    {
        if (item == null || amount <= 0) return false;

        // 1) Fill existing stacks
        foreach (var slot in slots)
        {
            if (!slot.IsEmpty && slot.item == item && !slot.IsFull)
            {
                int space = item.maxStack - slot.quantity;
                int toAdd = Mathf.Min(space, amount);
                slot.quantity += toAdd;
                amount -= toAdd;
                if (amount <= 0) return true;
            }
        }

        // 2) Use empty slots
        foreach (var slot in slots)
        {
            if (slot.IsEmpty)
            {
                slot.item = item;
                slot.quantity = Mathf.Min(item.maxStack, amount);
                amount -= slot.quantity;
                if (amount <= 0) return true;
            }
        }

        // If we get here, inventory is full for the leftover amount
        return amount <= 0;
    }

    public bool CanRemove(ItemDefinition item, int amount)
    {
        return CountOf(item) >= amount;
    }

    public bool RemoveItem(ItemDefinition item, int amount)
    {
        if (item == null || amount <= 0) return false;
        if (!CanRemove(item, amount)) return false;

        // Drain stacks from rightmost or leftmost. Choose a policy. Here: left to right.
        for (int i = 0; i < slots.Count && amount > 0; i++)
        {
            var slot = slots[i];
            if (slot.IsEmpty || slot.item != item) continue;

            int take = Mathf.Min(slot.quantity, amount);
            slot.quantity -= take;
            amount -= take;
            if (slot.quantity <= 0) slot.Clear();
        }

        return true;
    }

    public List<InventorySlotSaveData> ExportSlots()
    {
        var list = new List<InventorySlotSaveData>();
        foreach (var slot in slots)
        {
            list.Add(new InventorySlotSaveData
            {
                itemID = slot.IsEmpty ? "" : slot.item.itemID,
                quantity = slot.IsEmpty ? 0 : slot.quantity
            });
        }
        return list;
    }

    public void ImportSlots(List<InventorySlotSaveData> data, Func<string, ItemDefinition> idToItem)
    {
        // Clear existing first
        foreach (var s in slots) s.Clear();

        if (data == null) return;

        for (int i = 0; i < data.Count && i < slots.Count; i++)
        {
            var entry = data[i];

            if (string.IsNullOrEmpty(entry.itemID) || entry.quantity <= 0)
            {
                slots[i].Clear();
                continue;
            }

            ItemDefinition def = idToItem(entry.itemID);
            if (def == null)
            {
                slots[i].Clear();
                continue;
            }

            slots[i].item = def;
            slots[i].quantity = entry.quantity;
        }

        OnInventoryChanged?.Invoke();
    }

    public void MoveSlot(int from, int to)
    {
        var slots = this.slots;

        // Simple swap for now
        var temp = slots[from];
        slots[from] = slots[to];
        slots[to] = temp;

        OnInventoryChanged?.Invoke();
    }

    // Optional: expose slots for UI
    public IReadOnlyList<InventorySlot> Slots => slots;
}
