using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyTarget : MonoBehaviour
{
    public static EnemyTarget instance;
    public List<Transform> enemyList = new List<Transform>();
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Enemy"))
        {
            Debug.Log("Enemy Entered");
            EnemyBehaviour enemy = collision.GetComponent<EnemyBehaviour>();
            if (enemy != null && !enemyList.Contains(enemy.transform))
            {
                enemyList.Add(enemy.transform);
            }
        }
    }
    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Enemy"))
        {
            EnemyBehaviour enemy = collision.GetComponent<EnemyBehaviour>();
            if (enemy != null && enemyList.Contains(enemy.transform))
            {
                enemyList.Remove(enemy.transform);
            }
        }
    }
    public void RemoveEnemy(Transform enemyTransform)
    {
        if (enemyList.Contains(enemyTransform))
        {
            enemyList.Remove(enemyTransform);
        }
    }
}
