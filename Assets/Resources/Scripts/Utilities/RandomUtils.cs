using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Cơ chế phá hủy khi thay thế GameObject.
/// </summary>
public enum ReplaceDestroyMode
{
    /// <summary>
    /// Thay thế ngay lập tức (Instantiate mới → Destroy cũ).
    /// </summary>
    Immediate,

    /// <summary>
    /// Hủy object cũ sau một khoảng delay (dùng cho animation, VFX).
    /// </summary>
    Delay,

    /// <summary>
    /// Không destroy object cũ, chỉ disable (phù hợp object pooling).
    /// </summary>
    DisableOnly
}


/// <summary>
/// Bộ thư viện các hàm Random nâng cao.
/// Giúp tạo ra sự ngẫu nhiên có kiểm soát, tự nhiên và công bằng hơn cho game.
/// </summary>
public static class RandomUtils
{
    /// <summary>
    /// Random một số float nhưng bị "khóa" vào các bước nhảy (Grid Snapping).
    /// <para><b>Công dụng:</b> Dùng để đặt vị trí vật thể sao cho thẳng hàng lối, không bị lẻ số. 
    /// Ví dụ: steps=0.5 thì kết quả chỉ có thể là 1.0, 1.5, 2.0... (không bao giờ ra 1.234).</para>
    /// </summary>  
    /// <param name="min">Giá trị nhỏ nhất.</param>
    /// <param name="max">Giá trị lớn nhất.</param>
    /// <param name="steps">Bước nhảy (Khoảng cách giữa các giá trị).</param>
    public static float RandomWithSteps(float min, float max, float steps = 0.5f)
    {
        if (steps <= 0f)
        {
            Debug.LogWarning("Steps phải lớn hơn 0");
            return min;
        }
        else if (min > max)
        {
            Debug.LogWarning("min phải nhỏ hơn hoặc bằng max");
            return min;
        }

        // Tính xem có bao nhiêu bước nhảy trong khoảng min-max
        int stepCount = Mathf.RoundToInt((max - min) / steps);

        // Random chọn một bước thứ n
        int randIndex = Random.Range(0, stepCount + 1);

        // Tính ra giá trị thực tế
        return min + randIndex * steps;
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
    /// Tạo độ cao theo hình sóng Sin (Lượn sóng đều đặn).
    /// <para><b>Công dụng:</b> Tạo ra các platform lên xuống nhịp nhàng. Phù hợp cho các đoạn ngắn cần thay đổi độ cao rõ rệt.</para>
    /// </summary>
    /// <param name="x">Vị trí trục X.</param>
    /// <param name="frequency">Tần số (Độ gắt). Giá trị càng cao, sóng càng ngắn (nhấp nhô nhanh hơn).</param>
    /// <param name="minH">Độ cao thấp nhất.</param>
    /// <param name="maxH">Độ cao cao nhất.</param>
    /// <param name="phaseOffset">Độ lệch pha (để mỗi lần spawn sóng bắt đầu ở vị trí khác nhau).</param>
    /// <param name="step">Bước nhảy làm tròn (Grid snapping).</param>
    public static float GetSineWaveHeight(float x, float frequency, float minH, float maxH, float phaseOffset, float step = 0.5f)
    {
        // Công thức Sin: sin(x * tần số + pha) -> trả về giá trị từ -1 đến 1
        float sineValue = Mathf.Sin((x * frequency) + phaseOffset);

        // Chuyển từ khoảng [-1, 1] sang [0, 1] để dễ Lerp
        float normalizedSine = (sineValue + 1f) / 2f;

        // Nội suy ra độ cao thực tế
        float rawHeight = Mathf.Lerp(minH, maxH, normalizedSine);

        // Làm tròn theo step (nếu cần thẳng hàng lối)
        if (step > 0)
        {
            float snapped = Mathf.Round(rawHeight / step) * step;
            return Mathf.Clamp(snapped, minH, maxH);
        }
        return rawHeight;
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
    /// Thay thế một GameObject hiện tại bằng một GameObject khác dựa trên xác suất phần trăm,
    /// kèm theo cơ chế phá hủy có kiểm soát.
    /// </summary>
    /// <para>
    /// <b>Công dụng:</b>
    /// - Nâng cấp platform / enemy
    /// - Biến đổi vật thể theo RNG
    /// - Kết hợp animation, VFX trước khi phá hủy
    /// </para>
    public static GameObject ReplaceWithChance(
        GameObject original,
        GameObject replacePrefab,
        float chancePercent,
        ReplaceDestroyMode destroyMode = ReplaceDestroyMode.Immediate,
        float destroyDelay = 0f,
        bool keepParent = true,
        System.Action<GameObject> onBeforeDestroy = null
    )
    {
        // Kiểm tra an toàn
        if (original == null || replacePrefab == null)
            return original;

        // Không trúng xác suất → giữ nguyên object
        if (!ChancePercent(chancePercent))
            return original;

        // Lưu transform gốc
        Transform oldTransform = original.transform;
        Transform parent = keepParent ? oldTransform.parent : null;
        int siblingIndex = oldTransform.GetSiblingIndex();

        // Instantiate object mới
        GameObject newObj = Object.Instantiate(
            replacePrefab,
            oldTransform.position,
            oldTransform.rotation,
            parent
        );

        newObj.transform.localScale = oldTransform.localScale;

        if (keepParent)
            newObj.transform.SetSiblingIndex(siblingIndex);

        // Callback trước khi phá (rất hữu ích cho animation, VFX, event)
        onBeforeDestroy?.Invoke(original);

        // Cơ chế phá hủy
        switch (destroyMode)
        {
            case ReplaceDestroyMode.Immediate:
                Object.Destroy(original);
                break;

            case ReplaceDestroyMode.Delay:
                Object.Destroy(original, Mathf.Max(0f, destroyDelay));
                break;

            case ReplaceDestroyMode.DisableOnly:
                original.SetActive(false);
                break;
        }

        return newObj;
    }
    /// <summary>
    /// Random số thực (float) KHÔNG TRÙNG trong một khoảng [min, max] dựa theo bước nhảy (step).
    /// Mỗi mức giá trị chỉ xuất hiện 1 lần cho tới khi bốc hết thì tự động nạp lại túi.
    /// </summary>
    /// <para><b>Ví dụ:</b> min=0, max=1, step=0.5 -> Túi sẽ có [0.0, 0.5, 1.0].</para>
    public class FloatShuffleBag
    {
        private List<float> bag = new List<float>();
        private float min;
        private float max;
        private float step;

        public FloatShuffleBag(float minInclusive, float maxInclusive, float stepSize)
        {
            min = minInclusive;
            max = maxInclusive;
            step = stepSize;
            Refill();
        }

        /// <summary>
        /// Lấy giá trị float tiếp theo trong túi.
        /// </summary>
        public float Next()
        {
            if (bag.Count == 0)
                Refill();

            int index = Random.Range(0, bag.Count);
            float value = bag[index];
            bag.RemoveAt(index);

            return value;
        }

        /// <summary>
        /// Nạp lại toàn bộ dãy số và trộn.
        /// </summary>
        private void Refill()
        {
            bag.Clear();

            if (step <= 0f)
            {
                Debug.LogWarning("FloatShuffleBag: Step phải lớn hơn 0!");
                bag.Add(min);
                return;
            }

            int stepCount = Mathf.RoundToInt((max - min) / step);

            for (int i = 0; i <= stepCount; i++)
            {
                // Công thức này đảm bảo không bao giờ bị dồn sai số float
                bag.Add(min + i * step);
            }
        }
    }

    /// <summary>
    /// Random số nguyên KHÔNG TRÙNG trong một khoảng [min, max).
    /// Mỗi số chỉ xuất hiện 1 lần cho tới khi dùng hết thì reset.
    /// </summary>
    public class IntShuffleBag
    {
        private List<int> bag = new List<int>();
        private int min;
        private int max;

        public IntShuffleBag(int minInclusive, int maxExclusive)
        {
            min = minInclusive;
            max = maxExclusive;
            Refill();
        }

        /// <summary>
        /// Lấy số tiếp theo (không trùng).
        /// </summary>
        public int Next()
        {
            if (bag.Count == 0)
                Refill();

            int index = Random.Range(0, bag.Count);
            int value = bag[index];
            bag.RemoveAt(index);

            return value;
        }

        /// <summary>
        /// Nạp lại toàn bộ dãy số và trộn.
        /// </summary>
        private void Refill()
        {
            bag.Clear();
            for (int i = min; i < max; i++)
                bag.Add(i);
        }
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

        public ShuffleBag(IEnumerable<T> initialData)
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
    }/// <summary>
     /// Random chọn 1 phần tử trong danh sách dựa trên tỉ lệ % được chỉ định trước.
     /// <para><b>Công dụng:</b> Bốc gacha, rớt đồ (loot drop), hoặc chọn quái vật để spawn. 
     /// Nếu danh sách dài hơn số lượng % truyền vào, số % còn thiếu (để đủ 100%) sẽ được chia đều cho các phần tử còn lại.</para>
     /// </summary>
     /// <param name="list">Danh sách các phần tử cần bốc.</param>
     /// <param name="percentages">Các mức phần trăm tương ứng với từng phần tử (từ trái qua phải).</param>
     /// <returns>Phần tử được chọn ngẫu nhiên.</returns>
    public static T RandomWithDistributedPercent<T>(List<T> list, params float[] percentages)
    {
        // 1. Kiểm tra an toàn: Nếu list trống hoặc null thì báo lỗi và trả về giá trị mặc định
        if (list == null || list.Count == 0)
        {
            Debug.LogError("RandomUtils: Danh sách truyền vào đang trống!");
            return default;
        }

        // Mảng chứa tỉ lệ % CHÍNH THỨC của từng phần tử sau khi đã tính toán
        float[] finalWeights = new float[list.Count];
        float givenSum = 0f;

        // 2. Gán % cho các phần tử ĐÃ ĐƯỢC CHỈ ĐỊNH thông qua params
        int providedCount = Mathf.Min(list.Count, percentages.Length);
        for (int i = 0; i < providedCount; i++)
        {
            float p = Mathf.Max(0f, percentages[i]); // Chống lỗi truyền số âm
            finalWeights[i] = p;
            givenSum += p;
        }

        // 3. Xử lý % còn dư cho các phần tử CHƯA ĐƯỢC GÁN
        int remainingElements = list.Count - providedCount;
        if (remainingElements > 0)
        {
            // Tính lượng % còn lại (tối thiểu là 0 để không bị âm nếu user nhập lố 100%)
            float remainingPercent = Mathf.Max(0f, 100f - givenSum);

            // Chia đều cho các phần tử chưa có %
            float sharedPercent = remainingPercent / remainingElements;

            for (int i = providedCount; i < list.Count; i++)
            {
                finalWeights[i] = sharedPercent;
            }
        }
        else if (givenSum > 100f)
        {
            // Cảnh báo nếu user nhập danh sách % mà cộng lại vượt quá 100%
            Debug.LogWarning("RandomUtils: Tổng các % truyền vào lớn hơn 100%! Hàm sẽ tự động quy đổi lại theo tỉ lệ tương đối.");
        }

        // 4. Bắt đầu quay số ngẫu nhiên (Vòng quay Gacha)
        // Dùng max(100f, givenSum) để bao hàm trường hợp tổng > 100% thì vẫn lấy random chuẩn xác
        float totalWeight = Mathf.Max(100f, givenSum);
        float randomVal = Random.Range(0f, totalWeight);
        float currentSum = 0f;

        // Dò xem con số random rơi vào "khoảng" của phần tử nào
        for (int i = 0; i < list.Count; i++)
        {
            currentSum += finalWeights[i];
            if (randomVal <= currentSum)
            {
                return list[i]; // Trúng phần tử nào thì trả về phần tử đó
            }
        }

        // Fallback an toàn (tránh lỗi float precision ở góc làm tròn cuối cùng)
        return list[list.Count - 1];
    }
}