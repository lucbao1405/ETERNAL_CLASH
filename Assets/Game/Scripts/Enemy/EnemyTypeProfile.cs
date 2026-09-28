using UnityEngine;

[CreateAssetMenu(fileName = "EnemyTypeProfile", menuName = "Game/Enemy Type Profile")]
public class EnemyTypeProfile : ScriptableObject
{
    public string enemyName;

    [Header("Movement")]
    public float moveSpeed = 1f;
    public bool stopInCombatRange = true;

    [Header("Combat")]
    public float attackDamage = 5f;
    public float prepareTime = 0.6f;
    public float attackCooldown = 1.5f;

    [Header("Reaction")]
    public float knockbackForce = 2f;
    public float stunDuration = 0.5f;

    [Header("Behavior")]
    public EnemyBehaviorType behaviorType;
}

public enum EnemyBehaviorType
{
    Melee,
    FastMelee,
    Ranged
}
