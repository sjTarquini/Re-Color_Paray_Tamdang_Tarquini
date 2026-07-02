using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;

[System.Serializable]
public class MStageBinding
{
    public Button button;
    public string sceneName;
}

public class MLevelSelectionManager : MonoBehaviourPunCallbacks, IOnEventCallback
{
    public static MLevelSelectionManager Instance { get; private set; }

    [Header("Difficulty Groups")]
    [SerializeField] private RectTransform hardGroup;
    [SerializeField] private RectTransform veryHardGroup;
    [SerializeField] private CanvasGroup difficultyCanvasGroup;
    [SerializeField] private float upY = 0f;
    [SerializeField] private float downY = -340f;
    [SerializeField] private float slideDuration = 0.35f;

    [Header("Host Readiness")]
    [SerializeField] private Button readyButton;
    [SerializeField] private GameObject feedbackPopup;
    [SerializeField] private TextMeshProUGUI feedbackText;

    [Header("Hard Stages")]
    [SerializeField] private List<MStageBinding> hardStageBindings = new List<MStageBinding>();

    [Header("Very Hard Stages")]
    [SerializeField] private List<MStageBinding> veryHardStageBindings = new List<MStageBinding>();

    [Header("Role Selection")]
    [SerializeField] private Button roleSelectionToggleButton;
    [SerializeField] private TextMeshProUGUI roleSelectionToggleButtonText;
    [SerializeField] private CanvasGroup roleSelectCanvasGroup;
    [SerializeField] private Button role1SelectButton;
    [SerializeField] private Button role1RemoveButton;
    [SerializeField] private Button role2SelectButton;
    [SerializeField] private Button role2RemoveButton;
    [SerializeField] private GameObject roleFeedbackPopup;
    [SerializeField] private TextMeshProUGUI roleFeedbackText;
    [SerializeField] private GameObject feedbackPopupPrefab;
    [SerializeField] private Transform feedbackPopupContainer;
    [SerializeField] private GameObject roleFeedbackPopupPrefab;
    [SerializeField] private Transform roleFeedbackPopupContainer;
    [SerializeField] private bool isPlayerOne = true;

    private RectTransform activeGroup;
    private string pendingLevelScene;
    private bool playerTwoArrived;
    private bool rolesSelected;
    private bool levelSelected;
    private bool isRoleSelectMode;
    private string selectedRole1By;
    private string selectedRole2By;

    private const string PlayerTwoArrivedKey = "MultiplayerPlayerTwoArrived";
    private const string RolesSelectedKey = "MultiplayerRolesSelected";
    private const string LevelSelectedKey = "MultiplayerLevelSelected";
    private const string SelectedLevelSceneKey = "MultiplayerSelectedLevelScene";
    private const string PlayerOneNameKey = "MultiplayerPlayerOneName";
    private const string PlayerTwoNameKey = "MultiplayerPlayerTwoName";
    private const string Role1SelectedByKey = "MultiplayerRole1SelectedBy";
    private const string Role2SelectedByKey = "MultiplayerRole2SelectedBy";
    private const byte EventRoleSelectionChanged = 1;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        InitializeReferences();
        LoadStateFromPrefs();
        BindStageButtons();
        HideFeedback();
        HideRoleFeedback();
        SetCanvasGroupVisibility(roleSelectCanvasGroup, false);

        if (roleSelectionToggleButtonText != null)
            roleSelectionToggleButtonText.text = "Choose Role";

        ShowDefaultGroup();
        UpdateRoleButtons();
        UpdateReadyButtonState();

        if (PhotonNetwork.InRoom)
            isPlayerOne = PhotonNetwork.LocalPlayer.ActorNumber == 1;
    }

    private void OnEnable()
    {
        PhotonNetwork.AddCallbackTarget(this);
    }

    private void OnDisable()
    {
        PhotonNetwork.RemoveCallbackTarget(this);
    }

    public void OnHardSelected()
    {
        SwitchToGroup(hardGroup);
    }

    public void OnVeryHardSelected()
    {
        SwitchToGroup(veryHardGroup);
    }

    public void OnRoleSelectionTogglePressed()
    {
        isRoleSelectMode = !isRoleSelectMode;

        if (roleSelectionToggleButtonText != null)
            roleSelectionToggleButtonText.text = isRoleSelectMode ? "To Level Select" : "Choose Role";

        SetCanvasGroupVisibility(difficultyCanvasGroup, !isRoleSelectMode);
        SetCanvasGroupVisibility(roleSelectCanvasGroup, isRoleSelectMode);

        UpdateRoleButtons();
        HideFeedback();
        HideRoleFeedback();
    }

    public void OnRole1SelectPressed() => SendRoleSelection(1, true);
    public void OnRole1RemovePressed() => SendRoleSelection(1, false);
    public void OnRole2SelectPressed() => SendRoleSelection(2, true);
    public void OnRole2RemovePressed() => SendRoleSelection(2, false);

    public void SetPlayerTwoArrived(bool arrived)
    {
        playerTwoArrived = arrived;
        PlayerPrefs.SetInt(PlayerTwoArrivedKey, arrived ? 1 : 0);
        PlayerPrefs.Save();
        UpdateReadyButtonState();
    }

    public void SetRolesSelected(bool selected)
    {
        rolesSelected = selected;
        PlayerPrefs.SetInt(RolesSelectedKey, selected ? 1 : 0);
        PlayerPrefs.Save();
        UpdateReadyButtonState();
    }

    public void SetLevelSelected(bool selected)
    {
        levelSelected = selected;
        PlayerPrefs.SetInt(LevelSelectedKey, selected ? 1 : 0);
        PlayerPrefs.Save();
        UpdateReadyButtonState();
    }

    public void SelectStage(string sceneName)
    {
        pendingLevelScene = string.IsNullOrEmpty(sceneName) ? "SampleScene" : sceneName;
        PlayerPrefs.SetString(SelectedLevelSceneKey, pendingLevelScene);
        PlayerPrefs.SetInt(LevelSelectedKey, 1);
        PlayerPrefs.Save();

        levelSelected = true;
        UpdateReadyButtonState();
        HideFeedback();
    }

    public void OnReadyButtonPressed()
    {
        if (!playerTwoArrived)
        {
            ShowFeedback("Player 2 is not here yet!");
            return;
        }

        if (!rolesSelected)
        {
            ShowFeedback($"{GetPlayerOneName()} and {GetPlayerTwoName()} have not picked a role yet!");
            return;
        }

        if (!levelSelected)
        {
            ShowFeedback($"{GetPlayerOneName()} and {GetPlayerTwoName()} have not selected a level yet!");
            return;
        }

        HideFeedback();
        LoadScene(pendingLevelScene);
    }

    private void InitializeReferences()
    {
        if (hardGroup == null)
            hardGroup = FindRectTransform("HardGroup");

        if (veryHardGroup == null)
            veryHardGroup = FindRectTransform("VeryHardGroup");

        if (difficultyCanvasGroup == null)
            difficultyCanvasGroup = FindCanvasGroup("DifficultyCanvas") ?? FindCanvasGroup("DifficultyGroup");

        if (readyButton == null)
            readyButton = FindObjectOfType<Button>();

        if (feedbackPopup == null)
            feedbackPopup = FindObjectByName("FeedbackPopup");

        if (feedbackPopup != null)
            feedbackPopup.SetActive(false);

        if (feedbackText == null && feedbackPopup != null)
            feedbackText = feedbackPopup.GetComponentInChildren<TextMeshProUGUI>();

        if (roleSelectionToggleButton == null)
            roleSelectionToggleButton = FindButton("RoleSelectionToggleButton");

        if (roleSelectionToggleButton != null)
        {
            roleSelectionToggleButton.onClick.RemoveAllListeners();
            roleSelectionToggleButton.onClick.AddListener(OnRoleSelectionTogglePressed);
        }

        if (roleSelectionToggleButtonText == null && roleSelectionToggleButton != null)
            roleSelectionToggleButtonText = roleSelectionToggleButton.GetComponentInChildren<TextMeshProUGUI>();

        if (roleSelectCanvasGroup == null)
            roleSelectCanvasGroup = FindCanvasGroup("RoleSelect");

        if (role1SelectButton == null)
            role1SelectButton = FindButton("Role1SelectButton");

        if (role1RemoveButton == null)
            role1RemoveButton = FindButton("Role1RemoveButton");

        if (role2SelectButton == null)
            role2SelectButton = FindButton("Role2SelectButton");

        if (role2RemoveButton == null)
            role2RemoveButton = FindButton("Role2RemoveButton");

        if (roleFeedbackPopup == null)
            roleFeedbackPopup = FindObjectByName("RoleFeedbackPopup");

        if (roleFeedbackPopup != null)
            roleFeedbackPopup.SetActive(false);

        if (roleFeedbackText == null && roleFeedbackPopup != null)
            roleFeedbackText = roleFeedbackPopup.GetComponentInChildren<TextMeshProUGUI>();
    }

    private void BindStageButtons()
    {
        foreach (var binding in hardStageBindings)
        {
            if (binding == null || binding.button == null) continue;
            binding.button.onClick.RemoveAllListeners();
            binding.button.onClick.AddListener(() => SelectStage(binding.sceneName));
        }

        foreach (var binding in veryHardStageBindings)
        {
            if (binding == null || binding.button == null) continue;
            binding.button.onClick.RemoveAllListeners();
            binding.button.onClick.AddListener(() => SelectStage(binding.sceneName));
        }
    }

    private void ShowDefaultGroup()
    {
        if (hardGroup != null)
            SwitchToGroup(hardGroup);
        else if (veryHardGroup != null)
            SwitchToGroup(veryHardGroup);
    }

    private void SwitchToGroup(RectTransform selectedGroup)
    {
        if (selectedGroup == null) return;

        foreach (var group in new[] { hardGroup, veryHardGroup })
        {
            if (group != null && group != selectedGroup)
            {
                StartCoroutine(SlideGroup(group, downY));
                SetGroupInteractable(group, false);
            }
        }

        StartCoroutine(SlideGroup(selectedGroup, upY));
        SetGroupInteractable(selectedGroup, true);
        activeGroup = selectedGroup;
    }

    private IEnumerator SlideGroup(RectTransform group, float targetY)
    {
        if (group == null) yield break;

        float elapsed = 0f;
        float startY = group.anchoredPosition.y;

        while (elapsed < slideDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / slideDuration);
            float newY = Mathf.Lerp(startY, targetY, t);
            group.anchoredPosition = new Vector2(group.anchoredPosition.x, newY);
            yield return null;
        }

        group.anchoredPosition = new Vector2(group.anchoredPosition.x, targetY);
    }

    private void SetGroupInteractable(RectTransform group, bool interactable)
    {
        if (group == null) return;

        foreach (var button in group.GetComponentsInChildren<Button>(true))
        {
            button.interactable = interactable;
        }
    }

    private void UpdateReadyButtonState()
    {
        if (readyButton != null)
        {
            readyButton.interactable = playerTwoArrived && rolesSelected && levelSelected;
        }
    }

    private void HideFeedback()
    {
        if (feedbackPopup != null)
            feedbackPopup.SetActive(false);
    }

    private void ShowFeedback(string message)
    {
        if (feedbackPopupPrefab != null || feedbackPopup != null)
        {
            SpawnFloatingFeedback(message, feedbackPopupPrefab ?? feedbackPopup, feedbackPopupContainer ?? feedbackPopup?.transform.parent);
            return;
        }

        if (feedbackPopup != null)
            feedbackPopup.SetActive(true);

        if (feedbackText != null)
            feedbackText.text = message;
    }

    private void SpawnFloatingFeedback(string message, GameObject prototype, Transform parent)
    {
        if (prototype == null)
            return;

        GameObject instance = Instantiate(prototype, parent);
        instance.SetActive(true);

        TextMeshProUGUI text = instance.GetComponentInChildren<TextMeshProUGUI>();
        if (text != null)
            text.text = message;

        CanvasGroup canvasGroup = instance.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = instance.AddComponent<CanvasGroup>();

        RectTransform rect = instance.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.anchoredPosition = rect.anchoredPosition;
        }

        StartCoroutine(AnimateFloatingPopup(instance, canvasGroup, rect));
    }

    private IEnumerator AnimateFloatingPopup(GameObject popup, CanvasGroup canvasGroup, RectTransform rect)
    {
        if (popup == null || canvasGroup == null || rect == null)
            yield break;

        float duration = 2f;
        float fadeInTime = 0.4f;
        float holdTime = 0.8f;
        float fadeOutTime = duration - fadeInTime - holdTime;
        Vector2 startPos = rect.anchoredPosition;
        Vector2 endPos = startPos + new Vector2(0f, 40f);

        canvasGroup.alpha = 0f;

        float elapsed = 0f;
        while (elapsed < fadeInTime)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Clamp01(elapsed / fadeInTime);
            rect.anchoredPosition = Vector2.Lerp(startPos, endPos, elapsed / duration);
            yield return null;
        }

        canvasGroup.alpha = 1f;

        yield return new WaitForSeconds(holdTime);

        elapsed = 0f;
        while (elapsed < fadeOutTime)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = 1f - Mathf.Clamp01(elapsed / fadeOutTime);
            rect.anchoredPosition = Vector2.Lerp(endPos, endPos + new Vector2(0f, 15f), elapsed / fadeOutTime);
            yield return null;
        }

        Destroy(popup);
    }

    private void LoadScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
            sceneName = "SampleScene";

        if (LevelTransitioner.Instance != null)
            LevelTransitioner.Instance.TransitionToLevel(sceneName);
        else
            SceneManager.LoadScene(sceneName);
    }

    private void ShowRoleFeedback(string message)
    {
        if (roleFeedbackPopupPrefab != null || roleFeedbackPopup != null)
        {
            SpawnFloatingFeedback(message, roleFeedbackPopupPrefab ?? roleFeedbackPopup, roleFeedbackPopupContainer ?? roleFeedbackPopup?.transform.parent);
            return;
        }

        if (roleFeedbackPopup != null)
            roleFeedbackPopup.SetActive(true);

        if (roleFeedbackText != null)
            roleFeedbackText.text = message;
    }

    private void HideRoleFeedback()
    {
        if (roleFeedbackPopup != null)
            roleFeedbackPopup.SetActive(false);
    }

    private void SetCanvasGroupVisibility(CanvasGroup group, bool visible)
    {
        if (group == null)
            return;

        group.alpha = visible ? 1f : 0f;
        group.interactable = visible;
        group.blocksRaycasts = visible;
    }

    private void UpdateRoleButtons()
    {
        bool role1Selected = !string.IsNullOrEmpty(selectedRole1By);
        bool role2Selected = !string.IsNullOrEmpty(selectedRole2By);

        if (role1SelectButton != null)
            role1SelectButton.interactable = !role1Selected;
        if (role1RemoveButton != null)
            role1RemoveButton.interactable = role1Selected;

        if (role2SelectButton != null)
            role2SelectButton.interactable = !role2Selected;
        if (role2RemoveButton != null)
            role2RemoveButton.interactable = role2Selected;

        rolesSelected = AreRolesFullySelected();
        UpdateReadyButtonState();
    }

    private bool AreRolesFullySelected()
    {
        return !string.IsNullOrEmpty(selectedRole1By) && !string.IsNullOrEmpty(selectedRole2By);
    }

    private void SendRoleSelection(int roleIndex, bool selected)
    {
        string playerName = GetLocalPlayerNameOrDefault();
        object[] content = new object[] { roleIndex, selected, playerName };
        RaiseEventOptions options = new RaiseEventOptions { Receivers = ReceiverGroup.All };
        SendOptions sendOptions = new SendOptions { Reliability = true };

        PhotonNetwork.RaiseEvent(EventRoleSelectionChanged, content, options, sendOptions);
        ApplyRoleSelection(roleIndex, selected, playerName);
    }

    private void ApplyRoleSelection(int roleIndex, bool selected, string selectedBy)
    {
        if (roleIndex == 1)
            selectedRole1By = selected ? selectedBy : string.Empty;
        else if (roleIndex == 2)
            selectedRole2By = selected ? selectedBy : string.Empty;
        else
            return;

        UpdateRoleSelectionPrefs();
        UpdateRoleButtons();

        if (selected)
            ShowRoleFeedback($"{selectedBy} has selected Role{roleIndex}!");
        else
            ShowRoleFeedback($"{selectedBy} has deselected the Role{roleIndex} Role.");
    }

    private void UpdateRoleSelectionPrefs()
    {
        PlayerPrefs.SetString(Role1SelectedByKey, selectedRole1By ?? string.Empty);
        PlayerPrefs.SetString(Role2SelectedByKey, selectedRole2By ?? string.Empty);
        PlayerPrefs.SetInt(RolesSelectedKey, rolesSelected ? 1 : 0);
        PlayerPrefs.Save();
    }

    public void OnEvent(EventData photonEvent)
    {
        if (photonEvent.Code != EventRoleSelectionChanged)
            return;

        object[] data = photonEvent.CustomData as object[];
        if (data == null || data.Length < 3)
            return;

        int roleIndex = (int)data[0];
        bool selected = (bool)data[1];
        string selectedBy = data[2] as string ?? GetLocalPlayerNameOrDefault();

        ApplyRoleSelection(roleIndex, selected, selectedBy);
    }

    private void LoadStateFromPrefs()
    {
        playerTwoArrived = PlayerPrefs.GetInt(PlayerTwoArrivedKey, 0) == 1;
        rolesSelected = PlayerPrefs.GetInt(RolesSelectedKey, 0) == 1;
        levelSelected = PlayerPrefs.GetInt(LevelSelectedKey, 0) == 1;
        pendingLevelScene = PlayerPrefs.GetString(SelectedLevelSceneKey, string.Empty);
        selectedRole1By = PlayerPrefs.GetString(Role1SelectedByKey, string.Empty);
        selectedRole2By = PlayerPrefs.GetString(Role2SelectedByKey, string.Empty);
    }

    private string GetPlayerOneName()
    {
        return PlayerPrefs.GetString(PlayerOneNameKey, "Player 1");
    }

    private string GetPlayerTwoName()
    {
        return PlayerPrefs.GetString(PlayerTwoNameKey, "Player 2");
    }

    private string GetLocalPlayerNameOrDefault()
    {
        if (PhotonNetwork.InRoom && !string.IsNullOrEmpty(PhotonNetwork.NickName))
            return PhotonNetwork.NickName;

        return isPlayerOne ? GetPlayerOneName() : GetPlayerTwoName();
    }

    private RectTransform FindRectTransform(string objectName)
    {
        var go = GameObject.Find(objectName);
        if (go != null)
            return go.GetComponent<RectTransform>();

        foreach (var rect in FindObjectsOfType<RectTransform>())
        {
            if (rect.name == objectName)
                return rect;
        }

        return null;
    }

    private Button FindButton(string objectName)
    {
        var go = GameObject.Find(objectName);
        if (go != null)
            return go.GetComponent<Button>();

        foreach (var button in FindObjectsOfType<Button>())
        {
            if (button.name == objectName)
                return button;
        }

        return null;
    }

    private CanvasGroup FindCanvasGroup(string objectName)
    {
        var go = GameObject.Find(objectName);
        if (go != null)
            return go.GetComponent<CanvasGroup>();

        foreach (var cg in FindObjectsOfType<CanvasGroup>())
        {
            if (cg.name == objectName)
                return cg;
        }

        return null;
    }

    private GameObject FindObjectByName(string objectName)
    {
        var go = GameObject.Find(objectName);
        if (go != null)
            return go;

        foreach (var root in FindObjectsOfType<GameObject>())
        {
            if (root.name == objectName)
                return root;
        }

        return null;
    }
}
