using TMPro;
using UnityEngine;
using EternalClash.UI;

public class DamagePopup : MonoBehaviour
{
    private TextMeshProUGUI text;
    private Color startColor;
    private float timer;

    public float moveSpeed = 2f;
    public float horizontalRandom = 0.3f;
    public float lifeTime = 1f;
    public float scalePunch = 1.3f;

    public Color enemyDamageColor = Color.red;
    public Color playerDamageColor = new Color(1f, 0.5f, 0.2f);
    public Color healColor = Color.green;
    public Color blockColor = Color.cyan;

    private Vector3 startScale;
    private Vector3 moveDirection;

    private void Awake()
    {
        text = GetComponentInChildren<TextMeshProUGUI>();
        startScale = transform.localScale;
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

        switch(type)
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

        transform.localScale = startScale * scalePunch;
    }

    private void Update()
    {
        timer -= Time.deltaTime;

        transform.position += moveDirection * moveSpeed * Time.deltaTime;

        transform.localScale = Vector3.Lerp(
            transform.localScale,
            startScale,
            Time.deltaTime * 10f);

        if (text != null)
        {
            Color c = startColor;
            c.a = Mathf.Clamp01(timer / lifeTime);
            text.color = c;
        }

        if (timer <= 0)
            Destroy(gameObject);
    }
}
