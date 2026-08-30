using UnityEngine;

public class VillageController : MonoBehaviour
{
    public static VillageController Instance;

    public enum VillageState
    {
        Idle,
        Interacting,
        PreparingStage
    }

    public VillageState State { get; private set; }

    private void Awake()
    {
        Instance = this;
        State = VillageState.Idle;
    }

    public void EnterInteraction()
    {
        State = VillageState.Interacting;
    }

    public void ExitInteraction()
    {
        State = VillageState.Idle;
    }

    public void PrepareStage()
    {
        State = VillageState.PreparingStage;
    }
}
