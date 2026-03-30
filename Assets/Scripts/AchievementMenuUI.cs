using UnityEngine;
using UnityEngine.UI;

// Вспомогательный класс для связи ID ачивки и её картинки
[System.Serializable]
public class AchievementSlot
{
    public int id;             // ID ачивки (от 1 до 10)
    public Image bannerImage;  // Сама картинка (баннер) в UI
}

public class AchievementMenuUI : MonoBehaviour
{
    [Header("Список картинок в меню")]
    public AchievementSlot[] slots;

    [Header("Настройки цветов")]
    // Цвет открытой (Color.white означает "Оригинальные цвета картинки")
    public Color unlockedColor = Color.white; 
    
    // Цвет закрытой (Темно-серый)
    public Color lockedColor = new Color(0.3f, 0.3f, 0.3f, 1f); 

    // Этот метод вызывается АВТОМАТИЧЕСКИ каждый раз, 
    // когда игрок открывает панель с ачивками
    private void OnEnable()
    {
        RefreshMenu();
    }

    public void RefreshMenu()
    {
        // Проходимся по всем ячейкам, которые ты укажешь в Инспекторе
        foreach (var slot in slots)
        {
            if (slot.bannerImage == null) continue; // Защита от ошибки

            // Проверяем память: 1 - получено, 0 - не получено
            bool isUnlocked = PlayerPrefs.GetInt("ACH_" + slot.id, 0) == 1;

            if (isUnlocked)
            {
                // Если получено - делаем картинку яркой
                slot.bannerImage.color = unlockedColor;
            }
            else
            {
                // Если НЕТ - делаем картинку темной
                slot.bannerImage.color = lockedColor;
            }
        }
    }
}