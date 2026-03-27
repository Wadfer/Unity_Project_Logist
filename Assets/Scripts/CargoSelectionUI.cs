using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class CargoSelectionUI : MonoBehaviour
{
    public static CargoSelectionUI Instance;

    [Header("UI Основа")]
    public GameObject panel;
    public CanvasGroup canvasGroup;
    public float heightOffset = 3f;

    [Header("Готовые слоты (Перетащи со сцены)")]
    [Tooltip("3 верхние кнопки склада")]
    public Button[] warehouseSlots; 
    
    [Tooltip("2 нижних слота игрока (пустые рамки)")]
    public GameObject[] playerSlots; 

    [Header("Картинки (Спрайты)")]
    public Sprite redBoxSprite;
    public Sprite greenBoxSprite;
    public Sprite blueBoxSprite;

    private MapPoint currentWarehouse;
    private PlayerController player;
    private Camera mainCam;
    private bool isOpen = false;

    private void Awake()
    {
        Instance = this;
        mainCam = Camera.main;
        panel.SetActive(false);
    }

    private void Update()
    {
        if (isOpen && mainCam != null)
        {
            Vector3 dir = mainCam.transform.position - transform.position;
            if (dir != Vector3.zero) transform.rotation = Quaternion.LookRotation(-dir);
        }
    }

    public void Open(MapPoint warehouse, PlayerController p)
    {
        currentWarehouse = warehouse;
        player = p;

        transform.position = warehouse.transform.position + Vector3.up * heightOffset;
        panel.SetActive(true);
        isOpen = true;

        RefreshUI();
    }

    private void RefreshUI()
    {
        // 1. ОБНОВЛЯЕМ СКЛАД (ВЕРХНИЙ РЯД)
        for (int i = 0; i < warehouseSlots.Length; i++)
        {
            Button btn = warehouseSlots[i];
            // Находим картинку коробки внутри кнопки (первый дочерний объект)
            Image boxIcon = btn.transform.GetChild(0).GetComponent<Image>();
            
            // Сбрасываем старые нажатия
            btn.onClick.RemoveAllListeners();

            // Если для этого слота есть груз на складе
            if (i < currentWarehouse.cargoList.Count)
            {
                CargoType cargo = currentWarehouse.cargoList[i];
                boxIcon.sprite = GetBoxSprite(cargo);
                boxIcon.enabled = true; // Показываем коробку

                // Если кузов полон - кнопку нажимать нельзя
                if (player.myCargo.Count >= player.maxCargoCapacity)
                {
                    btn.interactable = false;
                }
                else
                {
                    btn.interactable = true;
                    int index = i; // Обязательно сохраняем индекс для кнопки
                    btn.onClick.AddListener(() => TakeCargo(index, cargo));
                }
            }
            else
            {
                // Груза нет - прячем коробку (желтая рамка остается), отключаем кнопку
                boxIcon.enabled = false;
                btn.interactable = false;
            }
        }

        // 2. ОБНОВЛЯЕМ КУЗОВ ИГРОКА (НИЖНИЙ РЯД)
        for (int i = 0; i < playerSlots.Length; i++)
        {
            // Находим картинку коробки внутри слота
            Image boxIcon = playerSlots[i].transform.GetChild(0).GetComponent<Image>();

            if (i < player.myCargo.Count)
            {
                // Показываем груз, который лежит в кузове
                boxIcon.sprite = GetBoxSprite(player.myCargo[i]);
                boxIcon.enabled = true;
            }
            else
            {
                // Место пустое - прячем коробку (желтая рамка остается)
                boxIcon.enabled = false;
            }
        }

        // Если все забрали или забили кузов полностью - можно не закрывать автоматически,
        // игрок сам уедет и меню закроется. Но если хочешь авто-закрытие - раскомментируй:
        // if (currentWarehouse.cargoList.Count == 0 || player.myCargo.Count >= player.maxCargoCapacity) CloseSmoothly();
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
    }
    public void CloseInstantly()
    {
        if (!isOpen) return;
        isOpen = false;
        StopAllCoroutines(); // На всякий случай останавливаем появление, если оно шло
        panel.SetActive(false); // Выключаем мгновенно
    }
}