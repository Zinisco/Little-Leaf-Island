using UnityEngine;
using TMPro;

public class TestUIController : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text dayLabel;   // Single TMP label showing all info
    public UnityEngine.UI.Button saveButton;
    public UnityEngine.UI.Button loadButton;
    public UnityEngine.UI.Button skipDayButton;

    float timer = 0f;

    void Start()
    {
        // Hook up button clicks
        saveButton.onClick.AddListener(() => SaveManager.I.SaveFarm());
        loadButton.onClick.AddListener(() => SaveManager.I.LoadFarm());
        skipDayButton.onClick.AddListener(() => TimeManager.I.SkipDay());

        // Subscribe to time updates
        TimeManager.I.OnDayChanged += UpdateDayLabel;

        UpdateDayLabel();
    }

    void OnDestroy()
    {
        if (TimeManager.I != null)
            TimeManager.I.OnDayChanged -= UpdateDayLabel;
    }

    void Update()
    {
        // Update every 1 second
        timer += Time.deltaTime;
        if (timer >= 1f)
        {
            timer = 0f;
            UpdateDayLabel();
        }
    }

    void UpdateDayLabel()
    {
        if (dayLabel == null) return;

        string dateStr = $"{TimeManager.I.currentDate:MMMM dd, yyyy}";
        string dayNumStr = $"Day {TimeManager.I.DayNumber}";
        string timeStr = TimeManager.I.GetCurrentTimeFormatted();

        // Order: Date -> Day Number -> Time
        dayLabel.text = $"{dateStr}\n{dayNumStr}\n{timeStr}";
    }
}
