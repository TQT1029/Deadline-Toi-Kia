using UnityEngine;

/// <summary>
/// Bộ thư viện các hàm Toán học và Noise nâng cao.
/// </summary>
public static class MathUtils
{
    /// <summary>
    /// Tạo giá trị Perlin Noise 1D ngẫu nhiên nhưng liên tục và giới hạn nó trong khoảng [minNoise, maxNoise].
    /// <para><b>Công dụng:</b> Tạo độ rung lắc (Camera shake), lơ lửng, hoặc thay đổi các thông số (gió, âm lượng) theo thời gian một cách tự nhiên.</para>
    /// </summary>
    /// <param name="x">Giá trị đầu vào di chuyển liên tục (Thường dùng: Time.time * tốc độ).</param>
    /// <param name="minNoise">Giá trị thấp nhất mong muốn trả về.</param>
    /// <param name="maxNoise">Giá trị cao nhất mong muốn trả về.</param>
    /// <param name="seed">Hạt giống để tách biệt các chuỗi noise. (Ví dụ: Trục X dùng seed 0, trục Y dùng seed 100 để 2 trục không rung giống hệt nhau).</param>
    /// <returns>Giá trị float ngẫu nhiên mượt mà từ minNoise đến maxNoise.</returns>
    public static float ClampPerlinNoise1D(float x, float minNoise, float maxNoise, float seed = 0f)
    {
        float rawNoise;
        if (seed == 0f)
        {
            rawNoise = Mathf.PerlinNoise1D(x);
        }
        else
        {
            rawNoise = Mathf.PerlinNoise(x, seed);
        }
        float safeNoise = Mathf.Clamp01(rawNoise);

        return Mathf.Lerp(minNoise, maxNoise, safeNoise);
    }

    /// <summary>
    /// Lấy mẫu Perlin Noise 2D (trên một mặt phẳng) và giới hạn trong khoảng [minNoise, maxNoise].
    /// <para><b>Công dụng:</b> Dùng để tạo địa hình (Terrain generation), phân bố tài nguyên trên bản đồ (quặng, cây cối), hoặc tạo mây.</para>
    /// </summary>
    /// <param name="x">Tọa độ trục X (thường là tọa độ thế giới nhân với độ chi tiết scale).</param>
    /// <param name="y">Tọa độ trục Y (hoặc Z trong môi trường 3D).</param>
    /// <param name="minNoise">Độ cao/giá trị thấp nhất.</param>
    /// <param name="maxNoise">Độ cao/giá trị cao nhất.</param>
    /// <param name="seedOffset">Độ lệch (Offset) để xáo trộn toàn bộ bản đồ. Đổi số này thì map sinh ra sẽ hoàn toàn khác.</param>
    /// <returns>Giá trị float nội suy theo bản đồ nhiễu 2D.</returns>
    public static float ClampPerlinNoise2D(float x, float y, float minNoise, float maxNoise, float seedOffset = 0f)
    {
        // 1. CỘNG SEED VÀO TỌA ĐỘ: Dời vị trí trích xuất mẫu noise trên bản đồ vô tận của Unity để tạo ra một map mới.
        float sampleX = x + seedOffset;
        float sampleY = y + seedOffset;

        float rawNoise = Mathf.PerlinNoise(sampleX, sampleY);

        // 2. KHÓA GIÁ TRỊ VÀ NỘI SUY: Giống như 1D, bước này bảo vệ logic cứng không bị văng ra khỏi biên giới min/max.
        float safeNoise = Mathf.Clamp01(rawNoise);
        return Mathf.Lerp(minNoise, maxNoise, safeNoise);
    }
}