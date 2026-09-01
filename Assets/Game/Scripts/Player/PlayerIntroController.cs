using UnityEngine;

namespace EternalClash.Player
{
    public class PlayerIntroController : MonoBehaviour
    {
        [SerializeField] private float speed = 5f;

        public bool canMove { get; private set; }
        public bool combatReady { get; private set; }
        public bool IsMovingIntro { get; private set; }

        private AutoRunner autoRunner;
        private PlayerChargeController chargeController;
        private EternalClash.Combat.PlayerCombatStateMachine combatState;
        private Vector3 targetPosition;

        private void Awake()
        {
            autoRunner = GetComponent<AutoRunner>();
            chargeController = GetComponent<PlayerChargeController>();
            combatState = GetComponent<EternalClash.Combat.PlayerCombatStateMachine>();
            canMove = false;
            combatReady = false;
        }

        public void BeginIntro(Vector3 startPosition, Vector3 destination)
        {
            BeginIntro(startPosition, destination, speed);
        }

        public void BeginIntro(Vector3 startPosition, Vector3 destination, float movementSpeed)
        {
            transform.position = startPosition;
            targetPosition = destination;
            speed = Mathf.Max(0.01f, movementSpeed);
            canMove = false;
            combatReady = false;
            IsMovingIntro = true;

            if (autoRunner != null)
                autoRunner.enabled = false;
            if (chargeController != null)
                chargeController.enabled = false;
        }

        private void Update()
        {
            if (!IsMovingIntro)
                return;

            transform.position = Vector3.MoveTowards(
                transform.position,
                targetPosition,
                speed * Time.deltaTime
            );

            if (Vector3.Distance(transform.position, targetPosition) > 0.001f)
                return;

            transform.position = targetPosition;
            IsMovingIntro = false;
            canMove = false;
            combatReady = true;

            if (chargeController != null)
            {
                chargeController.LockCurrentPosition();
                chargeController.enabled = true;
            }
            if (autoRunner != null)
                autoRunner.enabled = true;
            if (combatState != null)
                combatState.EnterCombat();
        }
    }
}