using UnityEngine;

public class Coin : MonoBehaviour
{
    [SerializeField] private int value = 1;
    [SerializeField] private float moveSpeed = 2f;

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
            return;

        transform.position = Vector2.MoveTowards(transform.position, player.position, moveSpeed * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            // Cộng coin cho player
            PlayerCoin playerCoin = other.GetComponent<PlayerCoin>();

            if (playerCoin != null)
            {
                playerCoin.AddCoin(value);
            }

            Destroy(gameObject);
        }
    }
}