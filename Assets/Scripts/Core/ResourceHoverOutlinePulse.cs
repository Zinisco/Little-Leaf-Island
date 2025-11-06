using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ResourceHoverGlowOverlay : MonoBehaviour
{
    [Header("Overlay Glow Material (Assign M_ToonGlowOverlay)")]
    public Material overlayGlowMaterial;

    [Header("Scale Pulse")]
    [Range(1.0f, 1.2f)] public float hoverScale = 1.06f;
    [Range(0.05f, 0.4f)] public float pulseDuration = 0.12f;

    Renderer[] renderers;
    Dictionary<Renderer, Material[]> originalMats = new Dictionary<Renderer, Material[]>();

    Transform target;
    Vector3 baseScale;
    Coroutine scaleRoutine;

    void Awake()
    {
        renderers = GetComponentsInChildren<Renderer>(true);

        // Store original materials
        foreach (var r in renderers)
        {
            originalMats[r] = r.sharedMaterials;
        }

        target = transform;
        baseScale = target.localScale;
    }

    void OnMouseEnter()
    {
        if (overlayGlowMaterial == null) return;

        AddOverlay();
        StartScaleTo(hoverScale);
    }

    void OnMouseExit()
    {
        RemoveOverlay();
        StartScaleTo(1f);
    }

    void OnDisable()
    {
        RemoveOverlay();
        if (scaleRoutine != null) StopCoroutine(scaleRoutine);
        target.localScale = baseScale;
    }

    void AddOverlay()
    {
        foreach (var r in renderers)
        {
            var mats = new List<Material>(r.sharedMaterials);
            if (!mats.Contains(overlayGlowMaterial))
            {
                mats.Add(overlayGlowMaterial);
                r.materials = mats.ToArray(); // Assign runtime instance
            }
        }
    }

    void RemoveOverlay()
    {
        foreach (var r in renderers)
        {
            if (originalMats.TryGetValue(r, out var orig))
                r.sharedMaterials = orig; // restore clean
        }
    }

    void StartScaleTo(float targetMul)
    {
        if (scaleRoutine != null) StopCoroutine(scaleRoutine);
        scaleRoutine = StartCoroutine(ScaleToRoutine(targetMul));
    }

    IEnumerator ScaleToRoutine(float targetMul)
    {
        Vector3 start = target.localScale;
        Vector3 end = baseScale * targetMul;
        float t = 0f;

        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / pulseDuration;
            float k = Mathf.SmoothStep(0f, 1f, t);
            target.localScale = Vector3.Lerp(start, end, k);
            yield return null;
        }

        target.localScale = end;
    }
}
