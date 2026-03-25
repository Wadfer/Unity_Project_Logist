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

    [Header("Груз")]
    public List<CargoType> myCargo = new List<CargoType>();
    public int maxCargoCapacity = 2;
    
    [Header("Визуал Груза")]
    public GameObject[] cargoVisuals; // Массив ссылок на кубики (2 штуки)

    [Header("Звуки и Эффекты")]
    public AudioClip engineSound;
    public AudioClip arriveSound;
    public ParticleSystem exhaustParticles;

    private AudioSource audioSource;
    private bool isMoving = false;
    private float lastMoveTime = 0f;

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.loop = true; audioSource.clip = engineSound; audioSource.Stop();
        if(exhaustParticles != null) exhaustParticles.Stop();

        if (currentPoint != null) 
        {
            // Ставим машинку на точку
            transform.position = currentPoint.transform.position;

            // --- НОВАЯ ЛОГИКА: Проверяем, не склад ли это? ---
            if (currentPoint.type == PointType.Warehouse)
            {
                bool tookCargo = false; // Флаг, взяли ли мы что-то
                
                // Забираем груз, пока есть место и пока на складе что-то есть
                while (currentPoint.cargoList.Count > 0 && myCargo.Count < maxCargoCapacity)
                {
                    CargoType taken = currentPoint.cargoList[0];
                    currentPoint.cargoList.RemoveAt(0);
                    myCargo.Add(taken);
                    Debug.Log($"Взят груз при старте: {taken}");
                    tookCargo = true;
                }

                // Если груз был взят, обновляем визуал склада (чтобы шарики пропали)
                if (tookCargo)
                {
                    currentPoint.UpdateVisuals();
                }
            }
        }
        
        // Обновляем кубики в кузове машинки
        UpdateCargoVisuals();

        // Важный момент: Обновляем стрелки навигации с микро-задержкой.
        // Это нужно, чтобы GameManager точно успел загрузиться и не перебил наши стрелки.
        Invoke(nameof(UpdateArrowsDelayed), 0.1f);
    }

    // Вспомогательный метод для обновления стрелок при старте
    private void UpdateArrowsDelayed()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RefreshNavigationArrows(myCargo);
        }
    }

    private void Update()
    {
        if (Mouse.current == null) return;
        if (Mouse.current.leftButton.wasPressedThisFrame && !isMoving && GameManager.Instance.isGameActive)
        {
            HandleClick();
        }
    }

    // !!! ЭТОТ МЕТОД БЫЛ ПОТЕРЯН, Я ЕГО ВЕРНУЛ !!!
    void HandleClick()
    {
        Vector2 mousePos = Mouse.current.position.ReadValue();
        Ray ray = Camera.main.ScreenPointToRay(mousePos);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            MapPoint targetPoint = hit.collider.GetComponent<MapPoint>();
            if (targetPoint != null)
            {
                TryMove(targetPoint);
            }
        }
    }

    // !!! ЭТОТ ТОЖЕ ВЕРНУЛ !!!
    void TryMove(MapPoint target)
    {
        if (Time.time - lastMoveTime < 0.5f) return;
        if (isMoving) return;

        if (currentPoint.neighbors.Contains(target))
        {
            lastMoveTime = Time.time;
            isMoving = true;
            StartCoroutine(MoveToPoint(target));
        }
    }

    // !!! И ЭТОТ !!!
    IEnumerator MoveToPoint(MapPoint target)
    {
        if(engineSound != null) audioSource.Play();
        if(exhaustParticles != null) exhaustParticles.Play();

        while (Vector3.Distance(transform.position, target.transform.position) > 0.1f)
        {
            transform.position = Vector3.MoveTowards(transform.position, target.transform.position, moveSpeed * Time.deltaTime);
            transform.LookAt(target.transform.position); 
            yield return null;
        }

        transform.position = target.transform.position;
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

        // ЛОГИКА СКЛАДА
        if (point.type == PointType.Warehouse)
        {
            while (point.cargoList.Count > 0 && myCargo.Count < maxCargoCapacity)
            {
                CargoType taken = point.cargoList[0];
                point.cargoList.RemoveAt(0);
                myCargo.Add(taken);
                Debug.Log($"Взят груз: {taken}");
            }
            point.UpdateVisuals();
            UpdateCargoVisuals();
        }
        // ЛОГИКА МАГАЗИНА
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

    void UpdateCargoVisuals()
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