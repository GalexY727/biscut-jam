using UnityEngine;

public class EnemyHealth : MonoBehaviour, IDamageable
{
    public float health = 5f;
    float hp;

    void Awake() => hp = health;

    public void Damage(float amount)
    {
        hp -= amount;
        if (hp <= 0f)
        {
            Destroy(gameObject);
        }
    }
}
