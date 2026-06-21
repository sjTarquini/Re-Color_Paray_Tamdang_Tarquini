using UnityEngine;

public class InGameAudioManager : MonoBehaviour
{
    public static InGameAudioManager Instance { get; private set; }

    [Header("Player Sounds")]
    public AudioClip jumpSound;
    public AudioClip crayonCollectSound;
    public AudioClip deathInstakillSound;
    public AudioClip deathBorderSound;

    [Header("Level Sounds")]
    public AudioClip levelCompleteSound;

    [Header("Object Sounds")]
    public AudioClip yellowBlueCombineSound;

    private AudioSource audioSource;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
    }

    private void Start()
    {
        // Pre-warm the audio system silently using a volume scale of 0
        if (jumpSound != null)
            audioSource.PlayOneShot(jumpSound, 0f);
    }

    public void PlaySound(AudioClip clip, float volume = 1f)
    {
        if (clip != null)
            audioSource.PlayOneShot(clip, volume);
    }
}