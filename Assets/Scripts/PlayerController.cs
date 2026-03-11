using UnityEngine;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    public MapPoint currentPoint; // Текущая точка, где стоит игрок
    public float moveSpeed = 5f;
    
    private bool isMoving = false;

    private void Start()
    {
        // При старте ставим машину на точку старта
        if (currentPoint != null)
        {
            transform.position = currentPoint.transform.position;
        }
    }

    private void Update()
    {
        // Простое управление мышкой: кликаем по точке, чтобы поехать
        if (Input.GetMouseButtonDown(0) && !isMoving && GameManager.Instance.isGameActive)
        {
            HandleClick();
        }
    }

void HandleClick()
{
    Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
    RaycastHit hit;

    // Рисуем луч в сцене (видно только в окне Scene, не в Game), чтобы понять куда он летит
    Debug.DrawRay(ray.origin, ray.direction * 100, Color.red, 2f);

    if (Physics.Raycast(ray, out hit))
    {
        Debug.Log($"1. Лучь попал в объект: {hit.collider.name}");

        // Проверяем, есть ли скрипт MapPoint
        MapPoint targetPoint = hit.collider.GetComponent<MapPoint>();

        if (targetPoint != null)
        {
            Debug.Log($"2. Это точка типа: {targetPoint.type}");
            TryMove(targetPoint);
        }
        else
        {
            Debug.Log("2. На этом объекте НЕТ скрипта MapPoint!");
        }
    }
    else
    {
        Debug.Log("0. Клик в пустоту (Raycast ничего не задел)");
    }
}

    void TryMove(MapPoint target)
    {
        // Проверяем, является ли целевая точка соседом текущей
        if (currentPoint.neighbors.Contains(target))
        {
            StartCoroutine(MoveToPoint(target));
        }
        else
        {
            Debug.Log("Сюда нельзя проехать напрямую!");
        }
    }

    IEnumerator MoveToPoint(MapPoint target)
    {
        isMoving = true;

        // Движение к цели
        while (Vector3.Distance(transform.position, target.transform.position) > 0.1f)
        {
            transform.position = Vector3.MoveTowards(transform.position, target.transform.position, moveSpeed * Time.deltaTime);
            // Поворот машины к цели
            transform.LookAt(target.transform.position); 
            yield return null;
        }

        // Прибыли
        transform.position = target.transform.position;
        currentPoint = target;
        isMoving = false;

        // Логика прибития в точку
        ArriveAtPoint(target);
    }

    void ArriveAtPoint(MapPoint point)
    {
        // 1. Тратим или получаем топливо
        int fuelChange = point.GetFuelCost();
        GameManager.Instance.ModifyFuel(fuelChange);

        // 2. Проверяем тип точки
        if (point.type == PointType.Warehouse)
        {
            GameManager.Instance.PickUpCargo();
        }
        else if (point.type == PointType.Shop)
        {
            GameManager.Instance.DeliverCargo();
        }
    }
}