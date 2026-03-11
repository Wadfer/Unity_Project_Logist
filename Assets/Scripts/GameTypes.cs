using UnityEngine;
// Просто список вариантов
public enum CargoType
{
    None,   // Пусто
    Red,    // Красная посылка
    Green,  // Зеленая посылка
    Blue    // Синяя посылка
}

// Вспомогательный класс, чтобы легко получать цвета
public static class CargoColors
{
    public static Color GetColor(CargoType type)
    {
        switch (type)
        {
            case CargoType.Red: return Color.red;
            case CargoType.Green: return Color.green;
            case CargoType.Blue: return Color.blue;
            default: return Color.white;
        }
    }
}