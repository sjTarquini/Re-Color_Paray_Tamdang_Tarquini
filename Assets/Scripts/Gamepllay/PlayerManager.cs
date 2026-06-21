using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    public static PlayerManager Instance { get; private set; }

    [Header("Object References")]
    [SerializeField] private GameObject player;

    [Header("Inventory")]
    [SerializeField] private bool hasKey = false;
    [SerializeField] private bool hasCrayon = false;

    [Header("Player State")]
    [SerializeField] private bool isAlive = true;

    public bool HasKey => hasKey;
    public bool HasCrayon => hasCrayon;
    public bool IsAlive => isAlive;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void CollectKey()
    {
        if (hasKey) return;
        hasKey = true;
    }

    public void CollectCrayon()
    {
        if (hasCrayon) return;
        hasCrayon = true;
    }
}
