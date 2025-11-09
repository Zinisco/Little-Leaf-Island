using System;
using UnityEngine;

public class Tile : MonoBehaviour
{
    public int x, y;
    public enum State { Grass, Soil, WetSoil, Decor }

    public State state = State.Grass;

    GameObject highlightInstance;
    [HideInInspector] public GameObject currentVisual;


    [Header("Crop State")]
    public CropDefinition crop;    // what crop is planted here?
    int growthStage = 0;           // which visual stage index
    int lastShovelDay;   // last in-game day shovel was used
    int lastWaterDay;    // last in-game day tile was watered


    GameObject plantedObject = null;
    Transform cropAnchor;
    float randomYRotation;
    int lastGrowthDayNumber;  // tracks which in-game day we last grew


    public void OnClicked()
    {
        if (!ToolManager.I) return;

        switch (ToolManager.I.currentTool)
        {
            case ToolManager.Tool.Selection:
                TryHarvest();
                break;

            case ToolManager.Tool.Shovel:
                Shovel();
                break;

            case ToolManager.Tool.Water:
                Water();
                break;

            case ToolManager.Tool.Seed:
                PlantSeed();
                break;
        }
    }


    void Update()
    {

    }

    void OnEnable()
    {
        if (TimeManager.I != null)
            TimeManager.I.OnSunrise += OnSunriseTick;
    }

    void OnDisable()
    {
        if (TimeManager.I != null)
            TimeManager.I.OnSunrise -= OnSunriseTick;
    }

    void OnSunriseTick()
    {
        // Dry wet soil each new day
        if (state == State.WetSoil)
        {
            state = State.Soil;
            UpdateVisual();
        }

        // Grow one stage if soil was wet yesterday (simple rule: be wet at sunrise)
        if (crop != null && /* was wet last day? your flag if you track it */ true)
        {
            growthStage = Mathf.Min(growthStage + 1, crop.StageCount - 1);
            RefreshCropVisual();
            Debug.Log($"Crop at {x},{y} grew at sunrise to stage {growthStage}");
        }
    }

    public void Shovel()
    {
        if (state != State.Grass) return;

        state = State.Soil;
        UpdateCropAnchor();
        lastShovelDay = TimeManager.I.DayNumber;
        UpdateVisual();
        PlayHoeFX();
    }


    void PlantSeed()
    {
        // allow planting on both dry and wet soil
        if ((state != State.Soil && state != State.WetSoil) || crop != null)
            return;

        // Require 1 carrot seed
        if (!InventorySystem.TrySpend("carrot_seed", 1))
        {
            Debug.Log("No seeds left!");
            return;
        }

        crop = TileManager.I.carrotCrop;
        growthStage = 0;
        lastGrowthDayNumber = TimeManager.I.DayNumber;
        lastWaterDay = TimeManager.I.DayNumber;

        Debug.Log($"Planted {crop.displayName} at {x},{y}");

        if (currentVisual != null)
            Destroy(currentVisual);

        GameObject seededPrefab = (state == State.WetSoil)
            ? TileManager.I.seededSoilWetPrefab
            : TileManager.I.seededSoilDryPrefab;

        currentVisual = Instantiate(
            seededPrefab,
            transform.position,
            Quaternion.identity,
            transform
        );

        UpdateCropAnchor();
        SpawnCropVisual();
    }



    void Water()
    {
        if (state == State.Soil || state == State.WetSoil)
        {
            state = State.WetSoil;
            lastWaterDay = TimeManager.I.DayNumber;

            // If there’s a crop planted, use wet seeded soil
            if (crop != null)
            {
                if (currentVisual != null)
                    Destroy(currentVisual);

                currentVisual = Instantiate(
                    TileManager.I.seededSoilWetPrefab,
                    transform.position,
                    Quaternion.identity,
                    transform
                );

                UpdateCropAnchor();
                RefreshCropVisual();
            }
            else
            {
                UpdateVisual();
            }

            PlayWaterFX();
        }
    }


    void TryHarvest()
    {
        if (crop == null) return;
        if (growthStage < crop.StageCount - 1) return;

        // ---- Normal output ----
        bool gaveNormal = false;
        if (!crop.rareReplaces || crop.rareItem == null)
        {
            if (crop.outputItem != null && crop.outputQuantity > 0)
            {
                InventorySystem.I.AddItem(crop.outputItem, crop.outputQuantity);
                gaveNormal = true;
            }
        }

        // ---- Rare Harvest ----
        if (crop.hasRareHarvest && crop.rareItem != null && crop.rareChance > 0f)
        {
            if (UnityEngine.Random.Range(0f, 100f) <= crop.rareChance)
            {
                if (crop.rareReplaces)
                {
                    // Replace normal output (but since we didn't give normal when rareReplaces=true, no need to remove)
                    InventorySystem.I.AddItem(crop.rareItem, 1);
                }
                else
                {
                    // Add rare bonus item
                    InventorySystem.I.AddItem(crop.rareItem, 1);
                }
            }
        }

        // Remove visuals
        if (plantedObject) Destroy(plantedObject);

        // Regrow or reset soil
        if (crop.regrows)
        {
            growthStage = Mathf.Max(0, crop.StageCount - 2);
            SpawnCropVisual();
        }
        else
        {
            crop = null;
            state = State.Soil;
            lastShovelDay = TimeManager.I.DayNumber;

            if (currentVisual != null) Destroy(currentVisual);
            currentVisual = Instantiate(
                TileManager.I.soilPrefab,
                transform.position,
                Quaternion.identity,
                transform
            );
            UpdateCropAnchor();
        }

        Debug.Log($"Harvested at {x},{y}. NormalGiven={gaveNormal}");
    }



    // ----------------------------------------------------------
    // Soil Reversion
    // ----------------------------------------------------------

    void CheckSoilRevert()
    {
        int currentDay = TimeManager.I.DayNumber;

        if (state == State.Soil && lastShovelDay < currentDay)
        {
            state = State.Grass;
            UpdateVisual();
        }

        if (state == State.WetSoil && lastWaterDay < currentDay)
        {
            state = State.Soil;
            UpdateVisual();
        }

    }


    // ----------------------------------------------------------
    // Visual Helpers
    // ----------------------------------------------------------



    public void UpdateVisual()
    {
        if (currentVisual != null)
            Destroy(currentVisual);

        GameObject prefab =
    state == State.Decor ? TileManager.I.decorGrassPrefab :
    state == State.WetSoil ? TileManager.I.wetSoilPrefab :
    state == State.Soil ? TileManager.I.soilPrefab :
    TileManager.I.grassPrefab;


        currentVisual = Instantiate(prefab, transform.position, Quaternion.identity, transform);

        UpdateCropAnchor();
    }



    public void ShowHighlight(GameObject highlightPrefab)
    {
        if (highlightInstance) return;

        MeshRenderer mr = GetComponentInChildren<MeshRenderer>();
        float topY = mr.bounds.max.y;

        highlightInstance = Instantiate(
            highlightPrefab,
            new Vector3(transform.position.x, topY + 0.02f, transform.position.z),
            Quaternion.identity,
            transform
        );
    }


    public void InitializeVisual(GameObject visual)
    {
        currentVisual = visual;
    }


    public void HideHighlight()
    {
        if (!highlightInstance) return;
        Destroy(highlightInstance);
        highlightInstance = null;
    }

    void SpawnCropVisual()
    {
        if (plantedObject != null)
            Destroy(plantedObject);

        Vector3 spawnPos = (cropAnchor != null)
            ? cropAnchor.position
            : transform.position + Vector3.up * 0.1f;

        if (growthStage == 0)
            randomYRotation = UnityEngine.Random.Range(0f, 360f);

        Quaternion randomRot = Quaternion.Euler(0, randomYRotation, 0);

        plantedObject = Instantiate(
            crop.stagePrefabs[growthStage],
            spawnPos,
            randomRot,
            transform
        );
    }



    void RefreshCropVisual()
    {
        if (plantedObject != null)
            Destroy(plantedObject);

        if (cropAnchor == null)      // attempt recovery
            UpdateCropAnchor();

        Vector3 spawnPos = (cropAnchor != null)
            ? cropAnchor.position
            : transform.position + Vector3.up * 0.1f;

        // Keep the same stored rotation across all stages
        Quaternion randomRot = Quaternion.Euler(0, randomYRotation, 0);

        plantedObject = Instantiate(
            crop.stagePrefabs[growthStage],
            spawnPos,
            randomRot,
            transform
        );
    }



   public void UpdateCropAnchor()
    {
        cropAnchor = null;
        if (currentVisual != null)
        {
            // try direct child first
            var direct = currentVisual.transform.Find("CropAnchor");
            if (direct != null) { cropAnchor = direct; return; }

            // fallback: search all children (handles nested anchors)
            foreach (var t in currentVisual.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == "CropAnchor") { cropAnchor = t; break; }
            }
        }
    }

    public void SetGrowthStage(int stage, float rotation)
    {
        growthStage = stage;
        randomYRotation = rotation;
        SpawnCropVisual();
    }


    void PlayHoeFX()
    {
        if (TileManager.I.hoeFXPrefab != null)
        {
            Quaternion rot = Quaternion.LookRotation(Vector3.up);
            ParticleSystem fx = Instantiate(
                TileManager.I.hoeFXPrefab,
                transform.position + Vector3.up * 0.1f,
                rot
            );

            fx.Play();
            Destroy(fx.gameObject, 2f);
        }

        if (TileManager.I.hoeSound != null)
        {
            AudioSource.PlayClipAtPoint(
                TileManager.I.hoeSound,
                transform.position,
                TileManager.I.hoeSoundVolume
            );
        }
    }

    void PlayWaterFX()
    {
        if (TileManager.I.waterFXPrefab != null)
        {
            // Spawn above the tile so droplets fall down onto it
            Vector3 spawnPos = transform.position + Vector3.up * 2.5f;

            // Look straight down (so cone spray points downward)
            Quaternion rot = Quaternion.LookRotation(Vector3.down);

            ParticleSystem fx = Instantiate(
                TileManager.I.waterFXPrefab,
                spawnPos,
                rot
            );

            fx.Play();
            Destroy(fx.gameObject, 2f);
        }

        if (TileManager.I.waterSound != null)
        {
            AudioSource.PlayClipAtPoint(
                TileManager.I.waterSound,
                transform.position,
                TileManager.I.waterSoundVolume
            );
        }
    }

    public int GetGrowthStage() => growthStage;
    public float GetRandomRotation() => randomYRotation;


}
