using UnityEngine;

public class RoadVisualizer : MonoBehaviour
{
    public Material roadMaterial; // Материал дороги (цвет)
    public float roadWidth = 0.5f;

    void Start()
    {
        // Находим все точки на карте
        MapPoint[] allPoints = FindObjectsOfType<MapPoint>();

        foreach (var point in allPoints)
        {
            if (point.neighbors == null) continue;

            foreach (var neighbor in point.neighbors)
            {
                // Чтобы не рисовать дорогу дважды (от А к Б и от Б к А), 
                // проверяем уникальный ID или просто рисуем, если это проще
                if (neighbor != null)
                {
                    CreateLine(point.transform.position, neighbor.transform.position);
                }
            }
        }
    }

    void CreateLine(Vector3 start, Vector3 end)
    {
        // Создаем пустой объект для линии
        GameObject lineObj = new GameObject("RoadConnection");
        lineObj.transform.parent = this.transform; // Делаем дочерним, чтобы не мусорить

        LineRenderer lr = lineObj.AddComponent<LineRenderer>();
        
        // Настройки линии
        lr.startWidth = roadWidth;
        lr.endWidth = roadWidth;
        lr.positionCount = 2;
        lr.SetPosition(0, start);
        lr.SetPosition(1, end);
        
        // Материал
        if(roadMaterial != null) lr.material = roadMaterial;
    }
}