using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelSelector : MonoBehaviour
{
    [SerializeField] private GameObject tutorialPopupPanel;

    private string pendingLevelScene;

    private const string PrefKey = "ShowTutorialPopup";

    private void Awake()
    {
        tutorialPopupPanel.SetActive(false);
    }
    public void OnFirstStageButton(string levelScene)
    {
        pendingLevelScene = levelScene;

        if (PlayerPrefs.GetInt(PrefKey, 1) == 1)
        {
            tutorialPopupPanel.SetActive(true);
        }
        else
        {
            LoadScene(levelScene);
        }
    }

    // Hook this up to any other stage buttons
    public void OnStageButton(string sceneName)
    {
        LoadScene(sceneName);
    }

    // "Yes" button on popup
    public void OnPopupYes()
    {
        PlayerPrefs.SetInt(PrefKey, 0);
        PlayerPrefs.Save();
        tutorialPopupPanel.SetActive(false);
        LoadScene("Tutorial");
    }

    // "No" button on popup
    public void OnPopupNo()
    {
        PlayerPrefs.SetInt(PrefKey, 0);
        PlayerPrefs.Save();
        tutorialPopupPanel.SetActive(false);
        LoadScene(pendingLevelScene);
    }

    // Call this to re-enable the popup (e.g. from settings)
    public void ReenableTutorialPopup()
    {
        PlayerPrefs.SetInt(PrefKey, 1);
        PlayerPrefs.Save();
    }

    private void LoadScene(string sceneName)
    {
        if (LevelTransitioner.Instance != null)
            LevelTransitioner.Instance.TransitionToLevel(sceneName);
        else
            SceneManager.LoadScene(sceneName);
    }
}