
using System;
using Microsoft.CodeAnalysis.PooledObjects;
using CVM;
namespace Microsoft.Cci
{
    internal sealed class PooledBlobBuilder : BinBuilder, IDisposable
    {
        private const int PoolSize = 128;
        private const int ChunkSize = 1024;

        private static ObjectPool<PooledBlobBuilder> s_chunkPool = new ObjectPool<PooledBlobBuilder>(() => new PooledBlobBuilder(ChunkSize), PoolSize);

        private PooledBlobBuilder(int size)
            : base()
        {
        }

        public static PooledBlobBuilder GetInstance(int size = ChunkSize)
        {
            // TODO: use size
            return s_chunkPool.Allocate();
        }

        public BinBuilder AllocateChunk(int minimalSize)
        {
            if (minimalSize <= ChunkSize)
            {
                return s_chunkPool.Allocate();
            }

            return new BinBuilder();
        }

        public  void FreeChunk()
        {
            s_chunkPool.Free(this);
        }
        public new void Free()
        {
            base.Dispose();
        }
        //public new void Free()
        //{
        //    base.Free();
        //}

        void IDisposable.Dispose()
        {
            Free();
        }
    }
}
