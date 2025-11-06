using TMPro;
using UnityEngine;

public class SeedDisplayUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI seedLabel;

    void Start()
    {
        if (seedLabel == null)
            seedLabel = GetComponent<TextMeshProUGUI>();

        UpdateSeedDisplay();
    }

    public void UpdateSeedDisplay()
    {
        seedLabel.text = $"Seed: {TileManager.I.worldSeed}";
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F3))
            seedLabel.gameObject.SetActive(!seedLabel.gameObject.activeSelf);
    }
}
