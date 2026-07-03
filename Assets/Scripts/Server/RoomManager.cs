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
        bool hasInput = !string.IsNullOrEmpty(userNameInputField.text) && 
                       !string.IsNullOrEmpty(roomNameInputField.text);
        
        createRoomButton.interactable = hasInput;
        joinRoomButton.interactable = hasInput;
    }

    public void CreateRoom()
    {
        if (PhotonNetwork.InRoom || PhotonNetwork.NetworkClientState == ClientState.Leaving)
        {
            Debug.Log("Waiting to leave previous room...");
            PhotonNetwork.LeaveRoom();
            return;
        }

        PhotonNetwork.NickName = userNameInputField.text;

        RoomOptions options = new RoomOptions { MaxPlayers = 2 };
        PhotonNetwork.CreateRoom(roomNameInputField.text, options);
    }

    public void JoinRoom()
    {
        if (PhotonNetwork.InRoom) return;

        PhotonNetwork.NickName = userNameInputField.text;
        PhotonNetwork.JoinRoom(roomNameInputField.text);
    }

    // ================= CALLBACKS =================
    public override void OnConnectedToMaster()
    {
        Debug.Log("Connected to Master Server");
        if (connectionStatusText != null)
        {
            connectionStatusText.gameObject.SetActive(true);
            connectionStatusText.text = "Server connected.";
        }
    }

    public override void OnCreatedRoom()
    {
        Debug.Log("Room Created! Loading Level Selection...");
        PhotonNetwork.LoadLevel("M_LevelSelection");
    }

    public override void OnJoinedRoom()
    {
        Debug.Log($"Joined Room! Players: {PhotonNetwork.CurrentRoom.PlayerCount} | IsMaster: {PhotonNetwork.IsMasterClient}");

        if (SceneManager.GetActiveScene().name != "M_LevelSelection")
        {
            PhotonNetwork.LoadLevel("M_LevelSelection");
        }
    }

    public override void OnLeftRoom()
    {
        Debug.Log("Left room.");
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        Debug.LogError($"Create Room Failed: {message}");
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        Debug.LogError($"Join Room Failed: {message}");
    }
}