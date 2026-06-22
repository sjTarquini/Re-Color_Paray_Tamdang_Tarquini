using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class LevelSelector : MonoBehaviour
{
    public static LevelSelector Instance { get; private set; }

    [Header("Popup")]
    [SerializeField] private GameObject tutorialPopupPanel;
    [SerializeField] private GameObject secondTutorialPopupPanel;

    [Header("Difficulty Groups")]
    [SerializeField] private RectTransform easyGroup;
    [SerializeField] private RectTransform mediumGroup;
    [SerializeField] private RectTransform hardGroup;
    private const string LastDifficultyKey = "LastDifficultyGroup";


    [Header("Position Settings")]
    [SerializeField] private float upY = 0f;       // Y position when active/visible
    [SerializeField] private float downY = -300f;  // Y position when hidden below
    [SerializeField] private float slideDuration = 0.4f;

    private string pendingLevelScene;
    private const string PrefKey = "ShowTutorialPopup";
    private const string SecondPrefKey = "ShowSecondTutorialPopup";
    private string StageKey(int stageNumber) => $"Level{stageNumber}Cleared";

    // Track which group is currently active
    private RectTransform activeGroup;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        tutorialPopupPanel.SetActive(false);
        secondTutorialPopupPanel.SetActive(false);

        SetGroupY(easyGroup, downY);
        SetGroupY(mediumGroup, downY);
        SetGroupY(hardGroup, downY);

        SetGroupInteractable(easyGroup, false);
        SetGroupInteractable(mediumGroup, false);
        SetGroupInteractable(hardGroup, false);

        string lastDifficulty = PlayerPrefs.GetString(LastDifficultyKey, "Easy");

        if (lastDifficulty == "Medium") SwitchToGroup(mediumGroup);
        else if (lastDifficulty == "Hard") SwitchToGroup(hardGroup);
        else SwitchToGroup(easyGroup);

    }

    private void Start()
    {
        if (activeGroup == easyGroup) SetupStageButtons(easyGroup, 1, 5);
        else if (activeGroup == mediumGroup) SetupStageButtons(mediumGroup, 6, 10);
        else if (activeGroup == hardGroup) SetupStageButtons(hardGroup, 11, 15);
    }

    // --- Difficulty Button Callbacks ---

    public void OnEasySelected()
    {
        SwitchToGroup(easyGroup);
    }

    public void OnMediumSelected()
    {
        SwitchToGroup(mediumGroup);
    }

    public void OnHardSelected()
    {
        SwitchToGroup(hardGroup);
    }

        private void SaveLastDifficulty(string difficulty)
    {
        PlayerPrefs.SetString(LastDifficultyKey, difficulty);
        PlayerPrefs.Save();
    }
    // --- Core Switch Logic ---

    private void SwitchToGroup(RectTransform selectedGroup)
    {
        foreach (var group in new[] { easyGroup, mediumGroup, hardGroup })
        {
            if (group != selectedGroup)
            {
                StartCoroutine(SlideGroup(group, downY));
                SetGroupInteractable(group, false);
            }
        }

        StartCoroutine(SlideGroup(selectedGroup, upY));
        SetGroupInteractable(selectedGroup, true);

        if (selectedGroup == easyGroup) SetupStageButtons(easyGroup, 1, 5);
        else if (selectedGroup == mediumGroup) SetupStageButtons(mediumGroup, 6, 10);
        else if (selectedGroup == hardGroup) SetupStageButtons(hardGroup, 11, 15);

        activeGroup = selectedGroup;
    }
    // --- Animation ---

    private IEnumerator SlideGroup(RectTransform group, float targetY)
    {
        float elapsed = 0f;
        float startY = group.anchoredPosition.y;

        while (elapsed < slideDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / slideDuration); // smooth ease in/out
            float newY = Mathf.Lerp(startY, targetY, t);
            group.anchoredPosition = new Vector2(group.anchoredPosition.x, newY);
            yield return null;
        }

        // Snap to exact target
        group.anchoredPosition = new Vector2(group.anchoredPosition.x, targetY);
    }

    // --- Helpers ---

    private void SetGroupY(RectTransform group, float y)
    {
        group.anchoredPosition = new Vector2(group.anchoredPosition.x, y);
    }

    private void SetGroupInteractable(RectTransform group, bool interactable)
    {
        foreach (var btn in group.GetComponentsInChildren<Button>())
        {
            btn.interactable = interactable;
            Debug.Log($"{btn.name} interactable = {interactable}, color = {btn.image.color}");
        }
    }

    //debug stuff
    public void ResetAllStageProgress()
    {
        // Wipe stage clear flags
        for (int i = 1; i <= 15; i++)
        {
            PlayerPrefs.DeleteKey(StageKey(i));
        }

        // Reset tutorial popups + difficulty
        PlayerPrefs.DeleteKey(PrefKey);
        PlayerPrefs.DeleteKey(SecondPrefKey);
        PlayerPrefs.DeleteKey(LastDifficultyKey);

        PlayerPrefs.Save();

        SetGroupInteractable(easyGroup, false);
        SetGroupInteractable(mediumGroup, false);
        SetGroupInteractable(hardGroup, false);

        SetupStageButtons(easyGroup, 1, 5);
        SetupStageButtons(mediumGroup, 6, 10);
        SetupStageButtons(hardGroup, 11, 15);

        Debug.Log("All stage progress wiped. Buttons locked, difficulty reset to Easy.");
    }
    public void UnlockAllStagesForTesting()
    {
        for (int i = 1; i <= 15; i++)
        {
            PlayerPrefs.SetInt(StageKey(i), 1);
        }

        // Disable tutorial popups so they don’t block you
        PlayerPrefs.SetInt(PrefKey, 0);
        PlayerPrefs.SetInt(SecondPrefKey, 0);

        PlayerPrefs.Save();

        if (activeGroup == easyGroup) SetupStageButtons(easyGroup, 1, 5);
        else if (activeGroup == mediumGroup) SetupStageButtons(mediumGroup, 6, 10);
        else if (activeGroup == hardGroup) SetupStageButtons(hardGroup, 11, 15);

        Debug.Log("All stages unlocked and buttons refreshed.");
    }

    // --- Level Loading (your existing logic, untouched) ---

    public void OnFirstStageButton(string levelScene)
    {
        pendingLevelScene = levelScene;
        SaveLastDifficulty("Easy");

        if (PlayerPrefs.GetInt(PrefKey, 1) == 1)
            tutorialPopupPanel.SetActive(true);
        else
            LoadScene(levelScene);
    }

    public void OnSecondStageButton(string levelScene)
    {
        pendingLevelScene = levelScene;
        SaveLastDifficulty("Medium");

        if (PlayerPrefs.GetInt(SecondPrefKey, 1) == 1)
            secondTutorialPopupPanel.SetActive(true);
        else
            LoadScene(levelScene);
    }

    public void OnStageEasyButton(string sceneName)
    {
        SaveLastDifficulty("Easy");
        LoadScene(sceneName);
    }

    public void OnStageMediumButton(string sceneName)
    {
        SaveLastDifficulty("Medium");
        LoadScene(sceneName);
    }

    public void OnStageHardButton(string sceneName)
    {
        SaveLastDifficulty("Hard");
        LoadScene(sceneName);
    }

    public void OnBackToMainMenu()
    {
        LoadScene("MainMenu");
    }

    public void OnPopupYes()
    {
        PlayerPrefs.SetInt(PrefKey, 0);
        PlayerPrefs.Save();
        tutorialPopupPanel.SetActive(false);
        LoadScene("Tutorial");
    }

    public void OnPopupNo()
    {
        PlayerPrefs.SetInt(PrefKey, 0);
        PlayerPrefs.Save();
        tutorialPopupPanel.SetActive(false);
        LoadScene(pendingLevelScene);
    }

    public void ReenableTutorialPopup()
    {
        PlayerPrefs.SetInt(PrefKey, 1);
        PlayerPrefs.Save();
    }

    public void ReenableSecondTutorialPopup()
    {
        PlayerPrefs.SetInt(SecondPrefKey, 1);
        PlayerPrefs.Save();
    }


    public void ShowSecondTutorialPopup()
    {
        if (PlayerPrefs.GetInt(SecondPrefKey, 1) == 1)
            secondTutorialPopupPanel.SetActive(true);
        else
            LoadScene("Tutorial2");
    }

    public void OnSecondPopupYes()
    {
        PlayerPrefs.SetInt(SecondPrefKey, 0);
        PlayerPrefs.Save();
        secondTutorialPopupPanel.SetActive(false);
        LoadScene("Tutorial2");
    }

    public void OnSecondPopupNo()
    {
        PlayerPrefs.SetInt(SecondPrefKey, 0);
        PlayerPrefs.Save();
        secondTutorialPopupPanel.SetActive(false);
        LoadScene(pendingLevelScene);
    }

    private void LoadScene(string sceneName)
    {
        if (LevelTransitioner.Instance != null)
            LevelTransitioner.Instance.TransitionToLevel(sceneName);
        else
            SceneManager.LoadScene(sceneName);
    }

    // --- Unlocking Levels ---

    public void MarkStageCleared(int stageNumber)
    {
        PlayerPrefs.SetInt(StageKey(stageNumber), 1);
        PlayerPrefs.Save();
    }

    public bool IsStageCleared(int stageNumber)
    {
        return PlayerPrefs.GetInt(StageKey(stageNumber), 0) == 1;
    }

    private void SetupStageButtons(RectTransform group, int startStage, int endStage)
    {
        Button[] buttons = group.GetComponentsInChildren<Button>();

        for (int i = startStage; i <= endStage; i++)
        {
            int index = i - startStage;
            bool unlocked = false;

            if (i == startStage)
            {
                if (i == 1) unlocked = true;
                else if (i == 6) unlocked = IsStageCleared(5);
                else if (i == 11) unlocked = IsStageCleared(10);
            }
            else
            {
                unlocked = IsStageCleared(i - 1);
            }

            buttons[index].interactable = unlocked;
        }
    }


}