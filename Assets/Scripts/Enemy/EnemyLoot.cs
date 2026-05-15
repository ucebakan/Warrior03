using UnityEngine;

public class EnemyLoot : MonoBehaviour
{
    [Header("Coin Drop")]
    [SerializeField] private bool dropCoin = true;
    [SerializeField] private GameObject coinPrefab;
    [SerializeField] private int coinValue = 1;
    [SerializeField] private Vector3 coinDropOffset = new Vector3(0f, 0.5f, 0f);

    [Header("XP Drop")]
    [SerializeField] private bool dropXP = true;
    [SerializeField] private GameObject xpPrefab;
    [SerializeField, Min(1)] private int xpValue = 10;
    [SerializeField] private Vector3 xpDropOffset = new Vector3(0.35f, 0.5f, 0f);

    public void DropLoot()
    {
        DropCoin();
        DropXP();
    }

    private void DropCoin()
    {
        if (!dropCoin)
            return;

        if (coinPrefab == null)
            return;

        GameObject spawnedCoin = Instantiate(coinPrefab, transform.position + coinDropOffset, Quaternion.identity);

        CoinPickup coinPickup = spawnedCoin.GetComponent<CoinPickup>();

        if (coinPickup != null)
        {
            coinPickup.SetCoinValue(coinValue);
        }
    }

    private void DropXP()
    {
        if (!dropXP)
            return;

        if (xpPrefab == null)
            return;

        GameObject spawnedXP = Instantiate(xpPrefab, transform.position + xpDropOffset, Quaternion.identity);

        XPPickup xpPickup = spawnedXP.GetComponent<XPPickup>();

        if (xpPickup != null)
        {
            xpPickup.SetXPAmount(xpValue);
        }
    }
}