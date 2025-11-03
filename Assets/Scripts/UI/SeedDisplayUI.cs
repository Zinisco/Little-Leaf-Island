using TMPro;
using UnityEngine;

public class SeedDisplayUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI seedLabel;

    void Start()
    {
        if (seedLabel == null)
        {
            seedLabel = GetComponent<TextMeshProUGUI>();
        }

        seedLabel.text = $"Seed: {TileManager.I.worldSeed}";
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F3))
        {
            gameObject.SetActive(!gameObject.activeSelf);
        }
    }
}
