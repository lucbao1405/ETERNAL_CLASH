using UnityEngine;

public class PlayerCoin : MonoBehaviour
{
    public int coins = 0;

    public void AddCoin(int amount)
    {
        coins += amount;

        Debug.Log("Coin: " + coins);
    }
}