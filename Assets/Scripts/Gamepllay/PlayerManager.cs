using System;
using System.Collections;
using Unity.VisualScripting;
using UnityEditor.SearchService;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerManager : MonoBehaviour
{
    public static PlayerManager Instance { get; private set; }

    [Header("Object References")]
    // [SerializeField] private GameObject playerPrefab;
    [SerializeField] private GameObject player;
    [SerializeField] private Transform playerSpawnPoint;
    private SpriteRenderer spriteRenderer;

    [Header("Inventory")]
    [SerializeField] private bool hasKey = false;

    [Header("Player State")]
    [SerializeField] private bool isAlive = true;

    public bool HasKey => hasKey;
    public bool IsAlive => isAlive;

    private Coroutine deathCoroutine;

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
        spriteRenderer = player.GetComponent<SpriteRenderer>();
    }

    public void CollectKey()
    {
        if (hasKey)
            return;

        hasKey = true;
        Debug.Log("PlayerManager: Key collected.");
    }

    // Function for player kill and respawn.
    public void KillPlayer()
    {
        // Safe-guarding just in case some shit happens and it calls again even if the player is still dead
        if (!isAlive)
        {
            return;
        }

        isAlive = false;
        spriteRenderer.enabled = false;
        Debug.Log("Player is alive: " + isAlive);
    }

    // Function for respawning the player.
    public void RespawnPlayer()
    {
        // Safe-guard in case this somehow gets called if the player is still alive.
        if (isAlive)
        {
            return;
        }

        isAlive = true;
        spriteRenderer.enabled = true;
        player.transform.position = playerSpawnPoint.position;
        Debug.Log("Player is alive: " + isAlive);
    }

    // Coroutine for waiting till a certain amount of seconds till respawning the player.
    private IEnumerator RespawnSequence()
    {
        KillPlayer();
        yield return new WaitForSeconds(2f);
        RespawnPlayer();
    }

    // Helper function to call for respawn sequence.
    public void StartKillAndRespawn()
    {
        if (deathCoroutine != null)
        {
            deathCoroutine = StartCoroutine(RespawnSequence());
        }
        else
        {
            StopCoroutine(RespawnSequence());
            deathCoroutine = StartCoroutine(RespawnSequence());
        }
    }

    // Unsubscribe to scene loaded event.
    // private void OnDisable()
    // {
    //     SceneManager.sceneLoaded -= OnSceneLoaded;
    // }
}