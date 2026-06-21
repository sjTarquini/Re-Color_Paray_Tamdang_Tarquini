using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlueObjectTrigger : MonoBehaviour
{
    [SerializeField] private float fadeDuration = 2f;
    private bool hasBeenTriggered = false;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        Debug.Log("Blue object hit by: " + collision.gameObject.name + " | Tag: " + collision.gameObject.tag);
        
        if (collision.gameObject.CompareTag("YellowObject") && !hasBeenTriggered)
        {
            Debug.Log("Yellow object detected! Starting destruction sequence.");
            hasBeenTriggered = true;
            Destroy(collision.gameObject);
            StartCoroutine(FadeOutAndDeactivate());
        }
    }

    private IEnumerator FadeOutAndDeactivate()
    {
        SpriteRenderer spriteRenderer = transform.parent.GetComponent<SpriteRenderer>();
        
        // === PLAY COMBINE SOUND ===
        if (InGameAudioManager.Instance != null)
            InGameAudioManager.Instance.PlaySound(InGameAudioManager.Instance.yellowBlueCombineSound);
        else if (GetComponent<AudioSource>() != null)
            GetComponent<AudioSource>().Play();

        if (spriteRenderer != null)
        {
            spriteRenderer.color = new Color(0.13f, 0.55f, 0.13f, 1f);

            float elapsedTime = 0f;
            while (elapsedTime < fadeDuration)
            {
                elapsedTime += Time.deltaTime;
                float alpha = Mathf.Lerp(1f, 0f, elapsedTime / fadeDuration);
                spriteRenderer.color = new Color(0.13f, 0.55f, 0.13f, alpha);
                yield return null;
            }
            spriteRenderer.color = new Color(0.13f, 0.55f, 0.13f, 1f);
        }

        transform.parent.gameObject.SetActive(false);
    }
}