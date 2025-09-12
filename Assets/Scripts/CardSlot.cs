// CardSlot.cs
using System.Collections;
using UnityEngine;

public class CardSlot : MonoBehaviour
{
    [Header("Firing Settings")]
    [Tooltip("Prefab of the bullet to spawn")]
    public GameObject bulletPrefab;
    [Tooltip("How often (in seconds) to fire while a card is present")]
    public float fireInterval = 1f;
    [Tooltip("Name of the hero assigned to this slot; leave null to disable")]
    public string heroName = null;

    private Coroutine _fireCoroutine;

    void Awake()
    {
        // If no hero is assigned, disable this slot entirely
        if (string.IsNullOrEmpty(heroName))
        {
            gameObject.SetActive(false);
        }
    }
    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.Space))
        {
            //StopCoroutine(_fireCoroutine);
        }
    }
    /// <summary>
    /// Unity callback whenever a child is added or removed.
    /// </summary>
    void OnTransformChildrenChanged()
    {
        // Don't do anything if slot is disabled
        if (!gameObject.activeSelf) return;

        // Start firing when a card arrives
        if (transform.childCount > 0 && _fireCoroutine == null)
        {
           // _fireCoroutine = StartCoroutine(FireRoutine());
        }
        // Stop firing when card is removed
        else if (transform.childCount == 0 && _fireCoroutine != null)
        {
           // StopCoroutine(_fireCoroutine);
           // _fireCoroutine = null;
        }
    }

    /// <summary>
    /// Spawns a bullet at this slot's position every fireInterval seconds.
    /// </summary>
    private IEnumerator FireRoutine()
    {
        while (true)
        {
            Instantiate(bulletPrefab, transform.position, Quaternion.identity);
            yield return new WaitForSeconds(fireInterval);
        }
    }
}
