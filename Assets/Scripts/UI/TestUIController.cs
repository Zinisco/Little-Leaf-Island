using UnityEngine;
using TMPro;

public class TestUIController : MonoBehaviour
{
    public TMP_Text dayLabel;

    float timer;

    void Start()
    {
        var t = TimeManager.I;
        if (t == null) return;

        t.OnDayChanged += Refresh;
        t.OnPhaseChanged += _ => Refresh();
        t.OnClockTick += _ => { /* optional per-frame if you want smooth time */ };

        Refresh();
    }

    void OnDestroy()
    {
        var t = TimeManager.I;
        if (t == null) return;
        t.OnDayChanged -= Refresh;
        t.OnPhaseChanged -= _ => Refresh();
        t.OnClockTick -= _ => { };
    }

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= 1f) { timer = 0f; Refresh(); }
    }

    void Refresh()
    {
        if (dayLabel == null || TimeManager.I == null) return;

        string phase = TimeManager.I.IsDaytime ? "Day" : "Night";
        dayLabel.text = $"Day {TimeManager.I.DayNumber} — {TimeManager.I.GetCurrentTimeFormatted()} ({phase}){(TimeManager.I.IsPaused ? " (Paused)" : "")}";
    }
}
