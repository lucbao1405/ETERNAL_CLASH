using UnityEngine;

namespace EternalClash.Stage
{
    /// <summary>
    /// Core flow cua ETERNAL CLASH:
    /// Player tu tien theo hanh trinh, khong dieu khien chay tu do.
    /// Enemy gap nhau trong cac combat window.
    /// </summary>
    public class StageRunnerController : MonoBehaviour
    {
        [SerializeField] private float progressSpeed = 2.5f;
        [SerializeField] private float distanceGoal = 100f;

        public float Progress { get; private set; }
        public bool IsFinished { get; private set; }

        private bool pausedByCombat;

        private void Update()
        {
            if (IsFinished || pausedByCombat)
                return;

            Progress += progressSpeed * Time.deltaTime;

            if (Progress >= distanceGoal)
            {
                Progress = distanceGoal;
                IsFinished = true;
                Debug.Log("[STAGE] Reach destination");
            }
        }

        public void EnterCombat()
        {
            pausedByCombat = true;
            Debug.Log("[STAGE] Combat window start");
        }

        public void ExitCombat()
        {
            pausedByCombat = false;
            Debug.Log("[STAGE] Combat window end");
        }
    }
}
