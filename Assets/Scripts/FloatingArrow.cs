using UnityEngine;

public class FloatingArrow : MonoBehaviour
{
    public float speed = 2f;
    public float height = 0.5f;
    
    private Vector3 startPos;

    void Start()
    {
        startPos = transform.localPosition;
    }

    void Update()
    {
        // Плавное движение вверх-вниз по синусоиде
        float newY = startPos.y + Mathf.Sin(Time.time * speed) * height;
        transform.localPosition = new Vector3(startPos.x, newY, startPos.z);
        
        // Вращение вокруг своей оси
        transform.Rotate(0, 50 * Time.deltaTime, 0);
    }
    
    // Метод для покраски стрелки
    public void SetColor(Color color)
    {
        // Находим рендерер (учитываем, что модель может быть внутри)
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (var r in renderers)
        {
            r.material.color = color;
        }
    }
}