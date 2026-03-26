using UnityEngine;

public class SimpleSoundToggle : MonoBehaviour
{
    // Эту функцию мы просто привяжем к твоему клику
    public void MuteUnmuteMusic()
    {
        if (AudioManager.Instance != null)
        {
            // Отправляем команду в главный менеджер звука
            AudioManager.Instance.ToggleSound();
        }
    }
}