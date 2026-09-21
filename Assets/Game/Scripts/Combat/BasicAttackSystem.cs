using UnityEngine;
using System.Collections;
using EternalClash.Character;
using EternalClash.Animation;

namespace EternalClash.Combat
{
    public class BasicAttackSystem : MonoBehaviour
    {
        [SerializeField] private int damage = 5;
        [SerializeField] private float attackCooldown = 1f;
        [SerializeField] private float knockbackForce = 5f;
        [SerializeField] private float attackRange = 0.85f;
        [SerializeField, Range(0.1f, 0.9f)] private float hitMoment = 0.5f;
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
        public void StartAttack()
        {
            if (target == null || !IsValidTarget(target) || cooldownTimer > 0f) return;
            Debug.Log("[ATTACK START] " + name + " -> " + target.name);
            if (stateMachine != null) stateMachine.ChangeState(CharacterState.Attack);
            // Animation-layer hook only (no gameplay impact).
            GetComponent<IPlayerAnimationFeedback>()?.NotifyAttack();
            EternalClash.Audio.GameAudio.Play(EternalClash.Audio.SoundId.PlayerAttack);
            cooldownTimer = attackCooldown;
            StartCoroutine(HitRoutine());
        }

        // Dmg den o giua state Attack (theo do dai clip Spine) thay vi ngay khi vao state.
        private IEnumerator HitRoutine()
        {
            IPlayerAnimationFeedback feedback = GetComponent<IPlayerAnimationFeedback>();
            float clipDuration = feedback != null ? feedback.GetAttackDuration() : 0f;
            yield return new WaitForSeconds(AttackTiming.HitDelay(clipDuration, hitMoment));
            if (!enabled) yield break; // player chet / stage ket thuc giua chu danh
            if (stateMachine != null && stateMachine.CurrentState != CharacterState.Attack) yield break; // bi gian doan
            DealDamage();
        }
        public void AnimationDealDamage()=>DealDamage();
        public void TryAttack(GameObject enemy){SetTarget(enemy);StartAttack();}
        private void DealDamage()
        {
            // Chup vao bien cuc bo: DealDamage() ben duoi co the khien enemy chet va
            // AttackTrigger.OnTriggerExit2D goi ClearTarget() (dat field target=null)
            // ngay trong luc dang chay ham nay. Neu cu doc lai field "target" sau do se
            // NullReferenceException - dung ban local nay thi khong bi anh huong.
            GameObject currentTarget = target;
            if(currentTarget==null||!IsValidTarget(currentTarget))return;
            if(CombatDamageResolver.Instance==null)return;
            int finalDamage=Village.PlayerStatSystem.Instance!=null?Village.PlayerStatSystem.Instance.BasicAttackDamage:damage;
            Debug.Log("[DAMAGE SENT] "+currentTarget.name+" Damage: "+finalDamage);
            CombatDamageResolver.Instance.DealDamage(currentTarget,finalDamage,DamageSource.BasicAttack);
            if(currentTarget==null)return;
            KnockbackReceiver knockback=currentTarget.GetComponent<KnockbackReceiver>();
            if(knockback!=null){Vector2 direction=(currentTarget.transform.position-transform.position).normalized;knockback.ApplyKnockback(direction,knockbackForce);}
        }
        private GameObject FindNearestEnemy(){GameObject[] enemies=GameObject.FindGameObjectsWithTag("Enemy");GameObject nearest=null;float nearestDistance=attackRange;foreach(GameObject enemy in enemies){if(!IsValidTarget(enemy))continue;float distance=Vector2.Distance(transform.position,enemy.transform.position);if(distance<=nearestDistance){nearestDistance=distance;nearest=enemy;}}return nearest;}
        private bool IsTargetInRange(GameObject enemy)=>enemy!=null&&Vector2.Distance(transform.position,enemy.transform.position)<=attackRange;
        private static bool IsValidTarget(GameObject enemy)=>enemy!=null&&enemy.CompareTag("Enemy")&&enemy.activeInHierarchy&&!IsDead(enemy);
        // Xac dang chay anim chet van activeInHierarchy: phai loai truoc khi no
        // chiem cho muc tieu gan nhat va chan player danh ke dung sau no.
        private static bool IsDead(GameObject enemy)=>enemy.TryGetComponent<EternalClash.Enemy.EnemyHealthSystem>(out var health)&&health.IsDead;
        private static GameObject ResolveEnemyRoot(GameObject obj){if(obj==null)return null;if(obj.CompareTag("Enemy"))return obj;Transform parent=obj.transform.parent;while(parent!=null){if(parent.CompareTag("Enemy"))return parent.gameObject;parent=parent.parent;}return null;}
    }
}
