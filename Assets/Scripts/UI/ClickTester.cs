using UnityEngine;
using UnityEngine.EventSystems;

public class ClickTester : MonoBehaviour, IPointerClickHandler
{
    public void OnPointerClick(PointerEventData e)
    {
        Debug.Log("ClickTester got " + e.button + " on " + gameObject.name);
    }
}
