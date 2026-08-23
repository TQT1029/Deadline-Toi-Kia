using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Hệ thống Object Pool generic hiệu năng cao cho Unity.
/// Tái sử dụng GameObject / Component thay vì gọi Instantiate / Destroy liên tục.
/// </summary>
public static class SimpleObjectPool<T> where T : Component
{
    private static readonly Dictionary<int, Queue<T>> _pools = new Dictionary<int, Queue<T>>();
    private static readonly Dictionary<int, int> _instanceToPrefabMap = new Dictionary<int, int>();

    /// <summary>
    /// Lấy một instance từ Pool hoặc Instantiate mới nếu Pool đang rỗng.
    /// </summary>
    public static T Get(T prefab, Transform parent = null)
    {
        if (prefab == null) return null;

        int prefabId = prefab.gameObject.GetInstanceID();

        if (!_pools.TryGetValue(prefabId, out Queue<T> poolQueue))
        {
            poolQueue = new Queue<T>();
            _pools[prefabId] = poolQueue;
        }

        T instance = null;

        while (poolQueue.Count > 0)
        {
            instance = poolQueue.Dequeue();
            if (instance != null)
            {
                break;
            }
        }

        if (instance == null)
        {
            instance = Object.Instantiate(prefab, parent);
        }
        else
        {
            if (parent != null) instance.transform.SetParent(parent);
            instance.gameObject.SetActive(true);
        }

        _instanceToPrefabMap[instance.gameObject.GetInstanceID()] = prefabId;
        return instance;
    }

    /// <summary>
    /// Lấy instance từ Pool và đặt vị trí, góc quay ban đầu.
    /// </summary>
    public static T Get(T prefab, Vector3 position, Quaternion rotation, Transform parent = null)
    {
        T instance = Get(prefab, parent);
        if (instance != null)
        {
            instance.transform.position = position;
            instance.transform.rotation = rotation;
        }
        return instance;
    }

    /// <summary>
    /// Trả instance về lại Pool.
    /// </summary>
    public static void Return(T instance)
    {
        if (instance == null) return;

        int instanceId = instance.gameObject.GetInstanceID();
        if (_instanceToPrefabMap.TryGetValue(instanceId, out int prefabId))
        {
            if (!_pools.TryGetValue(prefabId, out Queue<T> poolQueue))
            {
                poolQueue = new Queue<T>();
                _pools[prefabId] = poolQueue;
            }

            instance.gameObject.SetActive(false);
            poolQueue.Enqueue(instance);
        }
        else
        {
            // Nếu không tìm thấy thông tin Prefab thì hủy an toàn
            Object.Destroy(instance.gameObject);
        }
    }

    /// <summary>
    /// Khởi tạo trước một lượng object trong Pool để tránh lag lần đầu spawn.
    /// </summary>
    public static void Prewarm(T prefab, int count, Transform parent = null)
    {
        if (prefab == null || count <= 0) return;

        int prefabId = prefab.gameObject.GetInstanceID();
        if (!_pools.TryGetValue(prefabId, out Queue<T> poolQueue))
        {
            poolQueue = new Queue<T>();
            _pools[prefabId] = poolQueue;
        }

        for (int i = 0; i < count; i++)
        {
            T instance = Object.Instantiate(prefab, parent);
            instance.gameObject.SetActive(false);
            _instanceToPrefabMap[instance.gameObject.GetInstanceID()] = prefabId;
            poolQueue.Enqueue(instance);
        }
    }

    /// <summary>
    /// Xóa toàn bộ Pool (thường dùng khi chuyển Scene).
    /// </summary>
    public static void Clear()
    {
        foreach (var kvp in _pools)
        {
            while (kvp.Value.Count > 0)
            {
                T item = kvp.Value.Dequeue();
                if (item != null) Object.Destroy(item.gameObject);
            }
        }
        _pools.Clear();
        _instanceToPrefabMap.Clear();
    }
}

/// <summary>
/// Hệ thống Object Pool cho GameObject thuần túy (Prefabs).
/// Hỗ trợ tái sử dụng GameObject mà không cần chỉ định kiểu Component.
/// </summary>
public static class GameObjectPool
{
    private static readonly Dictionary<int, Queue<GameObject>> _pools = new Dictionary<int, Queue<GameObject>>();
    private static readonly Dictionary<int, int> _instanceToPrefabMap = new Dictionary<int, int>();

    /// <summary>
    /// Lấy một GameObject từ Pool hoặc Instantiate mới nếu Pool đang rỗng.
    /// </summary>
    public static GameObject Get(GameObject prefab, Transform parent = null)
    {
        if (prefab == null) return null;

        int prefabId = prefab.GetInstanceID();

        if (!_pools.TryGetValue(prefabId, out Queue<GameObject> poolQueue))
        {
            poolQueue = new Queue<GameObject>();
            _pools[prefabId] = poolQueue;
        }

        GameObject instance = null;

        while (poolQueue.Count > 0)
        {
            instance = poolQueue.Dequeue();
            if (instance != null)
            {
                break;
            }
        }

        if (instance == null)
        {
            instance = Object.Instantiate(prefab, parent);
        }
        else
        {
            if (parent != null) instance.transform.SetParent(parent);
            instance.SetActive(true);
        }

        _instanceToPrefabMap[instance.GetInstanceID()] = prefabId;
        return instance;
    }

    /// <summary>
    /// Lấy GameObject từ Pool và đặt vị trí, góc quay ban đầu.
    /// </summary>
    public static GameObject Get(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null)
    {
        GameObject instance = Get(prefab, parent);
        if (instance != null)
        {
            instance.transform.position = position;
            instance.transform.rotation = rotation;
        }
        return instance;
    }

    /// <summary>
    /// Trả GameObject về lại Pool.
    /// </summary>
    public static void Return(GameObject instance)
    {
        if (instance == null) return;

        int instanceId = instance.GetInstanceID();
        if (_instanceToPrefabMap.TryGetValue(instanceId, out int prefabId))
        {
            if (!_pools.TryGetValue(prefabId, out Queue<GameObject> poolQueue))
            {
                poolQueue = new Queue<GameObject>();
                _pools[prefabId] = poolQueue;
            }

            instance.SetActive(false);
            poolQueue.Enqueue(instance);
        }
        else
        {
            // Nếu không tìm thấy thông tin Prefab thì hủy an toàn
            Object.Destroy(instance);
        }
    }

    /// <summary>
    /// Khởi tạo trước một lượng GameObject trong Pool để tránh lag lần đầu spawn.
    /// </summary>
    public static void Prewarm(GameObject prefab, int count, Transform parent = null)
    {
        if (prefab == null || count <= 0) return;

        int prefabId = prefab.GetInstanceID();
        if (!_pools.TryGetValue(prefabId, out Queue<GameObject> poolQueue))
        {
            poolQueue = new Queue<GameObject>();
            _pools[prefabId] = poolQueue;
        }

        for (int i = 0; i < count; i++)
        {
            GameObject instance = Object.Instantiate(prefab, parent);
            instance.SetActive(false);
            _instanceToPrefabMap[instance.GetInstanceID()] = prefabId;
            poolQueue.Enqueue(instance);
        }
    }

    /// <summary>
    /// Xóa toàn bộ GameObject Pool (thường dùng khi chuyển Scene).
    /// </summary>
    public static void Clear()
    {
        foreach (var kvp in _pools)
        {
            while (kvp.Value.Count > 0)
            {
                GameObject item = kvp.Value.Dequeue();
                if (item != null) Object.Destroy(item);
            }
        }
        _pools.Clear();
        _instanceToPrefabMap.Clear();
    }
}
