using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class SpriteSequenceTrigger : MonoBehaviour
{
    [Tooltip("Parent controller that manages the sprite sequence.")]
    [SerializeField] private DialogueTrigger controller;

    [Tooltip("Index of the sprite to show when this trigger is entered.")]
    [SerializeField] private int targetSpriteIndex = 1;

    [Tooltip("How long to wait before disabling the trigger object.")]
    [SerializeField] private float disableDelay = 0.2f;

    [Tooltip("Optional tag filter for the triggering object. Leave blank to accept any object.")]
    [SerializeField] private string triggerTag = "Player";

    private bool triggered;

    private void Awake()
    {
        if (controller == null)
            controller = GetComponentInParent<DialogueTrigger>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryTrigger(other.gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        TryTrigger(other.gameObject);
    }

    private void TryTrigger(GameObject other)
    {
        if (triggered)
            return;

        if (!string.IsNullOrEmpty(triggerTag) && !other.CompareTag(triggerTag))
            return;

        if (controller == null)
        {
            Debug.LogWarning($"SpriteSequenceTrigger '{name}' has no SpriteSequenceController in parent.", this);
            return;
        }

        triggered = true;
        controller.ShowSpriteAt(targetSpriteIndex);
        StartCoroutine(DisableAfterDelay());
    }

    private IEnumerator DisableAfterDelay()
    {
        yield return new WaitForSeconds(disableDelay);
        gameObject.SetActive(false);
    }
}
