using UnityEngine;

public class EnemyLoot : MonoBehaviour
{
    [SerializeField] private GameObject coinPrefab;
    [SerializeField] private int coinValue = 1;
    [SerializeField] private Vector3 dropOffset = new Vector3(0f, 0.5f, 0f);

    public void DropLoot()
    {
        if (coinPrefab == null) return;

        GameObject spawnedCoin = Instantiate(coinPrefab, transform.position + dropOffset, Quaternion.identity);

        CoinPickup coinPickup = spawnedCoin.GetComponent<CoinPickup>();
        if (coinPickup != null)
        {
            coinPickup.SetCoinValue(coinValue);
        }
    }
}