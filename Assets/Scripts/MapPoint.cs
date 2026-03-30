using UnityEngine;
using System.Collections.Generic;

public enum PointType
{
    Road,
    TrafficLight,
    RoadWorks,
    Accident,
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
    public List<CargoType> cargoList = new List<CargoType>(); 

    private Transform visualContainer;

    [Header("Навигация (Стрелки)")]
    public GameObject arrowPrefab; 
    private List<GameObject> activeArrows = new List<GameObject>(); // Список стрелок

    [Header("Знаки Проблем (2D)")]
    public GameObject warningPrefab; 
    public Sprite trafficLightSprite; 
    public Sprite roadWorksSprite;    
    public Sprite accidentSprite;    
    public Sprite gasStationSprite; 

    public int GetFuelCost()
    {
        switch (type)
        {
            case PointType.GasStation: return 5;
            case PointType.TrafficLight: return -2;
            case PointType.RoadWorks: return -3;
            case PointType.Accident: return -4;
            default: return -1;
        }
    }

    private void Start()
    {
        // При старте только вешаем знаки. Стрелки вызовет GameManager!
        SetupWarningSign();
    }

    // Метод для шариков (только Склад)
    public void UpdateVisuals()
    {
        if (visualContainer != null) Destroy(visualContainer.gameObject);
        if (type == PointType.Shop) return; 
        if (cargoList.Count == 0) return;

        visualContainer = new GameObject("VisualMarkers").transform;
        visualContainer.parent = this.transform;
        visualContainer.localPosition = Vector3.up * 2f; 

        float offset = 0f;
        foreach (CargoType cargo in cargoList)
        {
            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(marker.GetComponent<Collider>());
            marker.transform.parent = visualContainer;
            marker.transform.localScale = Vector3.one * 0.4f;
            marker.transform.localPosition = new Vector3(offset, 0, 0); 
            marker.GetComponent<Renderer>().material.color = CargoColors.GetColor(cargo);
            offset += 0.6f; 
        }
        visualContainer.localPosition -= new Vector3((offset - 0.6f) / 2, 0, 0);
    }

    // Умный метод для стрелок (Склад и Магазин)
    public void UpdateArrows(List<CargoType> playerCargo)
    {
        // 1. Стираем все старые стрелки
        foreach (GameObject arrow in activeArrows)
        {
            if (arrow != null) Destroy(arrow);
        }
        activeArrows.Clear();

        if (arrowPrefab == null) return;

        // 2. Список того, что нужно нарисовать
        List<CargoType> arrowsToShow = new List<CargoType>();

        if (type == PointType.Warehouse)
        {
            // Склад показывает всё, что у него лежит
            arrowsToShow.AddRange(cargoList);
        }
        else if (type == PointType.Shop)
        {
            // Магазин проверяет кузов игрока
            if (playerCargo != null && playerCargo.Count > 0)
            {
                // Временная копия кузова, чтобы правильно считать дубликаты
                List<CargoType> tempPlayerCargo = new List<CargoType>(playerCargo);

                foreach (CargoType neededCargo in cargoList)
                {
                    if (tempPlayerCargo.Contains(neededCargo))
                    {
                        arrowsToShow.Add(neededCargo);       // Подходит! Добавляем стрелку
                        tempPlayerCargo.Remove(neededCargo); // Вычеркиваем груз
                    }
                }
            }
        }

        // Если список пуст (кузов пустой или цвета не совпали) - выходим, ничего не рисуем
        if (arrowsToShow.Count == 0) return;

        // 3. Рисуем нужные стрелки из списка
        float spacing = 1.0f; 
        float startX = -((arrowsToShow.Count - 1) * spacing) / 2f;

        for (int i = 0; i < arrowsToShow.Count; i++)
        {
            GameObject newArrow = Instantiate(arrowPrefab);
            Vector3 spawnPos = transform.position + new Vector3(startX + (i * spacing), 0, 0);
            newArrow.transform.position = spawnPos;
            newArrow.GetComponent<FloatingArrow>().SetColor(CargoColors.GetColor(arrowsToShow[i]));
            activeArrows.Add(newArrow);
        }
    }
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
            sign.transform.position = transform.position; 
            sign.GetComponent<SpriteRenderer>().sprite = iconToShow;
        }
        if (type == PointType.TrafficLight) iconToShow = trafficLightSprite;
        else if (type == PointType.RoadWorks) iconToShow = roadWorksSprite;
        else if (type == PointType.Accident) iconToShow = accidentSprite;
        else if (type == PointType.GasStation) iconToShow = gasStationSprite;
    }
    
}