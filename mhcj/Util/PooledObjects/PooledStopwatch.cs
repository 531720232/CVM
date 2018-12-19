using System.Diagnostics;

namespace Microsoft.CodeAnalysis.PooledObjects
{
    internal class PooledStopwatch : Stopwatch
    {
        private static readonly ObjectPool<PooledStopwatch> s_poolInstance = CreatePool();

        private readonly ObjectPool<PooledStopwatch> _pool;

        private PooledStopwatch(ObjectPool<PooledStopwatch> pool)
        {
            _pool = pool;
        }

        public void Free()
        {
            Reset();
            if (_pool != null)
                _pool.Free(this);
        }

        public static ObjectPool<PooledStopwatch> CreatePool()
        {
            ObjectPool<PooledStopwatch> pool = null;
            pool = new ObjectPool<PooledStopwatch>(() => new PooledStopwatch(pool), 128);
            return pool;
        }

        public static PooledStopwatch StartInstance()
        {
            var instance = s_poolInstance.Allocate();
            instance.Reset();
            instance.Start();
            return instance;
        }
    }
}
