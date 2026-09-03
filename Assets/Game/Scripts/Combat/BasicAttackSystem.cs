using UnityEngine;
using EternalClash.Character;

namespace EternalClash.Combat
{
    public class BasicAttackSystem : MonoBehaviour
    {
        [SerializeField] private int damage = 5;
        [SerializeField] private float attackCooldown = 1f;
        [SerializeField] private float knockbackForce = 5f;
        [SerializeField] private float attackRange = 2.2f;
        private float cooldownTimer;
        private GameObject target;
        private CharacterStateMachine stateMachine;
        private void Awake() => stateMachine = GetComponent<CharacterStateMachine>();
        private void Update()
        {
            if (cooldownTimer > 0f) cooldownTimer -= Time.deltaTime;
            if (target == null || !IsValidTarget(target) || !IsTargetInRange(target)) target = FindNearestEnemy();
            if (target != null && cooldownTimer <= 0f) StartAttack();
        }
        public void SetTarget(GameObject enemy){GameObject root=ResolveEnemyRoot(enemy);if(root!=null)target=root;}
        public void ClearTarget()=>target=null;
        public void StartAttack(){if(target==null||!IsValidTarget(target))return;Debug.Log("[ATTACK START] "+name+" -> "+target.name);if(stateMachine!=null)stateMachine.ChangeState(CharacterState.Attack);DealDamage();}
        public void AnimationDealDamage()=>DealDamage();
        public void TryAttack(GameObject enemy){SetTarget(enemy);StartAttack();}
        private void DealDamage()
        {
            if(target==null||cooldownTimer>0f||!IsValidTarget(target))return;
            if(CombatDamageResolver.Instance==null)return;
            cooldownTimer=attackCooldown;
            int finalDamage=Village.PlayerStatSystem.Instance!=null?Village.PlayerStatSystem.Instance.BasicAttackDamage:damage;
            Debug.Log("[DAMAGE SENT] "+target.name+" Damage: "+finalDamage);
            CombatDamageResolver.Instance.DealDamage(target,finalDamage,DamageSource.BasicAttack);
            KnockbackReceiver knockback=target.GetComponent<KnockbackReceiver>();
            if(knockback!=null){Vector2 direction=(target.transform.position-transform.position).normalized;knockback.ApplyKnockback(direction,knockbackForce);}
        }
        private GameObject FindNearestEnemy(){GameObject[] enemies=GameObject.FindGameObjectsWithTag("Enemy");GameObject nearest=null;float nearestDistance=attackRange;foreach(GameObject enemy in enemies){if(!IsValidTarget(enemy))continue;float distance=Vector2.Distance(transform.position,enemy.transform.position);if(distance<=nearestDistance){nearestDistance=distance;nearest=enemy;}}return nearest;}
        private bool IsTargetInRange(GameObject enemy)=>enemy!=null&&Vector2.Distance(transform.position,enemy.transform.position)<=attackRange;
        private static bool IsValidTarget(GameObject enemy)=>enemy!=null&&enemy.CompareTag("Enemy")&&enemy.activeInHierarchy;
        private static GameObject ResolveEnemyRoot(GameObject obj){if(obj==null)return null;if(obj.CompareTag("Enemy"))return obj;Transform parent=obj.transform.parent;while(parent!=null){if(parent.CompareTag("Enemy"))return parent.gameObject;parent=parent.parent;}return null;}
    }
}
