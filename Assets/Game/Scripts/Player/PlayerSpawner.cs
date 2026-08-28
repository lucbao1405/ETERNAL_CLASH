using UnityEngine;
using System.Collections;

namespace EternalClash.Player
{
    /// <summary>
    /// Handles spawning player prefab and moving player into combat position.
    /// </summary>
    public class PlayerSpawner : MonoBehaviour
    {
        [SerializeField] private GameObject playerPrefab;
        [SerializeField] private Transform spawnPoint;

        [Header("Spawn Movement")]
        [SerializeField] private float moveTargetX = -1f;
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private bool moveAfterSpawn = true;

        private GameObject currentPlayer;

        public GameObject CurrentPlayer => currentPlayer;

        private void Start()
        {
            SpawnPlayer();
        }

        public void SpawnPlayer()
        {
            if (playerPrefab == null || spawnPoint == null)
            {
                Debug.LogWarning("PlayerSpawner missing reference");
                return;
            }

            currentPlayer = Instantiate(
                playerPrefab,
                spawnPoint.position,
                Quaternion.identity
            );

            if (moveAfterSpawn)
            {
                StartCoroutine(MovePlayerToPosition());
            }
        }

        private IEnumerator MovePlayerToPosition()
        {
            Vector3 target = new Vector3(
                moveTargetX,
                currentPlayer.transform.position.y,
                currentPlayer.transform.position.z
            );

            while (Vector3.Distance(currentPlayer.transform.position, target) > 0.05f)
            {
                currentPlayer.transform.position = Vector3.MoveTowards(
                    currentPlayer.transform.position,
                    target,
                    moveSpeed * Time.deltaTime
                );

                yield return null;
            }

            currentPlayer.transform.position = target;
        }
    }
}
