using UnityEngine;

/// <summary>
/// Các hàm tiện ích random.
/// </summary>
public static class RandomUtilities
{
    /// <summary>
    /// Random một số float trong khoảng [minInp, maxInp] với bước nhảy (steps).
    /// Ví dụ: min=1.5, max=3, step=0.5 → kết quả có thể là 1.5, 2.0, 2.5, 3.0.
    /// </summary>
    /// <param name="minInp">Giá trị nhỏ nhất</param>
    /// <param name="maxInp">Giá trị lớn nhất</param>
    /// <param name="steps">Bước nhảy</param>
    /// <returns>Giá trị float random theo bước nhảy</returns>
    public static float RandomWithSteps(float minInp, float maxInp, float steps=0.5f)
    {
        if (steps <= 0f)
        {
            Debug.LogWarning("Steps phải lớn hơn 0");
            return minInp;
        }

        // Tính số lượng bước
        int stepCount = Mathf.RoundToInt((maxInp - minInp) / steps);

        // Random số nguyên trong khoảng [0, stepCount]
        int randIndex = Random.Range(0, stepCount + 1);

        // Tính giá trị kết quả
        float result = minInp + randIndex * steps;
        return result;
    }

    /// <summary>
    /// Hàm tính xác suất dựa trên phần trăm (0-100%).
    /// </summary>
    /// <param name="percentage">phần trăm đầu vào.</param>
    /// <returns>Trả về true nếu giá trị random ra bé hơn phần trăm đầu vào.</returns>
    public static bool ChancePercent(float percentage)
    {
        if (percentage <= 0f) return false;
        if (percentage >= 100f) return true;
        float randValue = Random.Range(0f, 100f);
        return randValue < percentage;
    }

    /// <summary>
    /// Hàm tính xác suất dựa trên trọng số (weight) so với tổng trọng số (totalWeight).
    /// </summary>
    /// <param name="weight">Trọng số của vật</param>
    /// <param name="totalWeight">Tổng trọng số tất cả các vật</param>
    /// <returns>Trả về true nếu giá trị random ra bé hơn trọng số.</returns>
    public static bool ChanceWeight(float weight, float totalWeight)
    {
        if (weight <= 0f) return false;
        if (weight >= totalWeight) return true;
        float randValue = Random.Range(0f, totalWeight);
        return randValue < weight;
    }
}
