using UnityEngine;
using System.Collections.Generic; // Нужно для списков

public enum PointType
{
    Road,
    BadRoad,
    GasStation,
    Warehouse,
    Shop
}

public class MapPoint : MonoBehaviour
{
    [Header("Настройки точки")]
    public PointType type = PointType.Road;
    public List<MapPoint> neighbors;

    [Header("Список Грузов")]
    // Для Склада: список того, что здесь лежит и можно забрать.
    // Для Магазина: список того, что сюда нужно привезти (заказы).
    public List<CargoType> cargoList = new List<CargoType>(); 
    
    // Контейнер для визуальных маркеров, чтобы не мусорить в иерархии
    private Transform visualContainer;

    public int GetFuelCost()
    {
        switch (type)
        {
            case PointType.GasStation: return 5;
            case PointType.BadRoad: return Random.Range(-3, 0);
            default: return -1;
        }
    }

    private void Start()
    {
        if (type == PointType.Warehouse || type == PointType.Shop)
        {
            UpdateVisuals();
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