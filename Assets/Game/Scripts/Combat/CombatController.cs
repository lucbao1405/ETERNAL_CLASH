using UnityEngine;

public class CombatController : MonoBehaviour
{
    public CharacterStats attacker;
    public CharacterStats defender;
    public HealthSystem targetHealth;

    public void Attack()
    {
        if (attacker == null || defender == null || targetHealth == null)
            return;

        float damage = DamageSystem.CalculateDamage(attacker, defender);
        targetHealth.TakeDamage(damage);
    }
}
