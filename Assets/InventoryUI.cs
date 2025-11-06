using TMPro;
using UnityEngine;

public class InventoryUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI woodText;
    [SerializeField] private TextMeshProUGUI stoneText;
    [SerializeField] private TextMeshProUGUI carrotText;

    void Awake()
    {
        if (woodText == null)
            woodText = transform.Find("WoodText")?.GetComponent<TextMeshProUGUI>();

        if (stoneText == null)
            stoneText = transform.Find("StoneText")?.GetComponent<TextMeshProUGUI>();

        if (carrotText == null)
            carrotText = transform.Find("CarrotText")?.GetComponent<TextMeshProUGUI>();
    }


    void OnEnable()
    {
        // If InventorySystem hasn’t initialized yet, delay
        if (InventorySystem.I == null)
        {
            Debug.Log("Delaying UI refresh, InventorySystem not ready yet.");
            return;
        }

        InventorySystem.OnResourceChanged += UpdateResource;
        RefreshAll();
    }

    void OnDisable()
    {
        InventorySystem.OnResourceChanged -= UpdateResource;
    }

    void Start()
    {
        RefreshAll();
    }

    void UpdateResource(InventorySystem.ResourceType type, int newAmount)
    {
        switch (type)
        {
            case InventorySystem.ResourceType.Wood:
                woodText.text = $"Wood: {newAmount}";
                break;

            case InventorySystem.ResourceType.Stone:
                stoneText.text = $"Stone: {newAmount}";
                break;

            case InventorySystem.ResourceType.Carrot:
                carrotText.text = $"Carrot: {newAmount}";
                break;
        }
    }

    void RefreshAll()
    {
        if (woodText != null)
            woodText.text = $"Wood: {InventorySystem.Get(InventorySystem.ResourceType.Wood)}";

        if (stoneText != null)
            stoneText.text = $"Stone: {InventorySystem.Get(InventorySystem.ResourceType.Stone)}";

        if (carrotText != null)
            carrotText.text = $"Carrot: {InventorySystem.Get(InventorySystem.ResourceType.Carrot)}";
    }

}
