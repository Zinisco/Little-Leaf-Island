using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ResourceHoverOutlinePulse : MonoBehaviour
{
    [Header("Outline")]
    public Material outlineGlowMaterial;          // assign your O3 glow material in Inspector
    [Range(0.01f, 0.25f)] public float outlineYOffset = 0.05f;

    [Header("Pulse")]
    [Range(1.0f, 1.2f)] public float hoverScale = 1.06f;  // P2 ~ 6%
    [Range(0.05f, 0.4f)] public float pulseDuration = 0.12f;

    Renderer[] renderers;
    // Cache original materials per renderer so we can cleanly restore
    Dictionary<Renderer, Material[]> originalMats = new Dictionary<Renderer, Material[]>();

    Transform target;          // what we scale, defaults to this.transform
    Vector3 baseScale;
    Coroutine scaleRoutine;

    void Awake()
    {
        renderers = GetComponentsInChildren<Renderer>(true);
        foreach (var r in renderers)
            originalMats[r] = r.sharedMaterials;   // shared to avoid runtime instantiation here

        target = transform;
        baseScale = target.localScale;
    }

    void OnMouseEnter()
    {
        // No hover while in expansion mode
        if (ExpansionModeManager.I != null && ExpansionModeManager.I.IsActive) return;

        ApplyOutline(true);
        StartScaleTo(hoverScale);

        // Optional: place a ring if you still want a top indicator
        // Shift object up slightly only if you use a ring mesh. Otherwise skip.
        // var top = GetTopY();
        // if (ringPrefab) Instantiate(ringPrefab, new Vector3(transform.position.x, top + outlineYOffset, transform.position.z), Quaternion.identity, transform);
    }

    void OnMouseExit()
    {
        ApplyOutline(false);
        StartScaleTo(1f);
    }

    void OnDisable()
    {
        // defensive restore
        ApplyOutline(false);
        if (scaleRoutine != null) StopCoroutine(scaleRoutine);
        target.localScale = baseScale;
    }

    void ApplyOutline(bool on)
    {
        if (outlineGlowMaterial == null) return;

        if (on)
        {
            foreach (var r in renderers)
            {
                var mats = new List<Material>(r.materials); // instanced at runtime
                // Avoid duplicates
                if (!mats.Contains(outlineGlowMaterial))
                {
                    mats.Add(outlineGlowMaterial);
                    r.materials = mats.ToArray();
                }
            }
        }
        else
        {
            foreach (var r in renderers)
            {
                // restore to original shared materials to keep things clean
                if (originalMats.TryGetValue(r, out var orig))
                    r.sharedMaterials = orig;
            }
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

    float GetTopY()
    {
        float top = transform.position.y;
        foreach (var r in renderers)
        {
            if (r != null) top = Mathf.Max(top, r.bounds.max.y);
        }
        return top;
    }
}
