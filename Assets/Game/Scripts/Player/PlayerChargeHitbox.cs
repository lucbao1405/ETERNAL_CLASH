using UnityEngine;
using EternalClash.Skill;

namespace EternalClash.Player
{
    public class PlayerChargeHitbox : MonoBehaviour
    {
        private PlayerChargeController chargeController;
        private ChargeSkill chargeSkill;

        private void Awake()
        {
            chargeController = GetComponentInParent<PlayerChargeController>();
            chargeSkill = GetComponentInParent<ChargeSkill>();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (chargeController == null || !chargeController.IsCharging)
                return;

            if (!other.CompareTag("Enemy"))
                return;

            Debug.Log("[CHARGE] Enemy collision - stop charge");

            AutoRunner runner = GetComponentInParent<AutoRunner>();
            if (runner != null)
                runner.StopChargeMovement();

            ChargeCollisionController collisionController = GetComponentInParent<ChargeCollisionController>();
            if (collisionController != null)
            {
                transform.root.position = collisionController.GetStopPosition(
                    transform.root.position,
                    other.transform.position
                );
            }

            chargeController.EndCharge();
        }
    }
}
