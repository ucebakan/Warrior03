using System.Collections;
using UnityEngine;

public class PlayerDamageShake : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform visualRoot;
    [SerializeField] private AudioSource audioSource;

    [Header("Shake Settings")]
    [SerializeField] private float shakeDuration = 0.12f;
    [SerializeField] private float shakeMagnitudeX = 0.06f;
    [SerializeField] private float shakeMagnitudeY = 0.03f;
    [SerializeField] private float shakeMagnitudeZ = 0.06f;
    [SerializeField] private float minTriggerInterval = 0.08f;

    [Header("Hurt SFX")]
    [SerializeField] private AudioClip hurtSFX;
    [SerializeField][Range(0f, 1f)] private float hurtVolume = 1f;

    private Vector3 originalLocalPosition;
    private Coroutine shakeCoroutine;
    private float lastTriggerTime = -999f;

    private void Awake()
    {
        if (visualRoot == null)
        {
            Animator foundAnimator = GetComponentInChildren<Animator>();
            if (foundAnimator != null)
            {
                visualRoot = foundAnimator.transform;
            }
        }

        if (visualRoot != null)
        {
            originalLocalPosition = visualRoot.localPosition;
        }

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = 0f;
    }

    private void LateUpdate()
    {
        if (visualRoot == null) return;

        if (shakeCoroutine == null)
        {
            visualRoot.localPosition = originalLocalPosition;
        }
    }

    public void TakeDamage(int damageAmount)
    {
        TriggerDamageFeedback();
    }

    public void TakeDamage(float damageAmount)
    {
        TriggerDamageFeedback();
    }

    public void PlayDamageShake()
    {
        TriggerDamageFeedback();
    }

    private void TriggerDamageFeedback()
    {
        if (visualRoot == null) return;

        if (Time.time < lastTriggerTime + minTriggerInterval)
            return;

        lastTriggerTime = Time.time;

        PlayHurtSFX();

        if (shakeCoroutine != null)
        {
            StopCoroutine(shakeCoroutine);
            visualRoot.localPosition = originalLocalPosition;
        }

        shakeCoroutine = StartCoroutine(ShakeRoutine());
    }

    private void PlayHurtSFX()
    {
        if (audioSource == null) return;
        if (hurtSFX == null) return;

        audioSource.PlayOneShot(hurtSFX, hurtVolume);
    }

    private IEnumerator ShakeRoutine()
    {
        float elapsed = 0f;

        while (elapsed < shakeDuration)
        {
            float offsetX = Random.Range(-shakeMagnitudeX, shakeMagnitudeX);
            float offsetY = Random.Range(-shakeMagnitudeY, shakeMagnitudeY);
            float offsetZ = Random.Range(-shakeMagnitudeZ, shakeMagnitudeZ);

            visualRoot.localPosition = originalLocalPosition + new Vector3(offsetX, offsetY, offsetZ);

            elapsed += Time.deltaTime;
            yield return null;
        }

        visualRoot.localPosition = originalLocalPosition;
        shakeCoroutine = null;
    }
}