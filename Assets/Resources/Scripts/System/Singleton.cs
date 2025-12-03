using UnityEngine;

// Lớp cơ sở (Base class) cho mọi Manager
public abstract class Singleton<T> : MonoBehaviour where T : Component
{
    private static T _instance;
    private static bool _isQuitting = false;

    public static T Instance
    {
        get
        {
            if (_isQuitting)
            {
                Debug.LogWarning($"[Singleton] Instance '{typeof(T)}' already destroyed on application quit. Won't create again - returning null.");
                return null;
            }

            if (_instance == null)
            {
                // Tìm kiếm object có sẵn trong scene
                _instance = FindFirstObjectByType<T>();

                if (_instance == null)
                {
                    // Nếu không tìm thấy, tạo mới (Trường hợp gọi Instance mà chưa có Bootstrapper)
                    GameObject obj = new GameObject();
                    obj.name = typeof(T).Name;
                    _instance = obj.AddComponent<T>();
                }
            }
            return _instance;
        }
    }

    protected virtual void Awake()
    {
        if (_instance == null)
        {
            _instance = this as T;
            DontDestroyOnLoad(gameObject);
            OnAwake();
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }

    // Hàm ảo để các lớp con override thay vì dùng Awake
    protected virtual void OnAwake() { }

    protected virtual void OnApplicationQuit()
    {
        _isQuitting = true;
    }
}