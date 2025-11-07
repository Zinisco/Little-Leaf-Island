using System;
using System.Collections.Generic;
using UnityEngine;

public class InventorySystem : MonoBehaviour
{
    public enum ResourceType { Wood, Stone, Carrot }

    public static InventorySystem I;

    [Header("Inventory Slots")]
    [SerializeField] int startingSlots = 36;
    [SerializeField] List<InventorySlot> slots = new List<InventorySlot>();

    [Header("Shipping Slots")]
    [SerializeField] int shippingSlotCount = 12;                // 2 rows of 6
    [SerializeField] List<InventorySlot> shippingSlots = new List<InventorySlot>();

    [Header("Enum -> Item mapping")]
    [SerializeField] private ItemDefinition woodItem;
    [SerializeField] private ItemDefinition stoneItem;
    [SerializeField] private ItemDefinition carrotItem;

    [Header("Sell Prices (fallback map)")]
    [SerializeField] private List<ItemPrice> priceTable = new List<ItemPrice>();

    [Serializable] public class ItemPrice { public ItemDefinition item; public int price; }

    // Events
    public static event Action<ResourceType, int> OnResourceChanged;
    public static event Action OnInventoryChanged;
    public static event Action OnShippingChanged;

    // Fired when a sale happens (for your UI popup if you want it later)
    public static event Action<ShippingSaleResult> OnShippingSold;

    void Awake()
    {
        I = this;

        if (slots.Count == 0)
        {
            for (int i = 0; i < startingSlots; i++)
                slots.Add(new InventorySlot());
        }

        if (shippingSlots.Count == 0)
        {
            for (int i = 0; i < shippingSlotCount; i++)
                shippingSlots.Add(new InventorySlot());
        }
    }

    // ---------- Legacy static API kept intact ----------
    public static int Get(ResourceType type)
    {
        if (I == null) { Debug.LogWarning("InventorySystem accessed before initialization."); return 0; }
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

    // ---------- New slot-based engine (Inventory) ----------
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
                if (amount <= 0) { OnInventoryChanged?.Invoke(); return true; }
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
                if (amount <= 0) { OnInventoryChanged?.Invoke(); return true; }
            }
        }

        OnInventoryChanged?.Invoke();
        return amount <= 0;
    }

    public bool CanRemove(ItemDefinition item, int amount) => CountOf(item) >= amount;

    public bool RemoveItem(ItemDefinition item, int amount)
    {
        if (item == null || amount <= 0) return false;
        if (!CanRemove(item, amount)) return false;

        for (int i = 0; i < slots.Count && amount > 0; i++)
        {
            var slot = slots[i];
            if (slot.IsEmpty || slot.item != item) continue;

            int take = Mathf.Min(slot.quantity, amount);
            slot.quantity -= take;
            amount -= take;
            if (slot.quantity <= 0) slot.Clear();
        }

        OnInventoryChanged?.Invoke();
        return true;
    }

    public void MoveInventorySlot(int from, int to)
    {
        var temp = slots[from];
        slots[from] = slots[to];
        slots[to] = temp;
        OnInventoryChanged?.Invoke();
    }

    // ---------- Shipping Bin API ----------
    public IReadOnlyList<InventorySlot> ShippingSlots => shippingSlots;

    public bool AddToShipping(ItemDefinition item, int amount, int toIndex)
    {
        if (item == null || amount <= 0) return false;

        // Try stack if same item in target
        var slot = shippingSlots[toIndex];
        if (slot.IsEmpty)
        {
            slot.item = item;
            slot.quantity = Mathf.Min(item.maxStack, amount);
            int remainder = amount - slot.quantity;

            // spill remainder anywhere in shipping
            if (remainder > 0) TryAddShippingOverflow(item, remainder);
        }
        else if (slot.item == item && !slot.IsFull)
        {
            int space = item.maxStack - slot.quantity;
            int toAdd = Mathf.Min(space, amount);
            slot.quantity += toAdd;
            int remainder = amount - toAdd;
            if (remainder > 0) TryAddShippingOverflow(item, remainder);
        }
        else
        {
            // Different item in target slot -> try general add
            TryAddShippingOverflow(item, amount);
        }

        OnShippingChanged?.Invoke();
        return true;
    }

    public void MoveShippingSlot(int from, int to)
    {
        var temp = shippingSlots[from];
        shippingSlots[from] = shippingSlots[to];
        shippingSlots[to] = temp;
        OnShippingChanged?.Invoke();
    }

    public bool TransferInventoryToShipping(int invIndex, int shipIndex)
    {
        var inv = slots[invIndex];
        if (inv.IsEmpty) return false;

        AddToShipping(inv.item, inv.quantity, shipIndex);
        inv.Clear();

        OnInventoryChanged?.Invoke();
        OnShippingChanged?.Invoke();
        return true;
    }

    public bool TransferShippingToInventory(int shipIndex, int invIndex)
    {
        var ship = shippingSlots[shipIndex];
        if (ship.IsEmpty) return false;

        // Try add fully into inventory; if some remainder, keep it in shipping
        int qty = ship.quantity;
        ItemDefinition item = ship.item;

        // Attempt to add; simulate: add then clear
        ship.Clear();
        OnShippingChanged?.Invoke();

        bool ok = AddItem(item, qty);
        if (!ok)
        {
            // if inventory couldn't take all, try to put back what didn't fit (best effort)
            // Here we keep it simple: if AddItem returned false, we assume some remainder.
            // Put a single full stack back so nothing gets lost.
            shippingSlots[shipIndex].item = item;
            shippingSlots[shipIndex].quantity = qty; // conservative; user can drag again
            OnShippingChanged?.Invoke();
            return false;
        }

        return true;
    }

    bool TryAddShippingOverflow(ItemDefinition item, int amount)
    {
        // 1) Fill existing stacks
        foreach (var s in shippingSlots)
        {
            if (!s.IsEmpty && s.item == item && !s.IsFull)
            {
                int space = item.maxStack - s.quantity;
                int toAdd = Mathf.Min(space, amount);
                s.quantity += toAdd;
                amount -= toAdd;
                if (amount <= 0) return true;
            }
        }
        // 2) Empty slots
        foreach (var s in shippingSlots)
        {
            if (s.IsEmpty)
            {
                s.item = item;
                s.quantity = Mathf.Min(item.maxStack, amount);
                amount -= s.quantity;
                if (amount <= 0) return true;
            }
        }
        return amount <= 0;
    }

    public int GetPrice(ItemDefinition def)
    {
        if (def != null && def.sellPrice > 0)        // <- use per-item price first
            return def.sellPrice;

        // Fallback table (optional)
        foreach (var ip in priceTable)
            if (ip.item == def) return Mathf.Max(0, ip.price);

        return 0;
    }


    // ----- Selling -----
    [Serializable]
    public class ShippingSaleEntry
    {
        public ItemDefinition item;
        public int quantity;
        public int unitPrice;
        public int subtotal;
    }

    [Serializable]
    public class ShippingSaleResult
    {
        public List<ShippingSaleEntry> entries = new List<ShippingSaleEntry>();
        public int total;
    }

    public ShippingSaleResult SellShippingContents()
    {
        var breakdown = new Dictionary<ItemDefinition, int>();
        foreach (var s in shippingSlots)
        {
            if (s.IsEmpty) continue;
            if (!breakdown.ContainsKey(s.item)) breakdown[s.item] = 0;
            breakdown[s.item] += s.quantity;
        }

        // clear bin
        foreach (var s in shippingSlots) s.Clear();
        OnShippingChanged?.Invoke();

        var result = new ShippingSaleResult();
        int totalCoins = 0;

        foreach (var kv in breakdown)
        {
            int unit = kv.Key.sellPrice;
            int subtotal = unit * kv.Value;
            result.entries.Add(new ShippingSaleEntry
            {
                item = kv.Key,
                quantity = kv.Value,
                unitPrice = unit,
                subtotal = subtotal
            });
            totalCoins += subtotal;
        }
        result.total = totalCoins;

        if (totalCoins > 0)
            EconomySystem.I.AddCoins(totalCoins);

        OnShippingSold?.Invoke(result);
        return result;
    }

    // ---------- Save helpers unchanged ----------
    public Dictionary<string, int> ExportAll()
    {
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

        OnResourceChanged?.Invoke(ResourceType.Wood, Get(ResourceType.Wood));
        OnResourceChanged?.Invoke(ResourceType.Stone, Get(ResourceType.Stone));
        OnResourceChanged?.Invoke(ResourceType.Carrot, Get(ResourceType.Carrot));
        OnInventoryChanged?.Invoke();
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
        foreach (var s in slots) s.Clear();
        if (data == null) return;

        for (int i = 0; i < data.Count && i < slots.Count; i++)
        {
            var entry = data[i];
            if (string.IsNullOrEmpty(entry.itemID) || entry.quantity <= 0) { slots[i].Clear(); continue; }

            ItemDefinition def = idToItem(entry.itemID);
            if (def == null) { slots[i].Clear(); continue; }

            slots[i].item = def;
            slots[i].quantity = entry.quantity;
        }

        OnInventoryChanged?.Invoke();
    }

    // === Helper accessors used by UI ===
    public int GetSlotQuantity(int index)
    {
        if (index < 0 || index >= slots.Count) return 0;
        return slots[index].IsEmpty ? 0 : slots[index].quantity;
    }

    public ItemDefinition GetSlotItem(int index)
    {
        if (index < 0 || index >= slots.Count) return null;
        return slots[index].IsEmpty ? null : slots[index].item;
    }

    // === Partial transfer: move up to 'amount' units from ONE inventory slot to ONE target slot ===
    // Returns how many were actually placed into the target slot.
    public int TransferInventoryPartial(int fromIndex, int toIndex, int amount)
    {
        if (amount <= 0) return 0;
        if (fromIndex == toIndex) return 0;
        if (fromIndex < 0 || fromIndex >= slots.Count) return 0;
        if (toIndex < 0 || toIndex >= slots.Count) return 0;

        var from = slots[fromIndex];
        var to = slots[toIndex];

        if (from.IsEmpty) return 0;

        ItemDefinition item = from.item;

        // Determine how much can fit into the target slot
        int canPlace = 0;

        if (to.IsEmpty)
        {
            canPlace = Mathf.Min(item.maxStack, amount);
        }
        else if (to.item == item && !to.IsFull)
        {
            int space = item.maxStack - to.quantity;
            canPlace = Mathf.Min(space, amount);
        }
        else
        {
            // different item already in target and not empty -> cannot place here
            return 0;
        }

        if (canPlace <= 0) return 0;

        // Clamp by what's available in 'from'
        canPlace = Mathf.Min(canPlace, from.quantity);

        // Apply to target
        if (to.IsEmpty)
        {
            to.item = item;
            to.quantity = canPlace;
        }
        else
        {
            to.quantity += canPlace;
        }

        // Remove from source
        from.quantity -= canPlace;
        if (from.quantity <= 0) from.Clear();

        OnInventoryChanged?.Invoke();
        return canPlace;
    }

    // === Partial transfer: move up to 'amount' units from ONE inventory slot to ONE shipping slot ===
    // Returns how many were actually placed into the shipping slot.
    public int TransferInventoryToShippingPartial(int fromInvIndex, int toShipIndex, int amount)
    {
        if (amount <= 0) return 0;
        if (fromInvIndex < 0 || fromInvIndex >= slots.Count) return 0;
        if (toShipIndex < 0 || toShipIndex >= shippingSlots.Count) return 0;

        var from = slots[fromInvIndex];
        var to = shippingSlots[toShipIndex];
        if (from.IsEmpty) return 0;

        ItemDefinition item = from.item;

        // S1: only allow empty or same-item (no mixing/swap)
        int canPlace = 0;
        if (to.IsEmpty)
        {
            canPlace = Mathf.Min(item.maxStack, amount);
        }
        else if (to.item == item && !to.IsFull)
        {
            int space = item.maxStack - to.quantity;
            canPlace = Mathf.Min(space, amount);
        }
        else
        {
            return 0; // different item present -> ignore
        }

        if (canPlace <= 0) return 0;

        // Clamp by what's actually available in source
        canPlace = Mathf.Min(canPlace, from.quantity);

        // Apply to shipping
        if (to.IsEmpty)
        {
            to.item = item;
            to.quantity = canPlace;
        }
        else
        {
            to.quantity += canPlace;
        }

        // Remove from inventory
        from.quantity -= canPlace;
        if (from.quantity <= 0) from.Clear();

        OnInventoryChanged?.Invoke();
        OnShippingChanged?.Invoke();
        return canPlace;
    }

    // --- NEW: Merge or Swap Inventory <-> Inventory ---
    public int MergeOrSwapInventory(int fromIndex, int toIndex)
    {
        if (fromIndex == toIndex) return 0;
        if (fromIndex < 0 || fromIndex >= slots.Count) return 0;
        if (toIndex < 0 || toIndex >= slots.Count) return 0;

        var from = slots[fromIndex];
        var to = slots[toIndex];
        if (from.IsEmpty) return 0;

        // Empty target slot
        if (to.IsEmpty)
        {
            int move = Mathf.Min(from.quantity, from.item.maxStack);
            to.item = from.item;
            to.quantity = move;
            from.quantity -= move;
            if (from.quantity <= 0) from.Clear();
            OnInventoryChanged?.Invoke();
            return move;
        }

        // Same item: MERGE
        if (to.item == from.item && !to.IsFull)
        {
            int space = to.item.maxStack - to.quantity;
            int move = Mathf.Min(space, from.quantity);
            to.quantity += move;
            from.quantity -= move;
            if (from.quantity <= 0) from.Clear();
            OnInventoryChanged?.Invoke();
            return move;
        }

        // Different item: SWAP
        var tempItem = to.item;
        var tempQty = to.quantity;

        to.item = from.item;
        to.quantity = from.quantity;

        from.item = tempItem;
        from.quantity = tempQty;

        OnInventoryChanged?.Invoke();
        return 0; // Swap didn't "merge" any
    }

    // --- NEW: Merge Inventory -> SPECIFIC Shipping slot ---
    public int MergeInventoryIntoSpecificShipping(int fromInvIndex, int toShipIndex)
    {
        if (fromInvIndex < 0 || fromInvIndex >= slots.Count) return 0;
        if (toShipIndex < 0 || toShipIndex >= shippingSlots.Count) return 0;

        var from = slots[fromInvIndex];
        var to = shippingSlots[toShipIndex];
        if (from.IsEmpty) return 0;

        ItemDefinition item = from.item;

        // Empty slot
        if (to.IsEmpty)
        {
            int move = Mathf.Min(from.quantity, item.maxStack);
            to.item = item;
            to.quantity = move;
            from.quantity -= move;
            if (from.quantity <= 0) from.Clear();
            OnInventoryChanged?.Invoke();
            OnShippingChanged?.Invoke();
            return move;
        }

        // Same item: MERGE
        if (to.item == item && !to.IsFull)
        {
            int space = item.maxStack - to.quantity;
            int move = Mathf.Min(space, from.quantity);
            to.quantity += move;
            from.quantity -= move;
            if (from.quantity <= 0) from.Clear();
            OnInventoryChanged?.Invoke();
            OnShippingChanged?.Invoke();
            return move;
        }

        return 0; // different item present = cannot merge here
    }

    // === Partial transfer: Shipping -> Inventory ===
    public int TransferShippingToInventoryPartial(int fromShipIndex, int toInvIndex, int amount)
    {
        if (amount <= 0) return 0;
        if (fromShipIndex < 0 || fromShipIndex >= shippingSlots.Count) return 0;
        if (toInvIndex < 0 || toInvIndex >= slots.Count) return 0;

        var from = shippingSlots[fromShipIndex];
        var to = slots[toInvIndex];
        if (from.IsEmpty) return 0;

        ItemDefinition item = from.item;

        int canPlace = 0;
        if (to.IsEmpty)
        {
            canPlace = Mathf.Min(item.maxStack, amount);
        }
        else if (to.item == item && !to.IsFull)
        {
            int space = item.maxStack - to.quantity;
            canPlace = Mathf.Min(space, amount);
        }
        else
        {
            return 0;
        }

        if (canPlace <= 0) return 0;
        canPlace = Mathf.Min(canPlace, from.quantity);

        if (to.IsEmpty) { to.item = item; to.quantity = canPlace; }
        else { to.quantity += canPlace; }

        from.quantity -= canPlace;
        if (from.quantity <= 0) from.Clear();

        OnInventoryChanged?.Invoke();
        OnShippingChanged?.Invoke();
        return canPlace;
    }

    // === Partial transfer: Shipping -> Shipping (merge only) ===
    public int TransferShippingPartial(int fromShipIndex, int toShipIndex, int amount)
    {
        if (amount <= 0) return 0;
        if (fromShipIndex == toShipIndex) return 0;
        if (fromShipIndex < 0 || fromShipIndex >= shippingSlots.Count) return 0;
        if (toShipIndex < 0 || toShipIndex >= shippingSlots.Count) return 0;

        var from = shippingSlots[fromShipIndex];
        var to = shippingSlots[toShipIndex];
        if (from.IsEmpty) return 0;

        ItemDefinition item = from.item;

        int canPlace = 0;
        if (to.IsEmpty)
        {
            canPlace = Mathf.Min(item.maxStack, amount);
        }
        else if (to.item == item && !to.IsFull)
        {
            int space = item.maxStack - to.quantity;
            canPlace = Mathf.Min(space, amount);
        }
        else
        {
            return 0;
        }

        if (canPlace <= 0) return 0;
        canPlace = Mathf.Min(canPlace, from.quantity);

        if (to.IsEmpty) { to.item = item; to.quantity = canPlace; }
        else { to.quantity += canPlace; }

        from.quantity -= canPlace;
        if (from.quantity <= 0) from.Clear();

        OnShippingChanged?.Invoke();
        return canPlace;
    }


    // Merge into existing stacks, then first empty. Returns how many units moved.
    public int SmartMoveInventoryToShipping(int invIndex)
    {
        if (invIndex < 0 || invIndex >= slots.Count) return 0;
        var from = slots[invIndex];
        if (from.IsEmpty) return 0;

        int moved = 0;
        ItemDefinition item = from.item;

        // 1) Fill existing stacks
        for (int i = 0; i < shippingSlots.Count && from.quantity > 0; i++)
        {
            var s = shippingSlots[i];
            if (!s.IsEmpty && s.item == item && !s.IsFull)
            {
                int space = item.maxStack - s.quantity;
                int add = Mathf.Min(space, from.quantity);
                s.quantity += add;
                from.quantity -= add;
                moved += add;
            }
        }
        // 2) Fill empties
        for (int i = 0; i < shippingSlots.Count && from.quantity > 0; i++)
        {
            var s = shippingSlots[i];
            if (s.IsEmpty)
            {
                int add = Mathf.Min(item.maxStack, from.quantity);
                s.item = item;
                s.quantity = add;
                from.quantity -= add;
                moved += add;
            }
        }

        if (from.quantity <= 0) from.Clear();

        if (moved > 0)
        {
            OnInventoryChanged?.Invoke();
            OnShippingChanged?.Invoke();
        }
        return moved;
    }

    public int SmartMoveShippingToInventory(int shipIndex)
    {
        if (shipIndex < 0 || shipIndex >= shippingSlots.Count) return 0;
        var from = shippingSlots[shipIndex];
        if (from.IsEmpty) return 0;

        int moved = 0;
        ItemDefinition item = from.item;

        // 1) Fill existing stacks
        for (int i = 0; i < slots.Count && from.quantity > 0; i++)
        {
            var s = slots[i];
            if (!s.IsEmpty && s.item == item && !s.IsFull)
            {
                int space = item.maxStack - s.quantity;
                int add = Mathf.Min(space, from.quantity);
                s.quantity += add;
                from.quantity -= add;
                moved += add;
            }
        }
        // 2) Fill empties
        for (int i = 0; i < slots.Count && from.quantity > 0; i++)
        {
            var s = slots[i];
            if (s.IsEmpty)
            {
                int add = Mathf.Min(item.maxStack, from.quantity);
                s.item = item;
                s.quantity = add;
                from.quantity -= add;
                moved += add;
            }
        }

        if (from.quantity <= 0) from.Clear();

        if (moved > 0)
        {
            OnInventoryChanged?.Invoke();
            OnShippingChanged?.Invoke();
        }
        return moved;
    }

    // Optional: compact both containers by merging identical items across all slots.
    public int AutoStackAll()
    {
        int merged = 0;

        // Helper local function to compact one list
        void Compact(List<InventorySlot> list)
        {
            // Combine counts per item
            var counts = new Dictionary<ItemDefinition, int>();
            foreach (var s in list)
            {
                if (s.IsEmpty) continue;
                if (!counts.ContainsKey(s.item)) counts[s.item] = 0;
                counts[s.item] += s.quantity;
                s.Clear();
            }
            // Refill stacks
            foreach (var kv in counts)
            {
                int remaining = kv.Value;
                var item = kv.Key;

                // Fill to full stacks
                foreach (var s in list)
                {
                    if (remaining <= 0) break;
                    if (!s.IsEmpty) continue;
                    int add = Mathf.Min(item.maxStack, remaining);
                    s.item = item;
                    s.quantity = add;
                    remaining -= add;
                }
            }
        }

        // Snapshot before to estimate merges? We’ll just trigger events.
        Compact(slots);
        Compact(shippingSlots);

        OnInventoryChanged?.Invoke();
        OnShippingChanged?.Invoke();
        return merged;
    }





    // Expose inventory slots (unchanged)
    public IReadOnlyList<InventorySlot> Slots => slots;
}
