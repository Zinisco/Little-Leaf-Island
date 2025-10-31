using System;
using UnityEngine;

public class TimeManager : MonoBehaviour
{
    public static TimeManager I;

    public DateTime currentDate;  // always real-world date
    public int DayNumber { get; private set; } = 1;

    public event Action OnDayChanged;

    void Awake()
    {
        I = this;
        currentDate = DateTime.Now.Date; // today's date
    }

    void Update()
    {
        // Keep date synced with real world
        currentDate = DateTime.Now.Date;
    }

    public string GetCurrentTimeFormatted()
    {
        // returns something like "10:24 AM"
        return DateTime.Now.ToString("hh:mm tt");
    }

    public void SkipDay()
    {
        DayNumber++;
        Debug.Log($"Advanced to Day {DayNumber} (real date: {currentDate:MMM dd, yyyy})");
        OnDayChanged?.Invoke();
    }

    public void SetDay(int dayNumber)
    {
        DayNumber = dayNumber;
        currentDate = DateTime.Now.Date;
        OnDayChanged?.Invoke();
    }
}
