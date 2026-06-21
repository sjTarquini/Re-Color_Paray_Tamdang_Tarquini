using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class keyScript : MonoBehaviour
{
    private void Reset()
    {
        Collider2D collider = GetComponent<Collider2D>();
        collider.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            if (PlayerManager.Instance != null)
            {
                InGameAudioManager.Instance.PlaySound(InGameAudioManager.Instance.crayonCollectSound);
                PlayerManager.Instance.CollectKey();
                Destroy(gameObject);
            }
            else
            {
                Debug.LogWarning("keyScript: PlayerManager instance not found.");
            }
        }
    }
}