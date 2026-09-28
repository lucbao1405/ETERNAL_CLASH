using TMPro;
using UnityEngine;
using EternalClash.UI;

public class DamagePopup : MonoBehaviour
{
    private TextMeshProUGUI text;
    private Color startColor;
    private float timer;

    public float moveSpeed = 2.2f;
    public float horizontalRandom = 0.35f;
    public float lifeTime = 1f;
    public float scalePunch = 1.45f;

    public Color enemyDamageColor = Color.red;
    public Color playerDamageColor = new Color(1f, 0.5f, 0.2f);
    public Color healColor = Color.green;
    public Color blockColor = Color.cyan;
    public Color criticalColor = new Color(1f, 0.85f, 0.2f);
    public Color comboColor = Color.white;

    [Tooltip("He so phong to chu khi sat thuong chi mang")]
    public float criticalScale = 1.5f;

    [Header("Juice")]
    [Tooltip("Luc trong luc keo text roi xuong cuoi vu tuc")]
    public float gravity = 3.5f;
    [Tooltip("Do rung xoay cua crit")]
    public float critWobble = 8f;
    [Tooltip("Toc do hoi phuc scale sau khi punch")]
    public float scaleRecovery = 9f;

    private Vector3 startScale;
    private Vector3 moveDirection;
    private float currentGravity;
    private bool isCritical;

    private void Awake()
    {
        text = GetComponentInChildren<TextMeshProUGUI>();
        startScale = transform.localScale;
        currentGravity = gravity;
    }

    public void Setup(int damage, bool isPlayerDamage = false)
    {
        SetupText("-" + damage,
            isPlayerDamage ? DamagePopupManager.PopupType.PlayerDamage : DamagePopupManager.PopupType.Damage);
    }

    public void SetupText(string value, DamagePopupManager.PopupType type)
    {
        if (text == null)
            return;

        text.text = value;
        EnableOutline();

        switch (type)
        {
            case DamagePopupManager.PopupType.Heal:
                text.color = healColor;
                break;

            case DamagePopupManager.PopupType.Block:
                text.color = blockColor;
                break;

            case DamagePopupManager.PopupType.PlayerDamage:
                text.color = playerDamageColor;
                break;

            case DamagePopupManager.PopupType.Critical:
                text.color = criticalColor;
                isCritical = true;
                startScale *= criticalScale;
                transform.localScale = startScale;
                break;

            case DamagePopupManager.PopupType.Combo:
                text.color = comboColor;
                break;

            default:
                text.color = enemyDamageColor;
                break;
        }

        startColor = text.color;
        timer = lifeTime;

        moveDirection = new Vector3(
            Random.Range(-horizontalRandom, horizontalRandom),
            1f,
            0f);

        if (isCritical)
            moveDirection.y += 0.4f;

        transform.localScale = startScale * scalePunch;
    }

    private void EnableOutline()
    {
        // Them outline dam de chu luon doc duoc tren moi nen.
        text.fontMaterial.EnableKeyword(ShaderUtilities.Keyword_Outline);
        text.outlineWidth = 0.2f;
        text.outlineColor = new Color(0f, 0f, 0f, 0.9f);
    }

    private void Update()
    {
        timer -= Time.deltaTime;

        float lifeProgress = 1f - Mathf.Clamp01(timer / lifeTime);

        // Bay len + roi xuong nhe theo trong luc tao cung cong nhay.
        currentGravity = Mathf.Lerp(currentGravity, gravity * 3f, lifeProgress);
        Vector3 velocity = moveDirection * moveSpeed - Vector3.up * currentGravity * lifeProgress;
        transform.position += velocity * Time.deltaTime;

        // Scale pop-in: dap to roi hut ve kich thuoc goc, crit rung xoay them.
        float overshoot = Mathf.Sin(lifeProgress * Mathf.PI) * 0.12f;
        float settle = Mathf.Lerp(transform.localScale.x / Mathf.Max(startScale.x, 0.001f), 1f, Time.deltaTime * scaleRecovery);
        float targetScale = settle * (1f + overshoot);
        transform.localScale = startScale * targetScale;

        if (isCritical)
        {
            float wobble = Mathf.Sin(Time.time * 30f) * critWobble * (1f - lifeProgress);
            transform.rotation = Quaternion.Euler(0f, 0f, wobble);
        }

        if (text != null)
        {
            // Fade muot ve cuoi: giu dam gan nua dau roi moi mo dan.
            float fade = Mathf.Clamp01((timer / lifeTime - 0.35f) / 0.65f);
            Color c = startColor;
            c.a = fade;
            text.color = c;
        }

        if (timer <= 0)
            Destroy(gameObject);
    }
}
