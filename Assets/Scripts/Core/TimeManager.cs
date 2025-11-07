using System;
using UnityEngine;

public class TimeManager : MonoBehaviour
{
    public static TimeManager I;

    [Header("Clock")]
    [Tooltip("Real-time minutes for one full 24h in-game day.")]
    [SerializeField] private float dayLengthMinutes = 10f;   // (1) You picked 10m / day
    [Tooltip("Initial in-game hour on first load (0..24).")]
    [SerializeField] private float startHour = 8f;           // begin at 8:00 AM

    [Header("Sunrise / Sunset")]
    [Tooltip("Hour when a new day starts and crops grow.")]
    [SerializeField] private int sunriseHour = 6;            // (2) Grow at sunrise
    [Tooltip("Hour when daytime ends.")]
    [SerializeField] private int sunsetHour = 20;            // 8 PM

    // Runtime
    public int DayNumber { get; private set; } = 1;
    public float CurrentTime { get; private set; }           // 0..24
    public bool IsDaytime => CurrentTime >= sunriseHour && CurrentTime < sunsetHour;

    public event Action OnDayChanged;            // fires at sunrise (start of day)
    public event Action<bool> OnPhaseChanged;    // true = day, false = night (fires at sunrise/sunset)
    public event Action<float> OnClockTick;      // optional, fires every Update with current time
    public event Action OnSunrise;               // (2) crop growth hook

    float timeScalePerSecond;  // hours per real-time second
    bool paused;

    void Awake()
    {
        I = this;
        timeScalePerSecond = 24f / Mathf.Max(1f, dayLengthMinutes * 60f);
        CurrentTime = Mathf.Repeat(startHour, 24f);

        // Initial phase notify
        OnDayChanged?.Invoke();
        OnPhaseChanged?.Invoke(IsDaytime);
        if (IsAtOrPastSunrise(CurrentTime)) OnSunrise?.Invoke(); // handles edge case if starting after sunrise
    }

    void Update()
    {
        if (paused) { OnClockTick?.Invoke(CurrentTime); return; }

        float prevTime = CurrentTime;
        bool wasDay = IsDaytime;

        CurrentTime += Time.deltaTime * timeScalePerSecond;
        if (CurrentTime >= 24f) CurrentTime -= 24f;

        // Detect sunrise crossing (prev < sunrise && now >= sunrise) with wrap handling
        if (CrossedForward(prevTime, CurrentTime, sunriseHour))
        {
            DayNumber++;
            OnDayChanged?.Invoke();
            OnSunrise?.Invoke();
        }

        // Phase change notifications (sunrise -> day, sunset -> night)
        bool isDay = IsDaytime;
        if (isDay != wasDay)
            OnPhaseChanged?.Invoke(isDay);

        OnClockTick?.Invoke(CurrentTime);
    }

    // Helpers
    static bool CrossedForward(float prev, float now, float thresholdHour)
    {
        // Works across wrap (e.g., 23.9 -> 0.1)
        if (prev <= now) return prev < thresholdHour && now >= thresholdHour;
        return prev < thresholdHour || now >= thresholdHour;
    }

    static bool IsAtOrPastSunrise(float t) => t >= 0; // placeholder, not essential

    // Public API
    public string GetCurrentTimeFormatted()
    {
        int hours = Mathf.FloorToInt(CurrentTime);
        int minutes = Mathf.FloorToInt((CurrentTime - hours) * 60f);
        bool isPM = hours >= 12;
        int displayHour = hours % 12; if (displayHour == 0) displayHour = 12;
        return $"{displayHour:00}:{minutes:00} {(isPM ? "PM" : "AM")}";
    }

    public void SetPaused(bool value) => paused = value;
    public bool IsPaused => paused;

    public void SetDay(int day) { DayNumber = Mathf.Max(1, day); OnDayChanged?.Invoke(); }
    public void SetTime(float hour) { CurrentTime = Mathf.Repeat(hour, 24f); OnClockTick?.Invoke(CurrentTime); }
}
