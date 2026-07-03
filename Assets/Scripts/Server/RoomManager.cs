using UnityEngine;
using Photon.Pun;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using Photon.Realtime;

public class RoomManager : MonoBehaviourPunCallbacks
{
    [Header("UI Elements")]
    [SerializeField] private TMP_InputField userNameInputField;
    [SerializeField] private TMP_InputField roomNameInputField;
    [SerializeField] private Button createRoomButton;
    [SerializeField] private Button joinRoomButton;
    [SerializeField] private TextMeshProUGUI connectionStatusText;

    private void Awake()
    {
        PhotonNetwork.AutomaticallySyncScene = true;

        if (connectionStatusText != null)
            connectionStatusText.gameObject.SetActive(false);

        createRoomButton.interactable = false;
        joinRoomButton.interactable = false;
    }

    private void Update()
    {
        bool hasValidInput = !string.IsNullOrEmpty(userNameInputField.text) && 
                            !string.IsNullOrEmpty(roomNameInputField.text);
        
        createRoomButton.interactable = hasValidInput;
        joinRoomButton.interactable = hasValidInput;
    }

    public void CreateRoom()
    {
        if (PhotonNetwork.InRoom)
        {
            PhotonNetwork.LeaveRoom();
            return;
        }

        PhotonNetwork.NickName = userNameInputField.text;

        RoomOptions options = new RoomOptions
        {
            MaxPlayers = 2,
            IsVisible = true,
            IsOpen = true
        };

        PhotonNetwork.CreateRoom(roomNameInputField.text, options);
    }

    public void JoinRoom()
    {
        if (PhotonNetwork.InRoom) return;

        PhotonNetwork.NickName = userNameInputField.text;
        PhotonNetwork.JoinRoom(roomNameInputField.text);
    }

    public void ReturnToMainMenu()
    {
        PhotonNetwork.Disconnect();
        SceneManager.LoadScene("MainMenu");
    }

    // ====================== CALLBACKS ======================

    public override void OnConnectedToMaster()
    {
        base.OnConnectedToMaster();
        SetConnectedToMaster(true);
        Debug.Log("Connected to Master Server");
    }

    public override void OnCreatedRoom()
    {
        Debug.Log($"Room created successfully: {PhotonNetwork.CurrentRoom.Name}");
        PhotonNetwork.LoadLevel("M_LevelSelection");
    }

    public override void OnJoinedRoom()
    {
        Debug.Log($"Joined room: {PhotonNetwork.CurrentRoom.Name} | PlayerCount: {PhotonNetwork.CurrentRoom.PlayerCount}");
        
        // Only load if not already in the level selection scene
        if (SceneManager.GetActiveScene().name != "M_LevelSelection")
        {
            PhotonNetwork.LoadLevel("M_LevelSelection");
        }
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        Debug.LogError($"Join Room Failed: {message} (Code: {returnCode})");
        // TODO: Show message to player (room doesn't exist, full, etc.)
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        Debug.LogError($"Create Room Failed: {message} (Code: {returnCode})");
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        base.OnDisconnected(cause);
        Debug.Log($"Disconnected: {cause}");
        SceneManager.LoadScene("GameType");
    }

    public void SetConnectedToMaster(bool connected)
    {
        if (connectionStatusText != null)
        {
            connectionStatusText.gameObject.SetActive(connected);
            connectionStatusText.text = connected ? "Server connected." : "";
        }
    }
}