using UnityEngine;

namespace EternalClash.Player
{
    public class PlayerChargeController : MonoBehaviour
    {
        [SerializeField] private float normalSpeed = 2.5f;
        [SerializeField] private float chargeSpeed = 6f;

        public bool IsCharging { get; private set; }
        public float CurrentSpeed { get; private set; }

        private void Awake()
        {
            CurrentSpeed = normalSpeed;
        }

        public void StartCharge()
        {
            IsCharging = true;
            CurrentSpeed = chargeSpeed;
            Debug.Log("[CHARGE] Player speed up");
        }

        public void EndCharge()
        {
            IsCharging = false;
            CurrentSpeed = normalSpeed;
            Debug.Log("[CHARGE] Player speed reset");
        }
    }
}
