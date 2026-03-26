using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Сюда перетащи свою музыку:")]
    public AudioClip backgroundMusic; // <-- ДОБАВИЛИ ЭТО

    private AudioSource bgmSource;
    private bool isMuted = false;

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
            return;
        }

        bgmSource = GetComponent<AudioSource>();
        
        // --- ДОБАВИЛИ ЭТИ ДВЕ СТРОЧКИ ---
        if (backgroundMusic != null) bgmSource.clip = backgroundMusic; 
        
        bgmSource.loop = true; 
    }

    private void Start()
    {
        isMuted = PlayerPrefs.GetInt("IsMuted", 0) == 1;
        ApplyMuteState();

        if (!bgmSource.isPlaying)
        {
            bgmSource.Play();
        }
    }

public void ToggleSound()
    {
        isMuted = !isMuted;
        PlayerPrefs.SetInt("IsMuted", isMuted ? 1 : 0);
        PlayerPrefs.Save();
        
        ApplyMuteState();
    }

    private void ApplyMuteState()
    {
        // 1. Глобальная громкость игры (0 - тишина, 1 - звук есть)
        AudioListener.volume = isMuted ? 0f : 1f;

        // 2. Физически ставим музыку на паузу, если звук выключен
        if (isMuted)
        {
            if (bgmSource.isPlaying) bgmSource.Pause();
        }
        else
        {
            if (!bgmSource.isPlaying) bgmSource.Play();
        }
    }

    // НОВЫЙ МЕТОД: Позволяет кнопке узнать, выключен ли сейчас звук
    public bool IsMuted()
    {
        return isMuted;
    }
}