namespace EternalClash.Village
{
    public class AffinityManager
    {
        public static AffinityManager Instance { get; private set; }

        public int AffinityPoints { get; set; }
        public System.Collections.Generic.List<AffinityMilestoneData> Milestones { get; set; } = new System.Collections.Generic.List<AffinityMilestoneData>();

        public event System.Action<int> OnAffinityPointsChanged;

        public void AddAffinityPoints(int amount) {}
        public bool IsLetterUnlocked(string letterId) => false;
    }
}
