using UnityEngine;
using TMPro;

public class DayUI : MonoBehaviour
{
    [SerializeField] TMP_Text dayText;

    void Start()
    {
        if (TimeManager.I != null)
            TimeManager.I.OnDayChanged += UpdateDayLabel;

        UpdateDayLabel();
    }

    void OnDisable()
    {
        if (TimeManager.I != null)
            TimeManager.I.OnDayChanged -= UpdateDayLabel;
    }

    void UpdateDayLabel()
    {
        int dayNumber = TimeManager.I.DayNumber;
        dayText.text = $"Day {dayNumber:00}";
    }
}
