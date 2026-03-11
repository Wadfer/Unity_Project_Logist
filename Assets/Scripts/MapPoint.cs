using UnityEngine;
using System.Collections.Generic;

public enum PointType
{
    Road,       // Обычная дорога (-1 топлива)
    BadRoad,    // Плохая дорога (от -1 до -3)
    GasStation, // Заправка (+топливо)
    Warehouse,  // Склад (взять груз)
    Shop        // Магазин (сдать груз)
}

public class MapPoint : MonoBehaviour
{
    [Header("Настройки точки")]
    public PointType type = PointType.Road;
    public List<MapPoint> neighbors; // Соседние точки, куда можно поехать отсюда

    [Header("Параметры топлива")]
    public int fuelRefillAmount = 5; // Сколько дает заправка

    // Метод для расчета изменения топлива при попадании сюда
    public int GetFuelCost()
    {
        switch (type)
        {
            case PointType.GasStation:
                return fuelRefillAmount; // Восполняем топливо (положительное число)
            
            case PointType.BadRoad:
                return Random.Range(-3, 0); // Случайное число от -3 до -1 (Random.Range для int не включает макс. число, поэтому ставим 0, если хотим до -1)
                // Исправлено: Random.Range(int min, int max) - max не включается. 
                // Чтобы было от -3 до -1 включительно, нужно писать Random.Range(-3, -1 + 1) -> (-3, 0).
                
            default:
                return -1; // Обычный расход
        }
    }

    // Вспомогательная линия в редакторе, чтобы видеть связи
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        foreach (var neighbor in neighbors)
        {
            if (neighbor != null)
                Gizmos.DrawLine(transform.position, neighbor.transform.position);
        }
    }
        private void Start()
    {
        // Получаем доступ к "рисовалке" объекта
        Renderer ren = GetComponent<Renderer>();
        
        if (ren != null)
        {
            switch (type)
            {
                case PointType.GasStation:
                    ren.material.color = Color.green; // Заправка
                    break;
                case PointType.BadRoad:
                    ren.material.color = new Color(0.5f, 0, 0); // Темно-красный (Опасность)
                    break;
                case PointType.Warehouse:
                    ren.material.color = Color.blue; // Склад
                    break;
                case PointType.Shop:
                    ren.material.color = Color.yellow; // Магазин
                    break;
                default:
                    ren.material.color = Color.gray; // Обычная дорога
                    break;
            }
        }
    }
}