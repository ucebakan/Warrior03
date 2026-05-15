using UnityEngine;

public class XPPickup : MonoBehaviour
{
    [Header("XP Value")]
    [SerializeField, Min(1)] private int xpAmount = 10;

    [Header("Pickup SFX")]
    [SerializeField] private AudioClip pickupSFX;
    [SerializeField, Range(0f, 1f)] private float pickupVolume = 1f;

    [Header("Debug")]
    [SerializeField] private bool logPickup = true;

    private bool isCollected = false;

    public void SetXPAmount(int value)
    {
        xpAmount = Mathf.Max(1, value);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isCollected)
            return;

        PlayerXP playerXP = other.GetComponentInParent<PlayerXP>();

        if (playerXP == null)
            return;

        isCollected = true;

        playerXP.AddExperience(xpAmount);

        PlayPickupSound();

        if (logPickup)
        {
            Debug.Log("XP Collected: +" + xpAmount, this);
        }

        Destroy(gameObject);
    }

    private void PlayPickupSound()
    {
        if (pickupSFX == null)
            return;

        GameObject tempAudioObject = new GameObject("XPPickupSFX");
        tempAudioObject.transform.position = transform.position;

        AudioSource tempAudioSource = tempAudioObject.AddComponent<AudioSource>();
        tempAudioSource.clip = pickupSFX;
        tempAudioSource.volume = pickupVolume;
        tempAudioSource.playOnAwake = false;
        tempAudioSource.loop = false;
        tempAudioSource.spatialBlend = 0f;

        tempAudioSource.Play();

        Destroy(tempAudioObject, pickupSFX.length + 0.1f);
    }
}