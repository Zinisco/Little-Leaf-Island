using System;
using UnityEngine;

public class Tile : MonoBehaviour
{
    public int x, y;
    public enum State { Grass, Soil, WetSoil }

    public State state = State.Grass;

    DateTime lastShovelTime;
    public double revertAfterRealDays = 1.0; // 1 real life day

    GameObject highlightInstance;
    GameObject currentVisual;  

    [Header("Crop State")]
    public CropDefinition crop;    // what crop is planted here?
    int growthStage = 0;           // which visual stage index
    DateTime lastWaterTime;        // last day watered

    GameObject plantedObject = null;
    Transform cropAnchor;
    float randomYRotation;


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
        if (crop != null)
            CheckGrowth();
        else
            CheckSoilRevert();
    }

    void OnEnable()
    {
        TimeManager.I.OnDayChanged += CheckDayUpdate;
    }

    void OnDisable()
    {
        if (TimeManager.I != null)
            TimeManager.I.OnDayChanged -= CheckDayUpdate;
    }

    void CheckDayUpdate()
    {
        // Let the crop try to grow first
        if (crop != null)
            CheckGrowth();
        else
            CheckSoilRevert();

        // After growth checks, dry any wet soil
        if (state == State.WetSoil)
        {
            state = State.Soil;

            // Pick correct visual based on whether a crop exists
            if (crop != null)
            {
                if (currentVisual != null)
                    Destroy(currentVisual);

                currentVisual = Instantiate(
                    TileManager.I.seededSoilDryPrefab,
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

            Debug.Log($"Soil at {x},{y} dried out for the new day.");
        }
    }



    public void Shovel()
    {
        if (state != State.Grass) return;

        state = State.Soil;
        lastShovelTime = TimeManager.I.currentDate;
        UpdateVisual();
        PlayHoeFX();
    }


    void PlantSeed()
    {
        // allow planting on both dry and wet soil
        if ((state != State.Soil && state != State.WetSoil) || crop != null)
            return;

        crop = TileManager.I.carrotCrop;
        growthStage = 0;
        lastWaterTime = TimeManager.I.currentDate;

        Debug.Log($"Planted {crop.displayName} at {x},{y}");

        // Replace soil with correct seeded soil visual
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

        // Spawn crop on top using the new anchor
        SpawnCropVisual();

    }


    void Water()
    {
        if (state == State.Soil || state == State.WetSoil)
        {
            state = State.WetSoil;
            lastWaterTime = TimeManager.I.currentDate;

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



    // ----------------------------------------------------------
    // Growth System
    // ----------------------------------------------------------

    void CheckGrowth()
    {
        DateTime now = TimeManager.I.currentDate;

        // Only grow if the soil is still wet today
        if (state != State.WetSoil)
        {
            Debug.Log($"Crop at {x},{y} did not grow today — soil is dry!");
            return;
        }

        // Prevent multiple growths per day
        if (lastWaterTime.Date < now.Date)
        {
            lastWaterTime = now;
            growthStage = Mathf.Min(growthStage + 1, crop.StageCount - 1);
            RefreshCropVisual();

            Debug.Log($"Crop at {x},{y} grew to stage {growthStage}");
        }
    }


    void TryHarvest()
    {
        if (crop == null) return;
        if (growthStage < crop.StageCount - 1) return;

        Debug.Log($"Harvested {crop.displayName} at {x},{y}! +{crop.sellPrice} coins");

        EconomySystem.I.AddCoins(crop.sellPrice);
        Destroy(plantedObject);

        if (crop.regrows)
        {
            // regrow crops drop to one stage before mature
            growthStage = crop.StageCount - 2;
            SpawnCropVisual();
        }
        else
        {
            // Reset soil, not grass
            crop = null;
            state = State.Soil;
            lastShovelTime = TimeManager.I.currentDate;

            // Show plain soil (not seeded)
            if (currentVisual != null)
                Destroy(currentVisual);

            currentVisual = Instantiate(
                TileManager.I.soilPrefab,
                transform.position,
                Quaternion.identity,
                transform
            );

            UpdateCropAnchor();

            Debug.Log($"Tile at {x},{y} reverted to soil after harvest.");
        }
    }




    // ----------------------------------------------------------
    // Soil Reversion
    // ----------------------------------------------------------

    void CheckSoilRevert()
    {
        DateTime now = TimeManager.I.currentDate;

        // Only revert if no crop
        if (crop != null) return;

        if (state == State.Soil && lastShovelTime.Date < now.Date)
        {
            state = State.Grass;
            UpdateVisual();
        }

        if (state == State.WetSoil && lastWaterTime.Date < now.Date)
        {
            state = State.Soil;
            UpdateVisual();
        }
    }



    // ----------------------------------------------------------
    // Visual Helpers
    // ----------------------------------------------------------



    void UpdateVisual()
    {
        if (currentVisual != null)
            Destroy(currentVisual);

        GameObject prefab = (state == State.WetSoil ? TileManager.I.wetSoilPrefab :
                             state == State.Soil ? TileManager.I.soilPrefab :
                             TileManager.I.grassPrefab);

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



    void UpdateCropAnchor()
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



}
