using System;
using System.Collections.Generic;

[Serializable]
public class TileSaveData
{
    public int x;
    public int y;
    public string state;
    public string cropName;
    public int growthStage;
    public float randomYRotation;
}

[Serializable]
public class FarmSaveData
{
    public List<TileSaveData> tiles = new();
    public int dayNumber;
    public int worldSeed;

    public List<PendingSaveData> pending = new();

    //full slot inventory save
    public List<InventorySlotSaveData> inventorySlots = new();
}


[Serializable]
public class InventorySlotSaveData
{
    public string itemID;
    public int quantity;
}


[Serializable]
public class PendingSaveData
{
    public int x;
    public int y;
    public bool hasResource;
    public string type;      // "Tree" or "Rock"
    public int hp;
    public int prefabIndex;  // chosen model index
}
