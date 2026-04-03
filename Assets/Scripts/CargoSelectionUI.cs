// CargoSelectionUI.cs
using UnityEngine;
using UnityEngine.UI;

public class CargoSelectionUI : MonoBehaviour
{
    public static CargoSelectionUI Instance;

    [Header("UI Основа")]
    public GameObject panel;

    [Header("Готовые слоты (Перетащи со сцены)")]
    public Button[] warehouseSlots; 
    public GameObject[] playerSlots; 

    [Header("Картинки (Спрайты)")]
    public Sprite redBoxSprite;
    public Sprite greenBoxSprite;
    public Sprite blueBoxSprite;

    private MapPoint currentWarehouse;
    private PlayerController player;
    private bool isOpen = false;

    private void Awake()
    {
        Instance = this;
        panel.SetActive(false);
    }

    // Убрали метод Update() — нам больше не нужно следить за камерой!

    public void Open(MapPoint warehouse, PlayerController p)
    {
        currentWarehouse = warehouse;
        player = p;

        panel.SetActive(true); // Просто включаем панель на экране
        isOpen = true;

        RefreshUI();
    }

    private void RefreshUI()
    {
        // 1. ОБНОВЛЯЕМ СКЛАД
        for (int i = 0; i < warehouseSlots.Length; i++)
        {
            Button btn = warehouseSlots[i];
            Image boxIcon = btn.transform.GetChild(0).GetComponent<Image>();
            btn.onClick.RemoveAllListeners();

            if (i < currentWarehouse.cargoList.Count)
            {
                CargoType cargo = currentWarehouse.cargoList[i];
                boxIcon.sprite = GetBoxSprite(cargo);
                boxIcon.enabled = true;

                if (player.myCargo.Count >= player.maxCargoCapacity)
                    btn.interactable = false;
                else
                {
                    btn.interactable = true;
                    int index = i; 
                    btn.onClick.AddListener(() => TakeCargo(index, cargo));
                }
            }
            else
            {
                boxIcon.enabled = false;
                btn.interactable = false;
            }
        }

        // 2. ОБНОВЛЯЕМ КУЗОВ ИГРОКА
        for (int i = 0; i < playerSlots.Length; i++)
        {
            Image boxIcon = playerSlots[i].transform.GetChild(0).GetComponent<Image>();
            if (i < player.myCargo.Count)
            {
                boxIcon.sprite = GetBoxSprite(player.myCargo[i]);
                boxIcon.enabled = true;
            }
            else boxIcon.enabled = false;
        }
    }

    private Sprite GetBoxSprite(CargoType type)
    {
        switch (type)
        {
            case CargoType.Red: return redBoxSprite;
            case CargoType.Green: return greenBoxSprite;
            case CargoType.Blue: return blueBoxSprite;
            default: return null;
        }
    }

    private void TakeCargo(int index, CargoType cargo)
    {
        currentWarehouse.cargoList.RemoveAt(index);
        player.myCargo.Add(cargo);

        currentWarehouse.UpdateVisuals();
        player.UpdateCargoVisuals();
        GameManager.Instance.RefreshNavigationArrows(player.myCargo);

        RefreshUI();

        if (AchievementManager.Instance != null)
        {
            AchievementManager.Instance.UnlockAchievement(3);
            if (player.myCargo.Count >= 2 && player.myCargo[0] != player.myCargo[1])
                AchievementManager.Instance.UnlockAchievement(1);
        }
    }

    public void CloseInstantly()
    {
        if (!isOpen) return;
        isOpen = false;
        panel.SetActive(false); // Просто выключаем панель
    }
}