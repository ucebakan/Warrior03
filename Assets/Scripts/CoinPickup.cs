using UnityEngine;

public class CoinPickup : MonoBehaviour
{
    [SerializeField] private int coinValue = 1;

    public void SetCoinValue(int value)
    {
        coinValue = Mathf.Max(1, value);
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerWallet playerWallet = other.GetComponentInParent<PlayerWallet>();

        if (playerWallet == null) return;

        playerWallet.AddCoins(coinValue);
        Destroy(gameObject);
    }
}