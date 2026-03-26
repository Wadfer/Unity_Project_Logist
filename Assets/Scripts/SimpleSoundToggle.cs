using UnityEngine;

public class SimpleSoundToggle : MonoBehaviour
{
    [Header("Объекты Иконок (из Иерархии)")]
    public GameObject soundOnIcon;  // Сюда перетащим объект SoundOn
    public GameObject soundOffIcon; // Сюда перетащим объект SoundOf

    private void Start()
    {
        // Проверяем состояние звука при запуске и включаем нужную иконку
        UpdateButtonVisual();
    }

    // Эта функция висит на кнопке (на OnClick)
    public void MuteUnmuteMusic()
    {
        if (AudioManager.Instance != null)
        {
            // Переключаем звук в менеджере
            AudioManager.Instance.ToggleSound();
            
            // Обновляем видимость иконок
            UpdateButtonVisual();
        }
    }

    private void UpdateButtonVisual()
    {
        if (AudioManager.Instance == null) return;

        bool isMuted = AudioManager.Instance.IsMuted();

        // Если звук ВЫКЛЮЧЕН (Muted)
        if (isMuted)
        {
            if (soundOnIcon != null) soundOnIcon.SetActive(false); // Прячем зеленую
            if (soundOffIcon != null) soundOffIcon.SetActive(true); // Показываем красную
        }
        // Если звук ВКЛЮЧЕН
        else
        {
            if (soundOnIcon != null) soundOnIcon.SetActive(true);  // Показываем зеленую
            if (soundOffIcon != null) soundOffIcon.SetActive(false); // Прячем красную
        }
    }
}