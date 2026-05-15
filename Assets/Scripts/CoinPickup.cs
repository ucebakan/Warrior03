using UnityEngine;

public class CoinPickup : MonoBehaviour
{
    [Header("Coin Value")]
    [SerializeField] private int coinValue = 1;

    [Header("Pickup SFX")]
    [SerializeField] private AudioClip pickupSFX;
    [SerializeField][Range(0f, 1f)] private float pickupVolume = 1f;

    private bool isCollected = false;

    public void SetCoinValue(int value)
    {
        coinValue = Mathf.Max(1, value);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isCollected)
            return;

        PlayerWallet playerWallet = other.GetComponentInParent<PlayerWallet>();
        if (playerWallet == null)
            return;

        isCollected = true;

        playerWallet.AddCoins(coinValue);

        PlayPickupSound();

        Destroy(gameObject);
    }

    private void PlayPickupSound()
    {
        if (pickupSFX == null)
            return;

        GameObject tempAudioObject = new GameObject("CoinPickupSFX");
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