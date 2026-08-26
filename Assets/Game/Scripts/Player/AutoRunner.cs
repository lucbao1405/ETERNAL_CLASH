using UnityEngine;

namespace EternalClash.Player
{
    /// <summary>
    /// Player movement system for auto-scroller gameplay.
    /// Player always moves forward automatically.
    /// </summary>
    public class AutoRunner : MonoBehaviour
    {
        [SerializeField] private float runSpeed = 2.5f;
        private bool isRunning = true;

        public float RunSpeed => runSpeed;

        private void Update()
        {
            if (!isRunning) return;

            transform.position += Vector3.right * (runSpeed * Time.deltaTime);
        }

        public void StopRunning()
        {
            isRunning = false;
        }

        public void ResumeRunning()
        {
            isRunning = true;
        }

        public void SetSpeed(float speed)
        {
            runSpeed = speed;
        }
    }
}
