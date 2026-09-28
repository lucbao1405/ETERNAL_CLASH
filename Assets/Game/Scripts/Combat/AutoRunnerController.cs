using UnityEngine;

public class AutoRunnerController : MonoBehaviour
{
    public float speed = 2.5f;
    public bool overrideMovement;

    void Update()
    {
        if (overrideMovement) return;
        transform.position += Vector3.right * speed * Time.deltaTime;
    }

    public void OverrideMovement(float duration)
    {
        CancelInvoke();
        overrideMovement = true;
        Invoke(nameof(ResumeMovement), duration);
    }

    void ResumeMovement()
    {
        overrideMovement = false;
    }
}
