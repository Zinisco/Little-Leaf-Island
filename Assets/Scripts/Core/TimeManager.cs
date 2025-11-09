using System;
using System.Collections;
using UnityEngine;

public class TimeManager : MonoBehaviour
{
    public static TimeManager I;

    [Header("Clock")]
    [Tooltip("Real-time minutes for one full 24h in-game day.")]
    [SerializeField] private float dayLengthMinutes = 10f;
    [Tooltip("Initial in-game hour on first load (0..24).")]
    [SerializeField] private float startHour = 8f;

    [Header("Sunrise / Sunset")]
    [Tooltip("Hour when a new day starts and crops grow.")]
    [SerializeField] private int sunriseHour = 8;
    [Tooltip("Hour when daytime ends.")]
    [SerializeField] private int sunsetHour = 20;

    // Runtime
    public int DayNumber { get; private set; } = 1;
    public float CurrentTime { get; private set; }           // 0..24
    public bool IsDaytime => CurrentTime >= sunriseHour && CurrentTime < sunsetHour;

    public event Action OnDayChanged;            // fires at sunrise (start of day)
    public event Action<bool> OnPhaseChanged;    // true = day, false = night
    public event Action<float> OnClockTick;      // fires every Update with current time
    public event Action OnSunrise;               // crop growth hook

    float timeScalePerSecond;  // in-game hours per real-time second
    bool paused;

    // --- NEW: fast-forward state ---
    public bool IsFastForwarding { get; private set; }

    void Awake()
    {
        I = this;
        timeScalePerSecond = 24f / Mathf.Max(1f, dayLengthMinutes * 60f);
        CurrentTime = Mathf.Repeat(startHour, 24f);

        // Initial phase notify
        OnDayChanged?.Invoke();
        OnPhaseChanged?.Invoke(IsDaytime);
        if (IsAtOrPastSunrise(CurrentTime)) OnSunrise?.Invoke();
    }

    void Update()
    {
        if (paused || IsFastForwarding)
        {
            // still emit ticks so UI / sun update every frame
            OnClockTick?.Invoke(CurrentTime);
            return;
        }

        float prevTime = CurrentTime;
        bool wasDay = IsDaytime;

        CurrentTime += Time.deltaTime * timeScalePerSecond;
        if (CurrentTime >= 24f) CurrentTime -= 24f;

        // Sunrise crossing (with wrap)
        if (CrossedForward(prevTime, CurrentTime, sunriseHour))
        {
            DayNumber++;
            OnDayChanged?.Invoke();
            OnSunrise?.Invoke();
        }

        bool isDay = IsDaytime;
        if (isDay != wasDay)
            OnPhaseChanged?.Invoke(isDay);

        OnClockTick?.Invoke(CurrentTime);
    }

    // ---------- FAST FORWARD API ----------
    // C1: fully visible fast-forward, ~5s by default, shorter if close to sunrise
    public Coroutine FastForwardToNextSunrise(float baseDurationSeconds = 5f)
        => StartCoroutine(FastForwardRoutine(baseDurationSeconds));

    IEnumerator FastForwardRoutine(float baseDurationSeconds)
    {
        IsFastForwarding = true;

        // How many hours to get to the *next* sunrise (always at least a tiny bit, never 0)
        float hoursToTarget = HoursUntilNextSunrise(CurrentTime);
        if (hoursToTarget <= 0f) hoursToTarget = 24f; // already exactly sunrise ? next day sunrise

        // Scale duration: if we’re very close, go faster (min 0.6s)
        float duration = Mathf.Clamp(baseDurationSeconds * (hoursToTarget / 8f), 0.6f, baseDurationSeconds);
        float elapsed = 0f;
        float startTime = CurrentTime;

        // Smooth speed curve: accelerate, cruise, decelerate
        AnimationCurve curve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        // We’ll manually advance time so Update() doesn’t double-advance.
        // We still emit events (OnClockTick, sunrise/phase) inside the loop.
        while (elapsed < duration)
        {
            float dt = Time.deltaTime;
            elapsed += dt;

            float p = Mathf.Clamp01(elapsed / duration); // 0..1
            float targetOffset = hoursToTarget * curve.Evaluate(p); // 0..hoursToTarget
            float newTime = startTime + targetOffset;

            // Wrap
            float prev = CurrentTime;
            CurrentTime = Mathf.Repeat(newTime, 24f);

            // Fire events during fast-forward so lighting/sun truly animates
            TickEventsDuringManualAdvance(prev, CurrentTime);

            // Let systems update visuals this frame
            OnClockTick?.Invoke(CurrentTime);
            yield return null;
        }

        // Snap exactly to sunrise
        float prevFinal = CurrentTime;
        CurrentTime = Mathf.Repeat(sunriseHour, 24f);

        // If we didn’t cross sunrise in loop (very short duration), fire it now.
        if (!CrossedForward(prevFinal, CurrentTime, sunriseHour))
        {
            DayNumber++;
            OnDayChanged?.Invoke();
            OnSunrise?.Invoke();

            bool wasDay = (prevFinal >= sunriseHour && prevFinal < sunsetHour);
            bool isDay = IsDaytime;
            if (isDay != wasDay) OnPhaseChanged?.Invoke(isDay);
        }

        OnClockTick?.Invoke(CurrentTime);
        IsFastForwarding = false;
    }

    void TickEventsDuringManualAdvance(float prevTime, float nowTime)
    {
        // Handle sunrise crossing while we scrub time
        if (CrossedForward(prevTime, nowTime, sunriseHour))
        {
            DayNumber++;
            OnDayChanged?.Invoke();
            OnSunrise?.Invoke();
        }

        bool wasDay = prevTime >= sunriseHour && prevTime < sunsetHour;
        bool isDay = IsDaytime;
        if (isDay != wasDay) OnPhaseChanged?.Invoke(isDay);
    }

    float HoursUntilNextSunrise(float t)
    {
        // distance moving forward on a 24h loop
        float dist = sunriseHour - t;
        if (dist <= 0f) dist += 24f;
        return dist;
    }

    // Helpers
    static bool CrossedForward(float prev, float now, float thresholdHour)
    {
        if (Mathf.Approximately(prev, now)) return false;
        if (prev <= now) return prev < thresholdHour && now >= thresholdHour;
        return prev < thresholdHour || now >= thresholdHour; // wrapped
    }

    static bool IsAtOrPastSunrise(float t) => t >= 0; // placeholder

    // Public UI helpers
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
