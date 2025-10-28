using System;
using UnityEngine;

public class TimeManager : MonoBehaviour
{
    public static TimeManager I;

    public DateTime currentDate; // Fake date for crops
    public int DayNumber { get; private set; } = 1;

    public event Action OnDayChanged;

    void Awake()
    {
        I = this;
        currentDate = DateTime.Now.Date;
    }

    public void SkipDay()
    {
        currentDate = currentDate.AddDays(1);
        DayNumber++;

        Debug.Log($"Skipped to {currentDate.ToShortDateString()} — Day {DayNumber}");

        OnDayChanged?.Invoke();
    }
}
