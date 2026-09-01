using UnityEngine;

namespace EternalClash.Player
{
    public class PlayerChargeController : MonoBehaviour
    {
        [SerializeField] private float normalSpeed = 2.5f;
        [Header("Charge Settings")]
        [SerializeField] private float chargeDuration = 0.5f;

        private float lockedX;
        private float lockedY;
        private float lockedZ;

        public bool IsCharging { get; private set; }
        public float CurrentSpeed { get; private set; }

        private void Awake()
        {
            CurrentSpeed = normalSpeed;
            lockedX = transform.position.x;
            lockedY = transform.position.y;
            lockedZ = transform.position.z;
        }

        private void LateUpdate()
        {
            transform.position = new Vector3(lockedX, lockedY, lockedZ);
        }


        public float ChargeDuration => chargeDuration;

        public void LockCurrentPosition()
        {
            lockedX = transform.position.x;
            lockedY = transform.position.y;
            lockedZ = transform.position.z;
        }

        public void StartCharge()
        {
            IsCharging = true;
            CurrentSpeed = normalSpeed;
            lockedX = transform.position.x;
            lockedY = transform.position.y;
            lockedZ = transform.position.z;
            Debug.Log("[CHARGE] Player locked position, world charge active");
        }

        public void EndCharge()
        {
            IsCharging = false;
            CurrentSpeed = normalSpeed;
            Debug.Log("[CHARGE] Player speed reset");
        }
    }
}
