using UnityEngine;

public static class Bootstrapper
{
    // Chạy trước khi Scene đầu tiên được load
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void Execute()
    {
        var systemPrefab = Resources.Load(GameConstants.PATH_SYSTEM_PREFABS);

        if (systemPrefab == null)
        {
            Debug.LogError($"[Bootstrapper] Không tìm thấy file tại Resources/{GameConstants.PATH_SYSTEM_PREFABS}");
            return;
        }

        GameObject systemObject = Object.Instantiate((GameObject)systemPrefab);
        systemObject.name = "[System Managers]";
        Object.DontDestroyOnLoad(systemObject);
    }
}