using UnityEngine;
using UnityEngine.UI; // Обязательно для кнопок

public class LevelSelectionManager : MonoBehaviour
{
    [Header("Кнопки уровней (по порядку 1, 2, 3...)")]
    public Button[] levelButtons; 

    private void Start()
    {
        RefreshLevels();
    }

    // Эту функцию можно вызывать принудительно, если меню открывается не сразу
    private void OnEnable()
    {
        RefreshLevels();
    }

    public void RefreshLevels()
    {
        // Читаем из памяти открытый уровень (по умолчанию 1)
        int maxLevel = PlayerPrefs.GetInt("UnlockedLevel", 1);

        // Проходимся по всем кнопкам
        for (int i = 0; i < levelButtons.Length; i++)
        {
            // i = 0 это Уровень 1. i = 1 это Уровень 2 и т.д.
            int levelIndex = i + 1; 

            if (levelIndex <= maxLevel)
            {
                // Уровень открыт (Кнопка нажимается)
                levelButtons[i].interactable = true;
            }
            else
            {
                // Уровень закрыт (Кнопка серая и не нажимается)
                levelButtons[i].interactable = false;
            }
        }
    }
}