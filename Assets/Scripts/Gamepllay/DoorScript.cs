using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DoorScript : MonoBehaviour
{
    [Header("Children Components")]
    [SerializeField] private Animator lockAnimator;
    [SerializeField] private Animator doorAnimator;

    [Header("Animation State Names")]
    [SerializeField] private string lockStateName = "Lock";
    [SerializeField] private string openStateName = "Open";

    [Header("Level Transition Settings")]
    [SerializeField] private string nextSceneName = "Level2"; 

    [Header("Stage Settings")]
    [Tooltip("Which stage this door represents (e.g. 1 for Level1, 2 for Level2, etc.)")]
    [SerializeField] private int stageNumber = 1;

    [Header("Settings")]
    [SerializeField] private string playerTag = "Player";

    private bool hasOpened = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasOpened) return;
        if (!other.CompareTag(playerTag)) return;

        if (PlayerManager.Instance != null && PlayerManager.Instance.HasKey)
        {
            StartCoroutine(OpenSequence());
        }
    }

    private IEnumerator OpenSequence()
    {
        hasOpened = true;

        if (lockAnimator != null)
        {
            InGameAudioManager.Instance.PlaySound(InGameAudioManager.Instance.levelCompleteSound);
            MusicManager.Instance.StopMusic();
            lockAnimator.Play(lockStateName);
            yield return null;
            var lockStateInfo = lockAnimator.GetCurrentAnimatorStateInfo(0);
            yield return new WaitForSeconds(lockStateInfo.length);
        }

        if (doorAnimator != null)
        {
            doorAnimator.Play(openStateName);
            yield return null;
            var doorStateInfo = doorAnimator.GetCurrentAnimatorStateInfo(0);
            yield return new WaitForSeconds(doorStateInfo.length);
        }

        TeleportToNextLevel();
    }

    private void TeleportToNextLevel()
    {
        Debug.Log("Sequence complete! Transitioning to next level...");

        // ✅ Mark this stage as cleared before leaving
        PlayerPrefs.SetInt($"Level{stageNumber}Cleared", 1);
        PlayerPrefs.Save();

        if (string.IsNullOrEmpty(nextSceneName))
        {
            Debug.LogWarning("DoorScript: Next Scene Name is empty! Assign it in the Inspector.");
            return;
        }

        if (LevelTransitioner.Instance != null)
        {
            LevelTransitioner.Instance.TransitionToLevel(nextSceneName);
        }
        else
        {
            SceneManager.LoadScene(nextSceneName);
        }
    }
}
