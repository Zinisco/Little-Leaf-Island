using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class SleepManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Button sleepButton;

    [Header("Options")]
    [Tooltip("Base duration for the skip animation in seconds (scaled shorter if close to sunrise).")]
    [SerializeField] private float baseSkipSeconds = 5f;

    void Awake()
    {
        if (sleepButton != null)
            sleepButton.onClick.AddListener(OnSleepClicked);
    }

    void OnDestroy()
    {
        if (sleepButton != null)
            sleepButton.onClick.RemoveListener(OnSleepClicked);
    }

    void OnSleepClicked()
    {
        if (TimeManager.I == null || TimeManager.I.IsFastForwarding) return;
        StartCoroutine(SleepSequence());
    }

    IEnumerator SleepSequence()
    {
        // (C1) Fully visible world – optionally: disable movement/input here if needed

        // Kick off the visible fast-forward to next sunrise
        yield return TimeManager.I.FastForwardToNextSunrise(baseSkipSeconds);

        // We’ve hit sunrise; new day has been applied and OnSunrise fired
        // Optional: auto-save, replenish stamina, mail, etc.
        // SaveManager.I?.SaveFarm();
    }
}
