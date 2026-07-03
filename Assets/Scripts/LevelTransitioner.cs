using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Photon.Pun;                    // ← ADD THIS

public class LevelTransitioner : MonoBehaviour
{
    public static LevelTransitioner Instance { get; private set; }

    [Header("Canvas & Animator 1: Moving TO a Level")]
    [SerializeField] private GameObject toLevelCanvas;
    [SerializeField] private Animator toLevelAnimator;
    [SerializeField] private string toLevelStateName = "ToLevel";

    [Header("Canvas & Animator 2: Loading FROM a Level")]
    [SerializeField] private GameObject fromLevelCanvas;
    [SerializeField] private Animator fromLevelAnimator;
    [SerializeField] private string fromLevelStateName = "FromLevel";

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        if (toLevelCanvas != null) toLevelCanvas.SetActive(false);
        if (fromLevelCanvas != null) fromLevelCanvas.SetActive(false);
    }

    /// <summary>
    /// Use this for multiplayer (called by Master Client)
    /// </summary>
    public void TransitionToLevel(string sceneName)
    {
        StartCoroutine(TransitionSequence(sceneName));
    }

    private IEnumerator TransitionSequence(string sceneName)
    {
        Debug.Log($"[LevelTransitioner] Starting transition to {sceneName} | IsMaster: {PhotonNetwork.IsMasterClient}");

        // Play TO LEVEL animation
        if (toLevelCanvas != null && toLevelAnimator != null)
        {
            toLevelCanvas.SetActive(true);
            toLevelAnimator.Play(toLevelStateName);
            yield return new WaitForSeconds(1.5f); // or wait for animation properly
        }

        // LOAD SCENE
        if (PhotonNetwork.IsConnected && PhotonNetwork.InRoom)
        {
            if (PhotonNetwork.IsMasterClient)
            {
                PhotonNetwork.LoadLevel(sceneName);
            }
            yield break; // Non-masters wait for Photon
        }
        else
        {
            // Solo mode
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
            while (!asyncLoad.isDone) yield return null;
        }

        if (toLevelCanvas != null) toLevelCanvas.SetActive(false);
    }

    // Keep your death/reload method (single-player friendly)
    public void ReloadCurrentLevel()
    {
        string currentSceneName = SceneManager.GetActiveScene().name;
        StartCoroutine(ReloadCurrentLevelSequence(currentSceneName));
    }

    private IEnumerator ReloadCurrentLevelSequence(string sceneName)
    {
        yield return StartCoroutine(TransitionSequence(sceneName));
    }
}