using UnityEngine;

public class BlackBlockInstakill : MonoBehaviour
{
    // This block only triggers a normal respawn (keeps progress)
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            if (GameManager.Instance != null)
            {
                InGameAudioManager.Instance.PlaySound(InGameAudioManager.Instance.deathInstakillSound);
                GameManager.Instance.KillAndRespawnPlayer(resetProgress: false);
            }
            else
            {
                Debug.LogWarning("GameManager not found! Make sure it's in the scene.");
            }
        }
    }
}