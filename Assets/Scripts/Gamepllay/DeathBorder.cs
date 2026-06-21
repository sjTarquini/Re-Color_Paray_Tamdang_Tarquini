using UnityEngine;

public class DeathBorder : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            if (GameManager.Instance != null)
            {
                InGameAudioManager.Instance.PlaySound(InGameAudioManager.Instance.deathBorderSound);
                GameManager.Instance.RestartLevelWithTransition();
            }
            else
            {
                Debug.LogWarning("GameManager not found.");
                // Fallback
                UnityEngine.SceneManagement.SceneManager.LoadScene(
                    UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
            }
        }
    }
}