using UnityEngine;

public class BeeBullet : MonoBehaviour
{
    private Vector3 _direction;
    private int _damage;
    private float _speed;
    private Transform _target;
    private float _zigzagAmplitude = 0.5f;
    private float _zigzagFrequency = 6f;
    private float _lifeTime = 3f;
    private float _timer = 0f;
    private Vector3 _startOffset;

    public void Initialize(Vector3 dir, int damage, float speed, Transform target)
    {
        _direction = dir.normalized;
        _damage = damage;
        _speed = speed;
        _target = target;
        _startOffset = transform.position;
    }

    void Update()
    {
        _timer += Time.deltaTime;
        if (_timer > _lifeTime)
        {
            Destroy(gameObject);
            return;
        }
        if (_target == null)
        {
            Destroy(gameObject);
            return;
        }
        // Homing direction
        Vector3 toTarget = (_target.position - transform.position).normalized;
        _direction = Vector3.Lerp(_direction, toTarget, Time.deltaTime * 4f).normalized;
        // Zig-zag offset
        Vector3 perp = Vector3.Cross(_direction, Vector3.forward).normalized;
        float zigzag = Mathf.Sin(Time.time * _zigzagFrequency) * _zigzagAmplitude;
        Vector3 moveDir = (_direction + perp * zigzag).normalized;
        transform.position += moveDir * _speed * Time.deltaTime;
        // Face direction
        float ang = Mathf.Atan2(moveDir.y, moveDir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, ang - 90f);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Enemy"))
        {
            var eb = other.GetComponent<EnemyBehaviour>();
            if (eb != null) eb.ApplyDamage(_damage);
            Destroy(gameObject);
        }
    }
}