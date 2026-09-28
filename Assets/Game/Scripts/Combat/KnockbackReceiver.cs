using UnityEngine;
using EternalClash.Character;
using EternalClash.Enemy;
using EternalClash.World;
using EternalClash.Player;

namespace EternalClash.Combat
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class KnockbackReceiver : MonoBehaviour
    {
        public float knockbackDistance = 1.2f;
        public float knockbackDuration = 0.3f;
        public float recoveryTime = 0.25f;
        public float enemyStunTime = 0.5f;

        private Rigidbody2D rb;
        private CharacterStateMachine stateMachine;
        private EnemyMover enemyMover;
        private PlayerCombatStateMachine combatStateMachine;
        private AutoRunner autoRunner;
        private HealthSystem healthSystem;
        private EnemyHealthSystem enemyHealthSystem;
        private WorldScroller worldScroller;
        private bool isKnockback;
        private Vector2 targetPosition;
        private Vector2 originalPosition;
        private float knockbackElapsed;
        private bool isRecoveringFromHit;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            stateMachine = GetComponent<CharacterStateMachine>();
            enemyMover = GetComponent<EnemyMover>();
            combatStateMachine = GetComponent<PlayerCombatStateMachine>();
            autoRunner = GetComponent<AutoRunner>();
            healthSystem = GetComponent<HealthSystem>();
            enemyHealthSystem = GetComponent<EnemyHealthSystem>();
        }

        private void Start()
        {
            worldScroller = FindObjectOfType<WorldScroller>();
        }

        public bool IsRecoveringFromHit => isRecoveringFromHit;

        private void Update()
        {
            if (!isKnockback) return;

            // Ride the scrolling ground while knocked back, otherwise the frozen
            // enemy visibly slides against the moving world.
            if (worldScroller == null)
                worldScroller = FindObjectOfType<WorldScroller>();
            if (worldScroller != null)
            {
                float worldDeltaX = worldScroller.GetWorldVelocity().x * Time.deltaTime;
                originalPosition.x += worldDeltaX;
                targetPosition.x += worldDeltaX;
            }

            knockbackElapsed += Time.deltaTime;
            float t = Mathf.Clamp01(knockbackElapsed / Mathf.Max(0.01f, knockbackDuration));
            float easedT = Mathf.SmoothStep(0f, 1f, t);
            Vector2 nextPosition = Vector2.Lerp(originalPosition, targetPosition, easedT);
            nextPosition.y = originalPosition.y;
            transform.position = new Vector3(nextPosition.x, nextPosition.y, transform.position.z);

            if (knockbackElapsed >= knockbackDuration)
            {
                transform.position = new Vector3(targetPosition.x, originalPosition.y, transform.position.z);
                isKnockback = false;
                knockbackElapsed = 0f;
                if (stateMachine != null)
                    Invoke(nameof(RecoverFromHit), recoveryTime);
            }
        }

        /// <summary>
        /// distance &gt; 0 thi dung chinh no lam quang duoi (push-pull tung don danh),
        /// de trong thi dung knockbackDistance serialize san (hanh vi cu).
        /// </summary>
        public void ApplyKnockback(Vector2 direction, float force, float distance = -1f)
        {
            // Receiver bi disable (vd trong luc Charge de player miem knockback)
            // thi khong nhan don: dan quai van cham player (0 dmg qua multiplier)
            // nhung khong duoc kich ban recoil/HIT state giua luc charge.
            if (!enabled) return;
            if (healthSystem != null && healthSystem.IsDead) return;
            // HealthSystem (cua Player) tren dich khong bao gio bi tru mau nen
            // guard tren khong chay: blow ket liem van day xac truot vao ke dung sau.
            if (enemyHealthSystem != null && enemyHealthSystem.IsDead) return;
            if (isRecoveringFromHit) return;
            if (combatStateMachine != null && combatStateMachine.CurrentState == PlayerCombatState.Hit) return;
            if (combatStateMachine != null && combatStateMachine.CurrentState == PlayerCombatState.Dead) return;

            if (CompareTag("Player"))
            {
                ApplyPlayerEnvironmentKnockback(direction, force);
                return;
            }

            originalPosition = transform.position;
            knockbackElapsed = 0f;

            if (stateMachine != null)
                stateMachine.ChangeState(CharacterState.Hit);
            if (enemyMover != null)
                enemyMover.PauseMovement(enemyStunTime);

            float pushDistance = distance > 0f ? distance : knockbackDistance;
            targetPosition = new Vector2(
                originalPosition.x + Mathf.Sign(direction.x) * pushDistance,
                originalPosition.y);
            isKnockback = true;
        }

        private void ApplyPlayerEnvironmentKnockback(Vector2 direction, float force)
        {
            isRecoveringFromHit = true;

            if (stateMachine != null)
                stateMachine.ChangeState(CharacterState.Hit);

            if (combatStateMachine != null)
                combatStateMachine.Hit();

            if (autoRunner != null)
                autoRunner.StopRunning();

            if (worldScroller == null)
                worldScroller = FindObjectOfType<WorldScroller>();

            if (worldScroller != null)
                worldScroller.TriggerKnockback(force);

            if (DistanceProgress.Instance != null)
                DistanceProgress.Instance.ReduceDistance(knockbackDistance);

            float totalLockTime = recoveryTime + (worldScroller != null ? worldScroller.KnockbackTotalDuration : 0f);
            Invoke(nameof(RecoverFromHit), totalLockTime);

            CombatVFXController.Instance?.Shake(0.1f);
            Debug.Log("[KNOCKBACK] Player environment knockback: force=" + force);
        }

        private void RecoverFromHit()
        {
            isRecoveringFromHit = false;

            if (CompareTag("Player"))
            {
                if (healthSystem != null && healthSystem.IsDead) return;

                if (combatStateMachine != null && combatStateMachine.CurrentState == PlayerCombatState.Hit)
                    combatStateMachine.ReturnToCombatIdle();

                if (autoRunner != null)
                    autoRunner.ResumeRunning();

                if (stateMachine != null && stateMachine.CurrentState == CharacterState.Hit)
                    stateMachine.ChangeState(CharacterState.Run);
            }
            else
            {
                if (stateMachine != null && !isKnockback)
                    stateMachine.ChangeState(CharacterState.Idle);
            }
        }

        public void CancelRecovery()
        {
            isRecoveringFromHit = false;
            CancelInvoke(nameof(RecoverFromHit));
        }
    }
}
