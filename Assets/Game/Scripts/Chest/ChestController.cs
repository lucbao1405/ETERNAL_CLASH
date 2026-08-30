using UnityEngine;

namespace EternalClash.Chest
{
    public class ChestController : MonoBehaviour
    {
        public bool opened;
        
        public void Open()
        {
            if (opened) return;

            opened = true;
            Debug.Log("[CHEST] OPENED");

            GiveReward();
        }

        private void GiveReward()
        {
            Debug.Log("[CHEST] REWARD GIVEN");
        }
    }
}
