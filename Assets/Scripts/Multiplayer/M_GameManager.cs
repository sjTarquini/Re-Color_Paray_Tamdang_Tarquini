using UnityEngine;
using Photon.Pun;

/// <summary>
/// Instantiates the player for multiplayer networking.
/// </summary>
public class M_GameManager : MonoBehaviour
{
    public static M_GameManager Instance;

    [SerializeField] private Transform PlayerSpawnPoint;
    [SerializeField] private GameObject PlayerObject = null;
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("More than one instance of M_GameManager. Destroying clone.");
            Destroy(this);
        }
        else
        {
            Instance = this;
        }
    }

    // Spawn player at start up ONLY if player is set as role = 1.
    void Start()
    {
        // int role = MLevelSelectionManager.GetLocalRoleIndexFromPrefs();
        // if (role == 1)
        // {
            if (PlayerObject == null && PlayerSpawnPoint != null)
            {
                PlayerObject = PhotonNetwork.Instantiate("Prefabs/Multiplayer/M_Gray", PlayerSpawnPoint.position, PlayerSpawnPoint.rotation);
                Debug.Log("Player prefab spawn successful.");
            }
            else
            {
                Debug.LogError("Player prefab spawn unsuccessful.");
            }
        
    }
}
