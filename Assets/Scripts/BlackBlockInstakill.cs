using UnityEngine;

public class BlackBlockInstakill : MonoBehaviour
{
    // Calls PlayerManager's KillPlayer() function to kill the player and initiate the respawn sequence.
    private void OnTriggerEnter2D(Collider2D other) {
        if (other.CompareTag("Player"))
        {
            if (PlayerManager.Instance != null)
            {
                PlayerManager.Instance.StartKillAndRespawn();
            }
        }
    }
}
