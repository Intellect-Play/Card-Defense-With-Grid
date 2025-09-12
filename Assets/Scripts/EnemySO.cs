// EnemySO.cs
using UnityEngine;

[CreateAssetMenu(fileName = "NewEnemy", menuName = "ScriptableObjects/EnemySO", order = 1)]
public class EnemySO : ScriptableObject
{
    [Header("Stats")]
    [Tooltip("Starting health of this enemy")]
    public int health = 100;
    [Tooltip("Damage dealt per hit")]
    public int attack = 10;
    [Tooltip("Movement speed (units/sec)")]
    public float moveSpeed  = 1f;

    [Tooltip("Rewarded Gold")]
    public int rewardedGold;


    [Header("Prefab")]
    [Tooltip("Prefab to spawn in‑game")]
    public GameObject prefab;

    /// <summary>
    /// You can add any shared logic here for when this enemy attacks.
    /// </summary>
    public void Attack()
    {
        Debug.Log($"{name} attacks with power {attack}");
    }

    /// <summary>
    /// You can add any shared logic here for when this enemy dies.
    /// </summary>
    public void OnDestroyed()
    {
        Debug.Log($"{name} has been destroyed!");
    }
}
