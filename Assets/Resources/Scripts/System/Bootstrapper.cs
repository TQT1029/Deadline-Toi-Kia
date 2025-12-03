using UnityEngine;

public static class Bootstrapper
{
    // Chạy trước khi Scene đầu tiên được load
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void Execute()
    {
        // Đường dẫn: Resources/SystemPrefabs/SystemManagers.prefab
        var systemPrefab = Resources.Load("SystemPrefabs/SystemManagers");

        if (systemPrefab == null)
        {
            Debug.LogError("[Bootstrapper] Không tìm thấy file 'SystemManagers' trong folder Resources/SystemPrefabs/.");
            return;
        }

        // Tạo object gốc chứa tất cả hệ thống và không bao giờ hủy nó
        GameObject systemObject = Object.Instantiate((GameObject)systemPrefab);
        systemObject.name = "[System Managers]"; // Đặt tên cho dễ nhìn
        Object.DontDestroyOnLoad(systemObject);
    }
}