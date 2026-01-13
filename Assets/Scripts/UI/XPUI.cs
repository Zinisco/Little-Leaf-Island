using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class XPUI : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] TMP_Text levelText;
    [SerializeField] Slider xpSlider;

    bool subscribed;

    void OnEnable()
    {
        StartCoroutine(BindWhenReady());
    }

    void OnDisable()
    {
        Unbind();
    }

    IEnumerator BindWhenReady()
    {
        // Wait until XPSystem exists (handles script execution order / scene loads)
        while (XPSystem.I == null)
            yield return null;

        Bind();
        Paint(); // initial update
    }

    void Bind()
    {
        if (subscribed || XPSystem.I == null) return;

        XPSystem.I.OnXPChanged += HandleXPChanged;
        XPSystem.I.OnLevelUp += HandleLevelUp;
        subscribed = true;
    }

    void Unbind()
    {
        if (!subscribed || XPSystem.I == null) { subscribed = false; return; }

        XPSystem.I.OnXPChanged -= HandleXPChanged;
        XPSystem.I.OnLevelUp -= HandleLevelUp;
        subscribed = false;
    }

    void Paint()
    {
        if (XPSystem.I == null) return;
        HandleLevelUp(XPSystem.I.Level);
        HandleXPChanged(XPSystem.I.CurrentXP, XPSystem.I.XPToNext);
    }

    void HandleXPChanged(int xp, int xpToNext)
    {
        if (xpToNext <= 0) xpToNext = 1;

        if (xpSlider)
        {
            // normalized 0..1
            xpSlider.minValue = 0f;
            xpSlider.maxValue = 1f;
            xpSlider.value = Mathf.Clamp01((float)xp / xpToNext);
        }
    }

    void HandleLevelUp(int newLevel)
    {
        if (levelText) levelText.text = $"Level {newLevel}";
    }
}
