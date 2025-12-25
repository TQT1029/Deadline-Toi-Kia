using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Bộ thư viện các hàm Random nâng cao.
/// Giúp tạo ra sự ngẫu nhiên có kiểm soát, tự nhiên và công bằng hơn cho game.
/// </summary>
public static class RandomUtilities
{
    /// <summary>
    /// Random một số float nhưng bị "khóa" vào các bước nhảy (Grid Snapping).
    /// <para><b>Công dụng:</b> Dùng để đặt vị trí vật thể sao cho thẳng hàng lối, không bị lẻ số. 
    /// Ví dụ: steps=0.5 thì kết quả chỉ có thể là 1.0, 1.5, 2.0... (không bao giờ ra 1.234).</para>
    /// </summary>
    /// <param name="minInp">Giá trị nhỏ nhất.</param>
    /// <param name="maxInp">Giá trị lớn nhất.</param>
    /// <param name="steps">Bước nhảy (Khoảng cách giữa các giá trị).</param>
    public static float RandomWithSteps(float minInp, float maxInp, float steps = 0.5f)
    {
        if (steps <= 0f)
        {
            Debug.LogWarning("Steps phải lớn hơn 0");
            return minInp;
        }

        // Tính xem có bao nhiêu bước nhảy trong khoảng min-max
        int stepCount = Mathf.RoundToInt((maxInp - minInp) / steps);

        // Random chọn một bước thứ n
        int randIndex = Random.Range(0, stepCount + 1);

        // Tính ra giá trị thực tế
        return minInp + randIndex * steps;
    }

    /// <summary>
    /// Tính xác suất xảy ra một sự kiện dựa trên phần trăm (0-100%).
    /// <para><b>Công dụng:</b> Dùng cho các quyết định Có/Không. 
    /// Ví dụ: "Có spawn hố hay không?" (pitChance), "Có spawn quái hay không?".</para>
    /// </summary>
    /// <param name="percentage">Tỉ lệ phần trăm thành công (0 đến 100).</param>
    public static bool ChancePercent(float percentage)
    {
        if (percentage <= 0f) return false;
        if (percentage >= 100f) return true;

        // Random từ 0 đến 100, nếu nhỏ hơn mức phần trăm thì trúng
        return Random.Range(0f, 100f) < percentage;
    }

    /// <summary>
    /// Tính xác suất dựa trên trọng số (Weight) của vật phẩm so với tổng trọng số.
    /// <para><b>Công dụng:</b> Dùng để chọn vật phẩm trong danh sách (Loot Table). 
    /// Vật có weight cao sẽ dễ ra hơn, vật weight thấp sẽ hiếm hơn.</para>
    /// </summary>
    public static bool ChanceWeight(float weight, float totalWeight)
    {
        if (weight <= 0f) return false;
        if (weight >= totalWeight) return true;
        return Random.Range(0f, totalWeight) < weight;
    }

    /// <summary>
    /// Tạo độ cao mượt mà với biên độ thay đổi động (Dynamic Amplitude).
    /// <para><b>Công dụng:</b> Tạo ra các đoạn đường có lúc nhấp nhô rất cao (biên độ lớn), 
    /// có lúc lại phẳng lặng (biên độ nhỏ), nhưng luôn giữ được sự mượt mà liên kết.</para>
    /// </summary>
    /// <param name="x">Vị trí trục X.</param>
    /// <param name="scale">Độ gắt của địa hình (Tần số).</param>
    /// <param name="minH">Độ cao thấp nhất tuyệt đối.</param>
    /// <param name="maxH">Độ cao cao nhất tuyệt đối.</param>
    /// <param name="step">Bước nhảy làm tròn.</param>
    public static float GetDynamicWaveHeight(float x, float scale, float minH, float maxH, float step = 0.5f)
    {
        // 1. Sóng chính (Base Wave): Tạo ra đường đi mượt mà cơ bản
        // Trả về giá trị 0..1
        float baseWave = Mathf.PerlinNoise(x * scale, 0f);

        // 2. Điều biến biên độ (Amplitude Modulation): 
        // Dùng một lớp noise khác tần số thấp hơn (scale * 0.5) để điều khiển "độ gắt".
        // offset 100f để nó không trùng với sóng chính.
        float ampMod = Mathf.PerlinNoise(x * (scale * 0.5f) + 100f, 10f);

        // ampMod = 0 -> Biên độ hẹp (gần trung bình cộng).
        // ampMod = 1 -> Biên độ rộng (tiến về minH/maxH).

        // Tính trung điểm
        float midH = (minH + maxH) / 2f;

        // Lerp biên min/max dựa trên ampMod
        // Nếu ampMod thấp, min/max sẽ co về giữa -> đường đi phẳng hơn.
        // Nếu ampMod cao, min/max dãn ra -> đường đi dốc hơn, biên độ lớn.
        float currentMin = Mathf.Lerp(midH, minH, ampMod);
        float currentMax = Mathf.Lerp(midH, maxH, ampMod);

        // Map sóng chính vào khoảng biên độ động này
        float rawHeight = Mathf.Lerp(currentMin, currentMax, baseWave);

        // Làm tròn theo step
        if (step > 0)
        {
            float snapped = Mathf.Round(rawHeight / step) * step;
            return Mathf.Clamp(snapped, minH, maxH);
        }
        return Mathf.Clamp(rawHeight, minH, maxH);
    }

    /// <summary>
    /// Tạo độ cao ngẫu nhiên nhưng "mượt mà" và liên kết với nhau (Perlin Noise).
    /// <para><b>Công dụng:</b> Thay vì các tấm ván nhảy lung tung (cái cao tít, cái sát đất), 
    /// hàm này tạo ra độ cao lượn sóng tự nhiên. Tấm sau sẽ có độ cao gần tương đồng tấm trước.</para>
    /// </summary>
    /// <param name="xPosition">Vị trí trục X hiện tại (làm mốc lấy mẫu).</param>
    /// <param name="scale">Độ "gắt" của địa hình. (0.1 = đồi thoai thoải, 0.5 = núi dốc).</param>
    /// <param name="minHeight">Độ cao thấp nhất.</param>
    /// <param name="maxHeight">Độ cao cao nhất.</param>
    /// <param name="step">Làm tròn kết quả theo bước (để khớp với grid game).</param>
    public static float GetPerlinHeight(float xPosition, float scale, float minHeight, float maxHeight, float step = 0.5f)
    {
        // Lấy giá trị từ bản đồ nhiễu (0.0 đến 1.0)
        float noiseValue = Mathf.PerlinNoise(xPosition * scale, 0f);

        // Chuyển đổi từ khoảng 0..1 sang khoảng minHeight..maxHeight
        float rawHeight = Mathf.Lerp(minHeight, maxHeight, noiseValue);

        // Làm tròn số nếu cần thiết
        if (step > 0)
        {
            float snapped = Mathf.Round(rawHeight / step) * step;
            return Mathf.Clamp(snapped, minHeight, maxHeight);
        }
        return rawHeight;
    }

    /// <summary>
    /// Hệ thống "Túi Tráo Bài" (Shuffle Bag / Deck System).
    /// <para><b>Công dụng:</b> Đảm bảo tính công bằng ("Fair Random"). 
    /// Thay vì random hoàn toàn (có thể ra 10 lần Obstacle A liên tiếp), hệ thống này giống như bộ bài:
    /// Rút hết các lá bài trong túi rồi mới tráo lại. Đảm bảo mọi loại Obstacle đều được xuất hiện đều đặn.</para>
    /// </summary>
    public class ShuffleBag<T>
    {
        private List<T> originalData; // Dữ liệu gốc để nạp lại khi túi rỗng
        private List<T> currentBag;   // Cái túi hiện tại đang rút dần

        public ShuffleBag(List<T> initialData)
        {
            this.originalData = new List<T>(initialData);
            this.currentBag = new List<T>();
        }

        /// <summary>
        /// Rút một món đồ từ túi. Nếu túi rỗng sẽ tự động nạp đầy và tráo lại.
        /// </summary>
        public T Next()
        {
            // Nếu túi rỗng, nạp lại và xào bài
            if (currentBag.Count == 0)
            {
                currentBag.AddRange(originalData);
                Shuffle(currentBag);
            }

            // Rút lá bài đầu tiên ra
            T item = currentBag[0];
            currentBag.RemoveAt(0);
            return item;
        }

        // Thuật toán tráo bài Fisher-Yates (Xáo trộn danh sách)
        private void Shuffle(List<T> list)
        {
            for (int i = 0; i < list.Count; i++)
            {
                T temp = list[i];
                int randomIndex = Random.Range(i, list.Count);
                list[i] = list[randomIndex];
                list[randomIndex] = temp;
            }
        }
    }
}