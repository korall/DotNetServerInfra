
using System.Threading;

namespace Utility
{
    public abstract class Singleton<T>
        where T : Singleton<T>, new()
    {
        static T _Instance;
        public static T Instance
        {
            get
            {
                if (_Instance == null)
                {
                    var inst = new T();
                    inst = Interlocked.CompareExchange(ref _Instance, inst, null);
                    if (inst == null)
                        _Instance._InitOnCreateInstance();
                }
                return _Instance;
            }
        }

        protected virtual void _InitOnCreateInstance() { }
    }
}