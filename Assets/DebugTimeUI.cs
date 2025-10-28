using UnityEngine;

public class DebugTimeUI : MonoBehaviour
{
    public void SkipDayButton()
    {
        TimeManager.I.SkipDay();
    }
}
