using UnityEngine;

public class EnemyQuestTag : MonoBehaviour
{
    [SerializeField] private EnemyQuestType enemyQuestType = EnemyQuestType.None;

    public EnemyQuestType EnemyQuestType => enemyQuestType;
}