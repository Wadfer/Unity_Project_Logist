using UnityEngine;
using UnityEngine.InputSystem; // Обязательно для новой системы ввода
using System.Collections;

[RequireComponent(typeof(AudioSource))] // Автоматически добавит компонент звука
public class PlayerController : MonoBehaviour
{
    [Header("Навигация")]
    public MapPoint currentPoint; // Текущая точка
    public float moveSpeed = 5f;  // Скорость машины

    [Header("Груз")]
    public CargoType currentCargo = CargoType.None; // Какой груз везем
    public GameObject cargoVisualObject; // Ссылка на кубик в кузове (нужно перетащить в инспекторе)

    [Header("Звуки")]
    public AudioClip engineSound; // Звук мотора (Loop)
    public AudioClip arriveSound; // Звук прибытия (One Shot)

    [Header("Эффекты")]
    public ParticleSystem exhaustParticles; // Дым из трубы

    // Внутренние переменные
    private AudioSource audioSource;
    private bool isMoving = false;
    private float lastMoveTime = 0f; // Таймер для защиты от двойных кликов

    private void Start()
    {
        // Настройка звука
        audioSource = GetComponent<AudioSource>();
        audioSource.loop = true; 
        audioSource.clip = engineSound;
        audioSource.Stop(); 

        // Настройка дыма
        if(exhaustParticles != null) exhaustParticles.Stop();

        // Настройка позиции
        if (currentPoint != null)
        {
            transform.position = currentPoint.transform.position;
        }

        // Скрываем груз на старте, если его нет
        if (cargoVisualObject != null) cargoVisualObject.SetActive(false);
    }

    private void Update()
    {
        // Если мыши нет (например, геймпад), выходим
        if (Mouse.current == null) return;

        // Проверка клика левой кнопкой
        if (Mouse.current.leftButton.wasPressedThisFrame && !isMoving && GameManager.Instance.isGameActive)
        {
            HandleClick();
        }
    }

    void HandleClick()
    {
        // Пускаем луч от мышки
        Vector2 mousePos = Mouse.current.position.ReadValue();
        Ray ray = Camera.main.ScreenPointToRay(mousePos);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            // Пытаемся получить компонент точки
            MapPoint targetPoint = hit.collider.GetComponent<MapPoint>();

            if (targetPoint != null)
            {
                TryMove(targetPoint);
            }
        }
    }

    void TryMove(MapPoint target)
    {
        // ЗАЩИТА: Если прошло меньше 0.5 сек с прошлого клика - игнорируем
        if (Time.time - lastMoveTime < 0.5f) return;
        
        // Если уже едем - игнорируем
        if (isMoving) return;

        // Проверяем, является ли точка соседом
        if (currentPoint.neighbors.Contains(target))
        {
            lastMoveTime = Time.time; // Запоминаем время клика
            isMoving = true; // Блокируем движение сразу
            
            StartCoroutine(MoveToPoint(target));
        }
        else
        {
            Debug.Log("Туда нельзя проехать напрямую!");
        }
    }

    IEnumerator MoveToPoint(MapPoint target)
    {
        // --- СТАРТ ДВИЖЕНИЯ ---
        if(engineSound != null) audioSource.Play(); // Звук вкл
        if(exhaustParticles != null) exhaustParticles.Play(); // Дым вкл

        // Поворот и перемещение
        while (Vector3.Distance(transform.position, target.transform.position) > 0.1f)
        {
            transform.position = Vector3.MoveTowards(transform.position, target.transform.position, moveSpeed * Time.deltaTime);
            transform.LookAt(target.transform.position); 
            yield return null;
        }

        // --- ФИНИШ ---
        transform.position = target.transform.position;
        currentPoint = target;
        isMoving = false;
        
        audioSource.Stop(); // Звук выкл
        if(exhaustParticles != null) exhaustParticles.Stop(); // Дым выкл
        if(arriveSound != null) audioSource.PlayOneShot(arriveSound); // "Дзынь"

        // Логика прибытия
        ArriveAtPoint(target);
    }

       void ArriveAtPoint(MapPoint point)
    {
        // 1. Топливо
        int fuelChange = point.GetFuelCost();
        GameManager.Instance.ModifyFuel(fuelChange);

        // 2. ЛОГИКА СКЛАДА (Multi-Cargo)
        if (point.type == PointType.Warehouse)
        {
            // Берем груз, только если машина пустая
            if (currentCargo == CargoType.None)
            {
                // Проверяем, осталось ли что-то на складе
                if (point.cargoList.Count > 0)
                {
                    // Берем самый первый груз из списка (индекс 0)
                    CargoType takenCargo = point.cargoList[0];
                    
                    // Удаляем его со склада (физически забираем)
                    point.cargoList.RemoveAt(0);
                    
                    // Обновляем визуал точки (шарик исчезнет)
                    point.UpdateVisuals();

                    // Кладем себе в кузов
                    PickUpCargo(takenCargo);
                }
                else
                {
                    Debug.Log("Склад пуст!");
                }
            }
            else
            {
                Debug.Log("У вас уже занят кузов! Доставьте текущий груз.");
            }
        }
        
        // 3. ЛОГИКА МАГАЗИНА (Multi-Cargo)
        else if (point.type == PointType.Shop)
        {
            if (currentCargo != CargoType.None)
            {
                // Проверяем, нужен ли магазину НАШ груз
                if (point.cargoList.Contains(currentCargo))
                {
                    // Удаляем заказ из списка магазина (выполнено)
                    point.cargoList.Remove(currentCargo);
                    point.UpdateVisuals(); // Кубик исчезнет

                    DeliverCargo();
                }
                else
                {
                    Debug.Log($"Магазину не нужен {currentCargo}. Ему нужны: " + string.Join(", ", point.cargoList));
                }
            }
        }
    }

    void PickUpCargo(CargoType type)
    {
        currentCargo = type;
        Debug.Log($"Взят груз: {type}");
        
        // Включаем визуал (кубик в багажнике)
        if (cargoVisualObject != null)
        {
            cargoVisualObject.SetActive(true);
            // Красим кубик в цвет груза
            cargoVisualObject.GetComponent<Renderer>().material.color = CargoColors.GetColor(type);
        }
        
        GameManager.Instance.PickUpCargo(); // Обновляем UI
    }

    void DeliverCargo()
    {
        Debug.Log($"Груз {currentCargo} успешно доставлен!");
        currentCargo = CargoType.None;

        // Выключаем визуал
        if (cargoVisualObject != null)
        {
            cargoVisualObject.SetActive(false);
        }
        
        GameManager.Instance.DeliverCargo(); // Победа или +очки
    }
}