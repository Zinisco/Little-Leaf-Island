using System.Collections;
using UnityEngine;

public class ResourceNode : MonoBehaviour
{
    public enum ResourceType { Tree, Rock }

    public ResourceType type;
    public int hitPoints = 3;

    [Header("FX")]
    public ParticleSystem hitParticles;        // small burst on hit
    public ParticleSystem breakParticles;      // bigger burst on destroy
    public AudioClip hitSound;
    public AudioClip breakSound;
    [Range(0f, 1f)] public float hitVolume = 0.6f;
    [Range(0f, 1f)] public float breakVolume = 0.8f;

    [Header("Shake")]
    [Range(0f, 0.15f)] public float shakeAmplitude = 0.045f;
    [Range(0.01f, 0.25f)] public float shakeDuration = 0.08f;

    Transform shakeTarget;
    Vector3 baseWorldPos;

    void Awake()
    {
        shakeTarget = transform;
    }

    void Start()
    {
        baseWorldPos = shakeTarget.position; // capture after TileManager moves it
    }


    public void Hit()
    {
        hitPoints--;

        PlayHitFX();

        if (hitPoints <= 0)
            Harvest();
    }

    void PlayHitFX()
    {
        if (hitParticles != null)
        {
            var fx = Instantiate(hitParticles, GetHitSpawnPoint(), Quaternion.identity);
            fx.Play();
            Destroy(fx.gameObject, 2f);
        }

        if (hitSound != null)
            AudioSource.PlayClipAtPoint(hitSound, transform.position, hitVolume);

        // tiny shake
        StopAllCoroutines();
        StartCoroutine(ShakeRoutine());
    }

    Vector3 GetHitSpawnPoint()
    {
        var r = GetComponentInChildren<Renderer>();
        if (r == null) return transform.position + Vector3.up * 0.5f;
        return new Vector3(r.bounds.center.x, r.bounds.max.y, r.bounds.center.z);
    }

    IEnumerator ShakeRoutine()
    {
        float t = 0f;

        // Cache the fixed position at start of shake
        Vector3 startPos = baseWorldPos;

        while (t < shakeDuration)
        {
            t += Time.deltaTime;
            float k = 1f - Mathf.Clamp01(t / shakeDuration);

            // Side-to-side shake only (no Y movement)
            Vector3 offset = new Vector3(
                (Mathf.PerlinNoise(0, Time.time * 60f) - 0.5f) * shakeAmplitude,
                0f,
                (Mathf.PerlinNoise(1, Time.time * 60f) - 0.5f) * shakeAmplitude
            ) * k;

            shakeTarget.position = startPos + offset;
            yield return null;
        }

        // Reset perfectly to original spot
        shakeTarget.position = baseWorldPos;
    }

    void Harvest()
    {
        if (breakParticles != null)
        {
            var fx = Instantiate(breakParticles, GetHitSpawnPoint(), Quaternion.identity);
            fx.Play();
            Destroy(fx.gameObject, 2.5f);
        }

        if (breakSound != null)
            AudioSource.PlayClipAtPoint(breakSound, transform.position, breakVolume);

        if (type == ResourceType.Tree)
        {
            InventorySystem.AddWood(1 + Random.Range(1, 3)); // 1–3 wood
        }
        else if (type == ResourceType.Rock)
        {
            InventorySystem.AddStone(1 + Random.Range(1, 2)); // 1–2 stone
        }

        Destroy(gameObject);
    }
}
