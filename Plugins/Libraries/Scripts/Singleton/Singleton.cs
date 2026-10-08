// Created By   :   Isaac Bustad
// Created      :   7/4/2026



using UnityEngine;

// abstract class that we cannot instantiate
// pass in type we want to make single
public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T instance;
    private static bool applicationIsQuitting = false;

    [SerializeField] private bool persistent = default;

    public static T Instance
    {
        get
        {
            if (applicationIsQuitting)
            {
                // Debug.LogWarning($"[Singleton] Instance '{typeof(T)}' already destroyed on application quit. Missing reference exception avoided.");
                return null;
            }

            if (instance == null)
            {
                // Look for existing instance in the scene
                instance = (T)FindFirstObjectByType(typeof(T));

                // Create a new one if it doesn't exist
                if (instance == null)
                {
                    GameObject singletonObject = new GameObject();
                    instance = singletonObject.AddComponent<T>();
                    singletonObject.name = $"{typeof(T)} (Singleton)";
                }
            }

            return instance;
        }
    }

    protected virtual void Awake()
    {
        if (instance == null)
        {
            instance = this as T;
            
            if (persistent)
            {
                DontDestroyOnLoad(gameObject);
            }
        }
        else if (instance != this)
        {
            // Debug.LogWarning($"[Singleton] Duplicate instance of {typeof(T)} detected on {gameObject.name}. Destroying duplicate.");
            Destroy(gameObject);
        }
    }

    protected virtual void OnApplicationQuit()
    {
        applicationIsQuitting = true;
    }

    protected virtual void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }
}
