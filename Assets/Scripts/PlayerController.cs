using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;


[RequireComponent(typeof(AudioSource))]
public class PlayerController : MonoBehaviour
{
    [Header("Навигация")]
    public MapPoint currentPoint;
    public float moveSpeed = 5f;
    
    // --- НОВАЯ НАСТРОЙКА ВЫСОТЫ МАШИНЫ ---
    [Tooltip("На сколько поднять машину над дорогой, чтобы она не проваливалась")]
    public float heightOffset = 0.5f; 

    [Header("Груз")]
    public List<CargoType> myCargo = new List<CargoType>();
    public int maxCargoCapacity = 2;
    
    [Header("Визуал Груза")]
    public GameObject[] cargoVisuals;

    [Header("Звуки и Эффекты")]
    public AudioClip engineSound;
    public AudioClip arriveSound;
    public ParticleSystem exhaustParticles;

    private AudioSource audioSource;
    private bool isMoving = false;
    private float lastMoveTime = 0f;

    public void Start()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.loop = true; audioSource.clip = engineSound; audioSource.Stop();
        if(exhaustParticles != null) exhaustParticles.Stop();

        if (currentPoint != null) 
        {
            // ИЗМЕНЕНИЕ: Прибавляем heightOffset при старте игры
            transform.position = currentPoint.transform.position + Vector3.up * heightOffset;

            if (currentPoint.type == PointType.Warehouse)
        {
            // Открываем UI меню вместо автоматического забора
            CargoSelectionUI.Instance.Open(currentPoint, this);
        }
        }
        
        UpdateCargoVisuals();
        Invoke(nameof(UpdateArrowsDelayed), 0.1f);
    }

    private void UpdateArrowsDelayed()
    {
        if (GameManager.Instance != null) GameManager.Instance.RefreshNavigationArrows(myCargo);
    }

    private void Update()
    {
        if (Mouse.current == null) return;
        if (Mouse.current.leftButton.wasPressedThisFrame && !isMoving && GameManager.Instance.isGameActive)
        {
            HandleClick();
        }
    }

    void HandleClick()
    {
        Vector2 mousePos = Mouse.current.position.ReadValue();
        Ray ray = Camera.main.ScreenPointToRay(mousePos);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            MapPoint targetPoint = hit.collider.GetComponent<MapPoint>();
            if (targetPoint != null) TryMove(targetPoint);
        }
    }

    void TryMove(MapPoint target)
    {
        if (Time.time - lastMoveTime < 0.5f) return;
        if (isMoving) return;

        if (currentPoint.neighbors.Contains(target))
        {
            // --- МЕНЯЕМ ЭТУ СТРОЧКУ НА МГНОВЕННОЕ ЗАКРЫТИЕ ---
            if (CargoSelectionUI.Instance != null) CargoSelectionUI.Instance.CloseInstantly();

            lastMoveTime = Time.time;
            isMoving = true;
            StartCoroutine(MoveToPoint(target));
        }
    }

    IEnumerator MoveToPoint(MapPoint target)
    {
        if(engineSound != null) audioSource.Play();
        if(exhaustParticles != null) exhaustParticles.Play();

        // ИЗМЕНЕНИЕ: Вычисляем финальную точку с учетом нашей высоты
        Vector3 targetPosition = target.transform.position + Vector3.up * heightOffset;

        while (Vector3.Distance(transform.position, targetPosition) > 0.05f)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);
            
            // Заставляем машину смотреть в точку назначения (также приподнятую)
            transform.LookAt(targetPosition); 
            
            yield return null;
        }

        // Фиксируем позицию в конце пути
        transform.position = targetPosition;
        currentPoint = target;
        isMoving = false;
        
        audioSource.Stop();
        if(exhaustParticles != null) exhaustParticles.Stop();
        if(arriveSound != null) audioSource.PlayOneShot(arriveSound);

        ArriveAtPoint(target);
    }

    void ArriveAtPoint(MapPoint point)
    {
        int fuelChange = point.GetFuelCost();
        GameManager.Instance.ModifyFuel(fuelChange);

        if (point.type == PointType.Warehouse)
        {
            // Открываем UI меню вместо автоматического забора
            CargoSelectionUI.Instance.Open(point, this);
        }
        else if (point.type == PointType.Shop)
        {
            for (int i = myCargo.Count - 1; i >= 0; i--)
            {
                CargoType cargo = myCargo[i];
                if (point.cargoList.Contains(cargo))
                {
                    point.cargoList.Remove(cargo);
                    myCargo.RemoveAt(i);
                    GameManager.Instance.DeliverCargo();
                }
            }
            point.UpdateVisuals();
            UpdateCargoVisuals();
        }

        GameManager.Instance.RefreshNavigationArrows(myCargo);
    }

    public void UpdateCargoVisuals()
    {
        for (int i = 0; i < cargoVisuals.Length; i++)
        {
            if (i < myCargo.Count)
            {
                cargoVisuals[i].SetActive(true);
                cargoVisuals[i].GetComponent<Renderer>().material.color = CargoColors.GetColor(myCargo[i]);
            }
            else
            {
                cargoVisuals[i].SetActive(false);
            }
        }
    }
}