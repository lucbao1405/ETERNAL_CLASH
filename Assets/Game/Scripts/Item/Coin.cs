using UnityEngine;
using EternalClash.Village;

public class Coin : MonoBehaviour
{
    [SerializeField] private int value = 5;
    [SerializeField] private float moveSpeed = 6f;

    private Transform player;

    private void Awake()
    {
        GameObject obj = GameObject.FindGameObjectWithTag("Player");
        if (obj != null)
            player = obj.transform;
    }

    private void Update()
    {
        if (player == null)
        {
            GameObject obj = GameObject.FindGameObjectWithTag("Player");
            if (obj != null)
                player = obj.transform;
            return;
        }

        transform.position = Vector2.MoveTowards(transform.position, player.position, moveSpeed * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerCoin playerCoin = other.GetComponent<PlayerCoin>();
            if (playerCoin != null)
            {
                playerCoin.AddCoin(value);
            }

            GoldSystem.Instance?.AddGold(value);

            Destroy(gameObject);
        }
    }
}