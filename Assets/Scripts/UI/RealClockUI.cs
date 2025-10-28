using UnityEngine;
using TMPro;
using System;

public class RealClockUI : MonoBehaviour
{
    [SerializeField] TMP_Text clockText;

    void Update()
    {
        clockText.text = DateTime.Now.ToString("MMM dd, yyyy  hh:mm tt");
    }
}
