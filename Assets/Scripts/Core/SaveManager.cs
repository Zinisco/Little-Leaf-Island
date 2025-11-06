using System.IO;
using System.Collections.Generic;
using UnityEngine;

public class SaveManager : MonoBehaviour
{
    public static SaveManager I;
    string savePath;

    void Awake()
    {
        I = this;
        savePath = Path.Combine(Application.persistentDataPath, "farmSave.json");
    }

    public void SaveFarm()
    {
        FarmSaveData data = new FarmSaveData();
        data.dayNumber = TimeManager.I.DayNumber;
        data.worldSeed = TileManager.I.worldSeed;

        // Save Inventory Slots
        data.inventorySlots = InventorySystem.I.ExportSlots();

        foreach (var tilePair in TileManager.I.GetAllTiles())
        {
            Tile tile = tilePair.Value;
            TileSaveData t = new TileSaveData
            {
                x = tile.x,
                y = tile.y,
                state = tile.state.ToString(),
                cropName = tile.crop != null ? tile.crop.displayName : null,
                growthStage = tile.GetGrowthStage(),
                randomYRotation = tile.GetRandomRotation()
            };
            data.tiles.Add(t);
        }

        data.pending = new List<PendingSaveData>();
        foreach (var kvp in TileManager.I.ExportPending())
            data.pending.Add(kvp);

        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(savePath, json);

        Debug.Log($"Farm saved to {savePath}");
    }

    public void LoadFarm()
    {
        if (!File.Exists(savePath))
        {
            Debug.Log("No save file found, creating new farm.");
            return;
        }

        string json = File.ReadAllText(savePath);
        FarmSaveData data = JsonUtility.FromJson<FarmSaveData>(json);

        TileManager.I.worldSeed = data.worldSeed;

        if (data.pending != null && data.pending.Count > 0)
        {
            TileManager.I.ImportPending(data.pending);
        }

        // Load Inventory Slots
        if (data.inventorySlots != null && data.inventorySlots.Count > 0)
        {
            InventorySystem.I.ImportSlots(
                data.inventorySlots,
                id => ItemDatabase.I.GetItemByID(id)
            );
        }

        Object.FindFirstObjectByType<SeedDisplayUI>()?.UpdateSeedDisplay();

        TimeManager.I.SetDay(data.dayNumber);

        foreach (var t in data.tiles)
        {
            TileManager.I.AddOrRestoreTile(t);
        }

        Debug.Log("Farm loaded successfully!");
    }

}
