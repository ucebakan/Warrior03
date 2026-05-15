using UnityEngine;

public class EnemyNameTag : MonoBehaviour
{
    [Header("Enemy Display Name")]
    [SerializeField] private string displayName = "Enemy";

    public string DisplayName => displayName;
}