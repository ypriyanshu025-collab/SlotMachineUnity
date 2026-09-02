using UnityEngine;

namespace SlotMachine.Utils
{
    /// <summary>
    /// Generic base class for a MonoBehaviour that should only ever have one
    /// active instance in the scene (e.g. AudioManager). Kept intentionally
    /// small: no DontDestroyOnLoad / cross-scene persistence is needed for
    /// this project since it is a single-scene game.
    /// </summary>
    /// <typeparam name="T">The concrete singleton type.</typeparam>
    public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T _instance;

        public static T Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<T>();
                }
                return _instance;
            }
        }

        protected virtual void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this as T;
        }
    }
}
