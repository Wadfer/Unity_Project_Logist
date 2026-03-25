using UnityEngine;
using System.Collections.Generic;

// 1. Обновляем список типов точек
public enum PointType
{
    Road,           // Обычная дорога (-1 топливо)
    TrafficLight,   // Светофор (-2 топлива)
    RoadWorks,      // Дорожные работы (-3 топлива)
    Accident,       // Авария (-4 топлива)
    GasStation,     // Заправка (+5 топлива)
    Warehouse,      // Склад (-1 топливо)
    Shop            // Магазин (-1 топливо)
}

public class MapPoint : MonoBehaviour
{
    [Header("Настройки точки")]
    public PointType type = PointType.Road;
    public List<MapPoint> neighbors;

    [Header("Список Грузов")]
    public List<CargoType> cargoList = new List<CargoType>(); 
    public float arrowHeight = 4f; 
    
    private Transform visualContainer;

    [Header("Навигация (Стрелки)")]
    public GameObject arrowPrefab; 
    private GameObject myArrow;    

    // --- НОВЫЙ БЛОК: Иконки проблем ---
    [Header("Знаки Проблем (2D)")]
    public GameObject warningPrefab; // Сюда кидаем WarningSignPrefab
    public Sprite trafficLightSprite; // Картинка светофора
    public Sprite roadWorksSprite;    // Картинка ремонта
    public Sprite accidentSprite;     // Картинка аварии 
    public float warningHeight = 1.5f;

    // 2. Обновляем математику расхода топлива
    public int GetFuelCost()
    {
        switch (type)
        {
            case PointType.GasStation: 
                return 5;  // Дает 5 топлива

            case PointType.TrafficLight: 
                return -2; // -1 за ход, -1 за проблему

            case PointType.RoadWorks: 
                return -3; // -1 за ход, -2 за проблему

            case PointType.Accident: 
                return -4; // -1 за ход, -3 за проблему

            default: 
                return -1; // Обычная дорога, Склад и Магазин (-1 за перемещение)
        }
    }

    private void Start()
    {
        // Создаем стрелку, если префаб задан
        if (arrowPrefab != null)
        {
            myArrow = Instantiate(arrowPrefab);
            
            // ИЗМЕНЕНИЕ ЗДЕСЬ: Просто ставим стрелку в центр точки на земле.
            // Скрипт FloatingArrow сам поднимет её на нужную высоту!
            myArrow.transform.position = transform.position; 
            
            myArrow.SetActive(false); // Скрываем по умолчанию
        }

        // Вызов создания знаков проблем
        SetupWarningSign();
    }

    // Метод, который проверяет тип точки и вешает нужную картинку
    private void SetupWarningSign()
    {
        if (warningPrefab == null) return;

        Sprite iconToShow = null;

        if (type == PointType.TrafficLight) iconToShow = trafficLightSprite;
        else if (type == PointType.RoadWorks) iconToShow = roadWorksSprite;
        else if (type == PointType.Accident) iconToShow = accidentSprite;

        if (iconToShow != null)
        {
            GameObject sign = Instantiate(warningPrefab);
            
            // ИЗМЕНЕНИЕ: Мы просто ставим знак в центр точки на земле.
            // Скрипт FloatingWarning сам поднимет его на свои minHeight и maxHeight!
            sign.transform.position = transform.position; 
            
            sign.GetComponent<SpriteRenderer>().sprite = iconToShow;
        }
    }
    // Метод: Показать стрелку определенного цвета
    public void ShowGuideArrow(bool show, Color color = default)
    {
        if (myArrow != null)
        {
            myArrow.SetActive(show);
            if (show && color != default)
            {
                myArrow.GetComponent<FloatingArrow>().SetColor(color);
            }
        }
    }

    // Метод для обновления шариков над точкой
    public void UpdateVisuals()
    {
        // 1. Удаляем старые маркеры, если они были
        if (visualContainer != null) Destroy(visualContainer.gameObject);

        // 2. Если список пуст - ничего не рисуем
        if (cargoList.Count == 0) return;

        // 3. Создаем новый контейнер
        visualContainer = new GameObject("VisualMarkers").transform;
        visualContainer.parent = this.transform;
        visualContainer.localPosition = Vector3.up * 2f; // Поднимаем над точкой

        // 4. Рисуем каждый груз в ряд
        float offset = 0f;
        foreach (CargoType cargo in cargoList)
        {
            GameObject marker;
            
            // Для магазина делаем Кубики, для склада - Сферы (чтобы различать)
            if (type == PointType.Shop) marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            else marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);

            // Убираем лишний коллайдер
            Destroy(marker.GetComponent<Collider>());

            // Настройка размера и цвета
            marker.transform.parent = visualContainer;
            marker.transform.localScale = Vector3.one * 0.4f;
            marker.transform.localPosition = new Vector3(offset, 0, 0); // Сдвигаем каждый следующий
            
            marker.GetComponent<Renderer>().material.color = CargoColors.GetColor(cargo);

            offset += 0.6f; // Расстояние между шариками
        }
        
        // Центрируем весь ряд, чтобы было красиво
        visualContainer.localPosition -= new Vector3((offset - 0.6f) / 2, 0, 0);
    }
}