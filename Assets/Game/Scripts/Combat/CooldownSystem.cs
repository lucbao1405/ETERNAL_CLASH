using UnityEngine;

public class CooldownSystem : MonoBehaviour
{
    public float cooldownTime = 5f;
    private float currentCooldown;

    void Update()
    {
        if (currentCooldown > 0)
        {
            currentCooldown -= Time.deltaTime;
        }
    }

    public bool IsReady()
    {
        return currentCooldown <= 0;
    }

    public void StartCooldown()
    {
        currentCooldown = cooldownTime;
    }
}
