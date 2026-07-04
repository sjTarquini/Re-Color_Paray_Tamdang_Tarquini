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

    // Created at runtime in SetupStageBinding(); not shown in the Inspector.
    private Image p1Indicator;
    private Image p2Indicator;
    private Color originalButtonColor = Color.white;
    private bool hasOriginalColor;

    public Image P1Indicator { get => p1Indicator; set => p1Indicator = value; }
    public Image P2Indicator { get => p2Indicator; set => p2Indicator = value; }

    public Color OriginalButtonColor
    {
        get => originalButtonColor;
        set { originalButtonColor = value; hasOriginalColor = true; }
    }

    public bool HasOriginalColor => hasOriginalColor;
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
    [SerializeField] private float feedbackDuration = 2f;

    [Header("Hard Stages")]
    [SerializeField] private List<MStageBinding> hardStageBindings = new List<MStageBinding>();

    [Header("Very Hard Stages")]
    [SerializeField] private List<MStageBinding> veryHardStageBindings = new List<MStageBinding>();

    [Header("Level Selection Indicators")]
    [SerializeField] private Sprite player1IndicatorSprite;
    [SerializeField] private Sprite player2IndicatorSprite;
    [SerializeField] private Vector2 indicatorSize = new Vector2(40f, 40f);
    [SerializeField] private Vector2 indicatorMargin = new Vector2(6f, 6f);
    [SerializeField] private Color selectedButtonColor = new Color(0.65f, 0.85f, 1f, 1f);

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
    [SerializeField] private float roleFeedbackDuration = 2f;

    [Header("Role Selection Indicators")]
    [Tooltip("Where the P1/P2 owner icon appears for Role1. Leave empty to default to the Role1 Select button.")]
    [SerializeField] private RectTransform role1IndicatorAnchor;
    [Tooltip("Where the P1/P2 owner icon appears for Role2. Leave empty to default to the Role2 Select button.")]
    [SerializeField] private RectTransform role2IndicatorAnchor;

    [Header("Leave / Back")]
    [Tooltip("Scene to load when the local player presses the Leave button (e.g. the lobby/create-server scene).")]
    [SerializeField] private string createServerSceneName = "CreateServer";

    [Header("Testing")]
    [SerializeField] private bool singlePlayerTestMode = false;
    [Tooltip("When enabled, bypasses the 2-player requirement for testing with a single player.")]

    private Coroutine feedbackHideRoutine;
    private Coroutine roleFeedbackHideRoutine;

    private Image role1Indicator;
    private Image role2Indicator;

    private RectTransform activeGroup;
    private string pendingLevelScene;
    private bool playerTwoArrived;
    private bool rolesSelected;
    private bool levelSelected;
    private bool isRoleSelectMode;
    private string selectedRole1By;
    private string selectedRole2By;
    private bool? selectedRole1IsPlayerOne;
    private bool? selectedRole2IsPlayerOne;

    private string player1SelectedScene = string.Empty;
    private string player2SelectedScene = string.Empty;

    // FIX (joiner-uses-host-assets bug): this used to be a [SerializeField] bool that was cached
    // once in Start() via "isPlayerOne = PhotonNetwork.IsMasterClient;" behind an
    // "if (PhotonNetwork.InRoom)" check. If Start() ran before Photon had fully confirmed room
    // membership on the joiner's client (a real race right after a synced scene load), that check
    // could evaluate false and isPlayerOne would keep its default value of `true` forever -
    // making the joiner permanently think it was Player 1. Making this a live computed property
    // means it can never go stale: it always reflects Photon's current state.
    private bool isPlayerOne => PhotonNetwork.IsMasterClient;

    private const string PlayerTwoArrivedKey = "MultiplayerPlayerTwoArrived";
    private const string RolesSelectedKey = "MultiplayerRolesSelected";
    private const string LevelSelectedKey = "MultiplayerLevelSelected";
    private const string SelectedLevelSceneKey = "MultiplayerSelectedLevelScene";
    private const string PlayerOneNameKey = "MultiplayerPlayerOneName";
    private const string PlayerTwoNameKey = "MultiplayerPlayerTwoName";
    private const string Role1SelectedByKey = "MultiplayerRole1SelectedBy";
    private const string Role2SelectedByKey = "MultiplayerRole2SelectedBy";
    private const string Role1IsPlayerOneKey = "MultiplayerRole1IsPlayerOne";
    private const string Role2IsPlayerOneKey = "MultiplayerRole2IsPlayerOne";
    private const string Player1SelectedLevelKey = "MultiplayerPlayer1SelectedLevel";
    private const string Player2SelectedLevelKey = "MultiplayerPlayer2SelectedLevel";

    private const byte EventRoleSelectionChanged = 1;
    private const byte EventLevelSelectionChanged = 2;

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
        Debug.Log($"[MLevelSelectionManager] Scene loaded. IsMasterClient: {PhotonNetwork.IsMasterClient} | PlayerCount: {PhotonNetwork.CurrentRoom?.PlayerCount ?? 0}");
        InitializeReferences();
        LoadStateFromPrefs();
        ClearRoleSelectionState();
        BindStageButtons();
        SetupRoleIndicators();
        HideFeedback();
        HideRoleFeedback();
        SetCanvasGroupVisibility(roleSelectCanvasGroup, false);

        if (roleSelectionToggleButtonText != null)
            roleSelectionToggleButtonText.text = "Choose Role";

        ShowDefaultGroup();
        UpdateRoleButtons();

        // isPlayerOne is now a live property (PhotonNetwork.IsMasterClient), so no caching/assignment
        // needed here anymore. Kept the diagnostic log since it's still useful for debugging.
        Debug.Log($"=== LOCAL PLAYER INFO ===\n" +
                $"ActorNumber: {PhotonNetwork.LocalPlayer?.ActorNumber ?? -1}\n" +
                $"IsMasterClient: {PhotonNetwork.IsMasterClient}\n" +
                $"isPlayerOne: {isPlayerOne}\n" +
                $"NickName: {PhotonNetwork.NickName}\n" +
                $"Room PlayerCount: {PhotonNetwork.CurrentRoom?.PlayerCount ?? 0}");

        RefreshStageIndicators();
        UpdateLevelSelectedState();
        UpdateReadyButtonState();

        if (feedbackPopup == null)
            Debug.LogWarning("[MLevelSelectionManager] No feedbackPopup assigned/found. ShowFeedback() will silently do nothing.");
        if (feedbackText == null)
            Debug.LogWarning("[MLevelSelectionManager] No feedbackText (TMP) assigned/found. ShowFeedback() has nothing to write the message into.");

        if (roleFeedbackPopup == null)
            Debug.LogWarning("[MLevelSelectionManager] No roleFeedbackPopup assigned/found. ShowRoleFeedback() will silently do nothing.");
        if (roleFeedbackText == null)
            Debug.LogWarning("[MLevelSelectionManager] No roleFeedbackText (TMP) assigned/found. ShowRoleFeedback() has nothing to write the message into.");

        if (player1IndicatorSprite == null || player2IndicatorSprite == null)
            Debug.LogWarning("[MLevelSelectionManager] Player1IndicatorSprite or Player2IndicatorSprite is not assigned. Level pick icons won't be visible.");
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

    public void OnReadyButtonPressed()
    {
        if (!PhotonNetwork.IsMasterClient)
        {
            ShowFeedback("Only the host can start the game!");
            return;
        }

        if (!singlePlayerTestMode && !IsPlayerTwoPresent())
        {
            ShowFeedback("Player 2 is not here yet!");
            return;
        }

        if (!singlePlayerTestMode && !rolesSelected)
        {
            ShowFeedback($"{GetPlayerOneName()} and {GetPlayerTwoName()} have not picked a role yet!");
            return;
        }

        if (!levelSelected)
        {
            if (singlePlayerTestMode)
                ShowFeedback("You have not selected a level yet!");
            else
                ShowFeedback($"{GetPlayerOneName()} and {GetPlayerTwoName()} have not agreed on a level yet!");
            return;
        }

        HideFeedback();
        string sceneToLoad = pendingLevelScene;
        ClearLevelSelectionState();

        if (LevelTransitioner.Instance != null)
            LevelTransitioner.Instance.TransitionToLevel(sceneToLoad);
        else
        PhotonNetwork.LoadLevel(sceneToLoad);
    }

    /// <summary>
    /// Hook this up to a "Leave" / "Back" button. Leaves the Photon room (which also clears
    /// local level-selection state via OnLeftRoom) and returns to the create-server scene.
    /// </summary>
    public void OnLeaveButtonPressed()
    {
        ClearLevelSelectionState();

        if (PhotonNetwork.InRoom)
            PhotonNetwork.LeaveRoom();

        SceneManager.LoadScene(createServerSceneName);
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
            feedbackPopup = FindObjectByName("FeedbackMenu");

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
            role1SelectButton = FindButton("Select");

        if (role1RemoveButton == null)
            role1RemoveButton = FindButton("Deselect");

        if (role2SelectButton == null)
            role2SelectButton = FindButton("Select (1)");

        if (role2RemoveButton == null)
            role2RemoveButton = FindButton("Deselect (1)");

        if (roleFeedbackPopup == null)
            roleFeedbackPopup = FindObjectByName("FeedbackRole");

        if (roleFeedbackText == null && roleFeedbackPopup != null)
            roleFeedbackText = roleFeedbackPopup.GetComponentInChildren<TextMeshProUGUI>();
    }

    // ---------------------------------------------------------------
    // Stage / Level selection
    // ---------------------------------------------------------------

    private void BindStageButtons()
    {
        foreach (var binding in hardStageBindings)
            SetupStageBinding(binding);

        foreach (var binding in veryHardStageBindings)
            SetupStageBinding(binding);
    }

    private void SetupStageBinding(MStageBinding binding)
    {
        if (binding == null || binding.button == null) return;

        if (binding.button.image != null)
            binding.OriginalButtonColor = binding.button.image.color;

        binding.button.onClick.RemoveAllListeners();
        binding.button.onClick.AddListener(() => OnStageButtonPressed(binding));

        CreateIndicatorIcons(binding);
    }

    private void CreateIndicatorIcons(MStageBinding binding)
    {
        RectTransform buttonRect = binding.button.GetComponent<RectTransform>();
        if (buttonRect == null) return;

        binding.P1Indicator = CreateIndicatorIcon(
            buttonRect, player1IndicatorSprite, "P1Indicator",
            anchor: new Vector2(0f, 0f),
            anchoredOffset: new Vector2(indicatorMargin.x, indicatorMargin.y));

        binding.P2Indicator = CreateIndicatorIcon(
            buttonRect, player2IndicatorSprite, "P2Indicator",
            anchor: new Vector2(1f, 0f),
            anchoredOffset: new Vector2(-indicatorMargin.x, indicatorMargin.y));
    }

    private Image CreateIndicatorIcon(RectTransform parent, Sprite sprite, string objectName, Vector2 anchor, Vector2 anchoredOffset)
    {
        GameObject go = new GameObject(objectName, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = anchor;
        rect.sizeDelta = indicatorSize;
        rect.anchoredPosition = anchoredOffset;

        Image image = go.GetComponent<Image>();
        image.sprite = sprite;
        image.preserveAspect = true;
        image.raycastTarget = false;

        go.SetActive(false);
        return image;
    }

    private void OnStageButtonPressed(MStageBinding binding)
    {
        if (binding == null) return;

        string localSelection = GetLocalSelectedLevelScene();
        bool isDeselecting = !string.IsNullOrEmpty(localSelection) && localSelection == binding.sceneName;

        SendLevelSelection(isDeselecting ? string.Empty : binding.sceneName);
    }

    private void SendLevelSelection(string sceneName)
    {
        object[] content = new object[] { isPlayerOne, sceneName };
        RaiseEventOptions options = new RaiseEventOptions { Receivers = ReceiverGroup.All };
        SendOptions sendOptions = new SendOptions { Reliability = true };

        PhotonNetwork.RaiseEvent(EventLevelSelectionChanged, content, options, sendOptions);
        ApplyLevelSelection(isPlayerOne, sceneName);
    }

    private void ApplyLevelSelection(bool fromPlayerOne, string sceneName)
    {
        // One active pick per player - this simply overwrites their previous pick.
        if (fromPlayerOne)
            player1SelectedScene = sceneName;
        else
            player2SelectedScene = sceneName;

        UpdateLevelSelectionPrefs();
        RefreshStageIndicators();
        UpdateLevelSelectedState();

        // FIX: show feedback whenever a level pick changes, same pattern as role feedback.
        string playerName = fromPlayerOne ? GetPlayerOneName() : GetPlayerTwoName();
        if (!string.IsNullOrEmpty(sceneName))
            ShowFeedback($"{playerName} selected {sceneName}!");
        else
            ShowFeedback($"{playerName} deselected their level.");
    }

    private void UpdateLevelSelectedState()
    {
        bool levelReady = false;

        if (singlePlayerTestMode)
        {
            // In single-player mode, just need the local player to select a level
            string localSelection = GetLocalSelectedLevelScene();
            levelReady = !string.IsNullOrEmpty(localSelection);
            if (levelReady)
                pendingLevelScene = localSelection;
            else
                pendingLevelScene = string.Empty;
        }
        else
        {
            // In multiplayer mode, both players must select the same level
            bool bothPicked = !string.IsNullOrEmpty(player1SelectedScene) && !string.IsNullOrEmpty(player2SelectedScene);
            levelReady = bothPicked && player1SelectedScene == player2SelectedScene;

            if (levelReady)
                pendingLevelScene = player1SelectedScene;
            else
                pendingLevelScene = string.Empty;
        }

        if (!string.IsNullOrEmpty(pendingLevelScene))
        {
            PlayerPrefs.SetString(SelectedLevelSceneKey, pendingLevelScene);
            PlayerPrefs.Save();
        }
        else
        {
            PlayerPrefs.DeleteKey(SelectedLevelSceneKey);
            PlayerPrefs.Save();
        }

        SetLevelSelected(levelReady);
    }

    /// <summary>
    /// Wipes all role-selection state: in-memory picks and PlayerPrefs.
    /// Call when entering a new room so roles are always available for fresh picking.
    /// </summary>
    private void ClearRoleSelectionState()
    {
        selectedRole1By = string.Empty;
        selectedRole2By = string.Empty;
        selectedRole1IsPlayerOne = null;
        selectedRole2IsPlayerOne = null;

        PlayerPrefs.DeleteKey(Role1SelectedByKey);
        PlayerPrefs.DeleteKey(Role2SelectedByKey);
        PlayerPrefs.DeleteKey(Role1IsPlayerOneKey);
        PlayerPrefs.DeleteKey(Role2IsPlayerOneKey);
        PlayerPrefs.DeleteKey(RolesSelectedKey);
        PlayerPrefs.Save();

        UpdateRoleButtons();
    }

    /// <summary>
    /// Wipes all level-selection state: in-memory picks, PlayerPrefs, and the P1/P2 icons.
    /// Call this after starting a level and whenever the lobby is left, so stale picks
    /// from a previous session don't linger.
    /// </summary>
    public void ClearLevelSelectionState()
    {
        player1SelectedScene = string.Empty;
        player2SelectedScene = string.Empty;
        pendingLevelScene = string.Empty;

        PlayerPrefs.DeleteKey(Player1SelectedLevelKey);
        PlayerPrefs.DeleteKey(Player2SelectedLevelKey);
        PlayerPrefs.DeleteKey(SelectedLevelSceneKey);
        PlayerPrefs.Save();

        RefreshStageIndicators();
        SetLevelSelected(false);
    }

    public override void OnLeftRoom()
    {
        ClearLevelSelectionState();
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        UpdateReadyButtonState();
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        UpdateReadyButtonState();
    }

    private void RefreshStageIndicators()
    {
        string localSelection = GetLocalSelectedLevelScene();

        RefreshBindingGroup(hardStageBindings, localSelection);
        RefreshBindingGroup(veryHardStageBindings, localSelection);
    }

    private void RefreshBindingGroup(List<MStageBinding> bindings, string localSelection)
    {
        foreach (var binding in bindings)
        {
            if (binding == null || binding.button == null) continue;

            if (binding.P1Indicator != null)
                binding.P1Indicator.gameObject.SetActive(!string.IsNullOrEmpty(player1SelectedScene) && binding.sceneName == player1SelectedScene);

            if (binding.P2Indicator != null)
                binding.P2Indicator.gameObject.SetActive(!string.IsNullOrEmpty(player2SelectedScene) && binding.sceneName == player2SelectedScene);

            bool isLocalPick = !string.IsNullOrEmpty(localSelection) && binding.sceneName == localSelection;

            if (binding.button.image != null)
            {
                Color fallback = binding.HasOriginalColor ? binding.OriginalButtonColor : binding.button.image.color;
                binding.button.image.color = isLocalPick ? selectedButtonColor : fallback;
            }
        }
    }

    private string GetLocalSelectedLevelScene()
    {
        return isPlayerOne ? player1SelectedScene : player2SelectedScene;
    }

    private void UpdateLevelSelectionPrefs()
    {
        PlayerPrefs.SetString(Player1SelectedLevelKey, player1SelectedScene ?? string.Empty);
        PlayerPrefs.SetString(Player2SelectedLevelKey, player2SelectedScene ?? string.Empty);
        PlayerPrefs.Save();
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
            // Only the host's Ready button can ever become interactable.
            // Non-host clients can still see selections/indicators, but can't start the game.
            bool isHost = PhotonNetwork.IsMasterClient;
            readyButton.interactable = isHost && IsPlayerTwoPresent() && rolesSelected && levelSelected;
        }
    }

    /// <summary>
    /// Live check instead of a cached/PlayerPrefs flag - avoids the same kind of staleness bug
    /// that isPlayerOne had. Room player count is always accurate the moment Photon updates it.
    /// In single-player test mode, this always returns true to bypass the 2-player requirement.
    /// </summary>
    private bool IsPlayerTwoPresent()
    {
        if (singlePlayerTestMode)
            return true;

        return PhotonNetwork.CurrentRoom != null && PhotonNetwork.CurrentRoom.PlayerCount >= 2;
    }

    // ---------------------------------------------------------------
    // Feedback (Host Readiness) - single reusable TMP, no instantiate
    // ---------------------------------------------------------------

    private void HideFeedback()
    {
        if (feedbackHideRoutine != null)
        {
            StopCoroutine(feedbackHideRoutine);
            feedbackHideRoutine = null;
        }

        if (feedbackPopup != null)
            feedbackPopup.SetActive(false);
    }

    private void ShowFeedback(string message)
    {
        if (feedbackPopup == null || feedbackText == null)
        {
            Debug.LogWarning("[MLevelSelectionManager] ShowFeedback() called but feedbackPopup or feedbackText is missing. Message lost: \"" + message + "\"");
            return;
        }

        feedbackText.text = message;
        feedbackPopup.SetActive(true);

        if (feedbackHideRoutine != null)
            StopCoroutine(feedbackHideRoutine);

        feedbackHideRoutine = StartCoroutine(HideFeedbackAfterDelay());
    }

    private IEnumerator HideFeedbackAfterDelay()
    {
        yield return new WaitForSeconds(feedbackDuration);
        feedbackPopup.SetActive(false);
        feedbackHideRoutine = null;
    }

    // ---------------------------------------------------------------
    // Role Feedback - same pattern, single reusable TMP
    // ---------------------------------------------------------------

    private void ShowRoleFeedback(string message)
    {
        if (roleFeedbackPopup == null || roleFeedbackText == null)
        {
            Debug.LogWarning("[MLevelSelectionManager] ShowRoleFeedback() called but roleFeedbackPopup or roleFeedbackText is missing. Message lost: \"" + message + "\"");
            return;
        }

        roleFeedbackText.text = message;
        roleFeedbackPopup.SetActive(true);

        if (roleFeedbackHideRoutine != null)
            StopCoroutine(roleFeedbackHideRoutine);

        roleFeedbackHideRoutine = StartCoroutine(HideRoleFeedbackAfterDelay());
    }

    private IEnumerator HideRoleFeedbackAfterDelay()
    {
        yield return new WaitForSeconds(roleFeedbackDuration);
        roleFeedbackPopup.SetActive(false);
        roleFeedbackHideRoutine = null;
    }

    private void HideRoleFeedback()
    {
        if (roleFeedbackHideRoutine != null)
        {
            StopCoroutine(roleFeedbackHideRoutine);
            roleFeedbackHideRoutine = null;
        }

        if (roleFeedbackPopup != null)
            roleFeedbackPopup.SetActive(false);
    }

    private void LoadScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
            sceneName = "SampleScene";

        if (LevelTransitioner.Instance != null)
            LevelTransitioner.Instance.TransitionToLevel(sceneName);
        else
            PhotonNetwork.LoadLevel(sceneName);
    }

    private void SetCanvasGroupVisibility(CanvasGroup group, bool visible)
    {
        if (group == null)
            return;

        group.alpha = visible ? 1f : 0f;
        group.interactable = visible;
        group.blocksRaycasts = visible;
    }

    /// <summary>
    /// Display name for a role index, used in feedback messages.
    /// Role1 = "Grey", Role2 = "The Cursor".
    /// </summary>
    private string GetRoleName(int roleIndex)
    {
        switch (roleIndex)
        {
            case 1: return "Grey";
            case 2: return "The Cursor";
            default: return $"Role{roleIndex}";
        }
    }

    private void UpdateRoleButtons()
    {
        bool role1Selected = !string.IsNullOrEmpty(selectedRole1By);
        bool role2Selected = !string.IsNullOrEmpty(selectedRole2By);
        int localSelectedRole = GetLocalSelectedRoleIndex();

        if (role1SelectButton != null)
            role1SelectButton.interactable = !role1Selected && localSelectedRole != 2;
        if (role1RemoveButton != null)
            role1RemoveButton.interactable = role1Selected && selectedRole1By == GetLocalPlayerNameOrDefault();

        if (role2SelectButton != null)
            role2SelectButton.interactable = !role2Selected && localSelectedRole != 1;
        if (role2RemoveButton != null)
            role2RemoveButton.interactable = role2Selected && selectedRole2By == GetLocalPlayerNameOrDefault();

        rolesSelected = AreRolesFullySelected();
        RefreshRoleIndicators();
        UpdateReadyButtonState();
    }

    private void SetupRoleIndicators()
    {
        RectTransform anchor1 = role1IndicatorAnchor != null
            ? role1IndicatorAnchor
            : (role1SelectButton != null ? role1SelectButton.GetComponent<RectTransform>() : null);

        RectTransform anchor2 = role2IndicatorAnchor != null
            ? role2IndicatorAnchor
            : (role2SelectButton != null ? role2SelectButton.GetComponent<RectTransform>() : null);

        if (anchor1 != null)
        {
            role1Indicator = CreateIndicatorIcon(
                anchor1, player1IndicatorSprite, "RoleOwnerIndicator",
                anchor: new Vector2(1f, 0f),
                anchoredOffset: new Vector2(-indicatorMargin.x, indicatorMargin.y));
        }

        if (anchor2 != null)
        {
            role2Indicator = CreateIndicatorIcon(
                anchor2, player1IndicatorSprite, "RoleOwnerIndicator",
                anchor: new Vector2(1f, 0f),
                anchoredOffset: new Vector2(-indicatorMargin.x, indicatorMargin.y));
        }
    }

    private void RefreshRoleIndicators()
    {
        ApplyRoleIndicatorSprite(role1Indicator, selectedRole1IsPlayerOne);
        ApplyRoleIndicatorSprite(role2Indicator, selectedRole2IsPlayerOne);
    }

    private void ApplyRoleIndicatorSprite(Image indicator, bool? isPlayerOneOwner)
    {
        if (indicator == null) return;

        if (isPlayerOneOwner == null)
        {
            indicator.gameObject.SetActive(false);
            return;
        }

        indicator.sprite = isPlayerOneOwner.Value ? player1IndicatorSprite : player2IndicatorSprite;
        indicator.gameObject.SetActive(true);
    }

    private bool AreRolesFullySelected()
    {
        // In single-player test mode, role selection is not required
        if (singlePlayerTestMode)
            return true;

        return !string.IsNullOrEmpty(selectedRole1By) && !string.IsNullOrEmpty(selectedRole2By);
    }

    private void SendRoleSelection(int roleIndex, bool selected)
    {
        string playerName = GetLocalPlayerNameOrDefault();
        int localSelectedRole = GetLocalSelectedRoleIndex();

        if (selected)
        {
            if (localSelectedRole != 0 && localSelectedRole != roleIndex)
            {
                ShowRoleFeedback("You already have a role selected.");
                return;
            }

            if ((roleIndex == 1 && !string.IsNullOrEmpty(selectedRole1By)) ||
                (roleIndex == 2 && !string.IsNullOrEmpty(selectedRole2By)))
            {
                ShowRoleFeedback($"{GetRoleName(roleIndex)} is already taken.");
                return;
            }
        }
        else
        {
            if (localSelectedRole != roleIndex)
            {
                ShowRoleFeedback("You can only deselect the role you own.");
                return;
            }
        }

        object[] content = new object[] { roleIndex, selected, playerName, isPlayerOne };
        RaiseEventOptions options = new RaiseEventOptions { Receivers = ReceiverGroup.All };
        SendOptions sendOptions = new SendOptions { Reliability = true };

        PhotonNetwork.RaiseEvent(EventRoleSelectionChanged, content, options, sendOptions);
        ApplyRoleSelection(roleIndex, selected, playerName, isPlayerOne);
    }

    private void ApplyRoleSelection(int roleIndex, bool selected, string selectedBy, bool selectedByIsPlayerOne)
    {
        if (roleIndex == 1)
        {
            selectedRole1By = selected ? selectedBy : string.Empty;
            selectedRole1IsPlayerOne = selected ? (bool?)selectedByIsPlayerOne : null;
        }
        else if (roleIndex == 2)
        {
            selectedRole2By = selected ? selectedBy : string.Empty;
            selectedRole2IsPlayerOne = selected ? (bool?)selectedByIsPlayerOne : null;
        }
        else
        {
            return;
        }

        UpdateRoleSelectionPrefs();
        UpdateRoleButtons();

        if (selected)
            ShowRoleFeedback($"{selectedBy} has selected {GetRoleName(roleIndex)}!");
        else
            ShowRoleFeedback($"{selectedBy} has deselected {GetRoleName(roleIndex)}.");
    }

    private void UpdateRoleSelectionPrefs()
    {
        PlayerPrefs.SetString(Role1SelectedByKey, selectedRole1By ?? string.Empty);
        PlayerPrefs.SetString(Role2SelectedByKey, selectedRole2By ?? string.Empty);
        PlayerPrefs.SetInt(Role1IsPlayerOneKey, selectedRole1IsPlayerOne.HasValue ? (selectedRole1IsPlayerOne.Value ? 1 : 0) : -1);
        PlayerPrefs.SetInt(Role2IsPlayerOneKey, selectedRole2IsPlayerOne.HasValue ? (selectedRole2IsPlayerOne.Value ? 1 : 0) : -1);
        PlayerPrefs.SetInt(RolesSelectedKey, rolesSelected ? 1 : 0);
        PlayerPrefs.Save();
    }

    private int GetLocalSelectedRoleIndex()
    {
        string localName = GetLocalPlayerNameOrDefault();
        if (!string.IsNullOrEmpty(selectedRole1By) && selectedRole1By == localName)
            return 1;
        if (!string.IsNullOrEmpty(selectedRole2By) && selectedRole2By == localName)
            return 2;
        return 0;
    }

    /// <summary>
    /// Public accessor for other gameplay scripts (e.g. movement/cursor controllers)
    /// to check which role, if any, the local player currently owns.
    /// Returns 0 if no role is selected, 1 for Role1, 2 for Role2.
    /// </summary>
    public int GetLocalSelectedRoleIndexPublic() => GetLocalSelectedRoleIndex();

    /// <summary>
    /// Static role lookup that works from ANY scene, even ones where no MLevelSelectionManager
    /// instance exists (e.g. the actual gameplay scene). Reads directly from the PlayerPrefs
    /// values written during role selection, so it doesn't depend on this component - or its
    /// singleton Instance - surviving the scene transition triggered by PhotonNetwork.LoadLevel.
    /// Use this from gameplay scripts like MoveGray/MoveCursor instead of Instance.
    /// Returns 0 = no role, 1 = Grey, 2 = The Cursor.
    /// </summary>
    public static int GetLocalRoleIndexFromPrefs()
    {
        string role1By = PlayerPrefs.GetString(Role1SelectedByKey, string.Empty);
        string role2By = PlayerPrefs.GetString(Role2SelectedByKey, string.Empty);

        string localName;
        if (PhotonNetwork.InRoom && !string.IsNullOrEmpty(PhotonNetwork.NickName))
            localName = PhotonNetwork.NickName;
        else
            localName = PhotonNetwork.IsMasterClient
                ? PlayerPrefs.GetString(PlayerOneNameKey, "Player 1")
                : PlayerPrefs.GetString(PlayerTwoNameKey, "Player 2");

        if (!string.IsNullOrEmpty(role1By) && role1By == localName) return 1;
        if (!string.IsNullOrEmpty(role2By) && role2By == localName) return 2;
        return 0;
    }

    public void OnEvent(EventData photonEvent)
    {
        if (photonEvent.Code == EventRoleSelectionChanged)
        {
            object[] data = photonEvent.CustomData as object[];
            if (data == null || data.Length < 4)
                return;

            int roleIndex = (int)data[0];
            bool selected = (bool)data[1];
            string selectedBy = data[2] as string ?? GetLocalPlayerNameOrDefault();
            bool selectedByIsPlayerOne = data[3] is bool b && b;

            ApplyRoleSelection(roleIndex, selected, selectedBy, selectedByIsPlayerOne);
        }
        else if (photonEvent.Code == EventLevelSelectionChanged)
        {
            object[] data = photonEvent.CustomData as object[];
            if (data == null || data.Length < 2)
                return;

            bool fromPlayerOne = (bool)data[0];
            string sceneName = data[1] as string ?? string.Empty;

            ApplyLevelSelection(fromPlayerOne, sceneName);
        }
    }

    private void LoadStateFromPrefs()
    {
        playerTwoArrived = PlayerPrefs.GetInt(PlayerTwoArrivedKey, 0) == 1;
        rolesSelected = PlayerPrefs.GetInt(RolesSelectedKey, 0) == 1;
        levelSelected = PlayerPrefs.GetInt(LevelSelectedKey, 0) == 1;
        pendingLevelScene = PlayerPrefs.GetString(SelectedLevelSceneKey, string.Empty);
        selectedRole1By = PlayerPrefs.GetString(Role1SelectedByKey, string.Empty);
        selectedRole2By = PlayerPrefs.GetString(Role2SelectedByKey, string.Empty);

        int role1Flag = PlayerPrefs.GetInt(Role1IsPlayerOneKey, -1);
        selectedRole1IsPlayerOne = role1Flag == -1 ? (bool?)null : role1Flag == 1;

        int role2Flag = PlayerPrefs.GetInt(Role2IsPlayerOneKey, -1);
        selectedRole2IsPlayerOne = role2Flag == -1 ? (bool?)null : role2Flag == 1;
        player1SelectedScene = PlayerPrefs.GetString(Player1SelectedLevelKey, string.Empty);
        player2SelectedScene = PlayerPrefs.GetString(Player2SelectedLevelKey, string.Empty);
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