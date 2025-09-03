using UnityEngine;

// template : 틀, 형  사용법 <T>
// 싱글톤 : 관리자 , 전역 , 하나(유일)
// BS : 런타임(실행중) 존재 , Editor : 존재? -> SS
public abstract class Ch_BehaviourSingleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T _instance;

    public static T I
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<T>();

                if (_instance == null)
                {
                    GameObject o = new GameObject(typeof(T).Name);
                    _instance = o.AddComponent<T>();
                }
            }

            return _instance;
        }
    }

    protected abstract bool IsDontdestroy();

    protected virtual void Awake()
    {
        if (I != null && I != this)
        {
            Destroy(gameObject);
            return;
        }

        if (IsDontdestroy())
            DontDestroyOnLoad(gameObject);
    }

}
