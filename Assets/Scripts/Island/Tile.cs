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
        if (crop != null)
            CheckGrowth();
        else
            CheckSoilRevert();
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
        if (state != State.WetSoil || crop != null) return;

        crop = TileManager.I.carrotCrop;
        growthStage = 0;
        lastWaterTime = TimeManager.I.currentDate;

        Debug.Log($"Planted {crop.displayName} at {x},{y}");

        SpawnCropVisual();
    }


    void Water()
    {
        if (state == State.Soil || state == State.WetSoil)
        {
            state = State.WetSoil;
            lastWaterTime = TimeManager.I.currentDate;

            UpdateVisual();

            if (crop != null) // refresh crop look if already planted
                RefreshCropVisual();

            PlayWaterFX();
        }
    }


    // ----------------------------------------------------------
    // Growth System
    // ----------------------------------------------------------

    void CheckGrowth()
    {
        DateTime now = TimeManager.I.currentDate;

        if (lastWaterTime.Date < now.Date)
        {
            lastWaterTime = now;
            growthStage = Mathf.Min(growthStage + 1, crop.StageCount - 1);
            RefreshCropVisual();
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
            growthStage = crop.StageCount - 2;
            SpawnCropVisual();
        }
        else
        {
            crop = null;
            state = State.Soil;
            UpdateVisual();
        }
    }


    // ----------------------------------------------------------
    // Soil Reversion
    // ----------------------------------------------------------

    void CheckSoilRevert()
    {
        DateTime now = TimeManager.I.currentDate;

        if (state == State.Soil &&
            lastShovelTime.Date < now.Date &&
            crop == null)
        {
            state = State.Grass;
            UpdateVisual();
        }

        if (state == State.WetSoil &&
            lastWaterTime.Date < now.Date &&
            crop == null)
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

        currentVisual = Instantiate(
            prefab,
            transform.position,
            Quaternion.identity,
            transform
        );
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

        plantedObject = Instantiate(
            crop.stagePrefabs[growthStage],
            transform.position + Vector3.up * 0.1f,
            Quaternion.identity,
            transform
        );
    }

    void RefreshCropVisual()
    {
        if (plantedObject != null)
            Destroy(plantedObject);

        plantedObject = Instantiate(
            crop.stagePrefabs[growthStage],
            transform.position + Vector3.up * 0.1f,
            Quaternion.identity,
            transform
        );
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
