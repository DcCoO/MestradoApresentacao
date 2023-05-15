using UnityEngine;

public abstract class SingletonMonoBehaviour<T> : MonoBehaviour where T : SingletonMonoBehaviour<T>
{
    public static T instance { get; protected set; }

    protected virtual void Awake()
    {
        if (instance != null && instance != this)
            Destroy(instance.gameObject);
        instance = (T)this;
    }

    protected virtual void OnDestroy() => instance = null;
}