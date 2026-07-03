using UnityEngine;
using Photon.Pun;

public class ServerLoad : MonoBehaviourPunCallbacks
{
    [SerializeField] private RoomManager roomManager;

    void Start()
    {
        PhotonNetwork.ConnectUsingSettings();
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("Connected to Master Server");

        if (roomManager != null)
        {
            roomManager.OnConnectedToMaster();
        }
    }
}