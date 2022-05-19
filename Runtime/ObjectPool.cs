using System;
using System.Collections.Generic;

namespace Fab.UITKDropdown
{
    public class PoolExhaustedException : Exception
    {
        private static readonly string msg = "No more pool objects available";
        public PoolExhaustedException()
            : base(msg) { }
    }

    /// <summary>
    /// A generic Object pool
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ObjectPool<T> where T : class
    {
        protected Stack<T> pooled;
        protected List<T> inUse;

        protected int poolSize;

        /// <summary>
        /// The size of the pool
        /// </summary>
        public int PoolSize => poolSize;

        /// <summary>
        /// The number of available objects in the pool
        /// </summary>
        public int Available => pooled.Count;

        protected bool autoExpand;
        
        /// <summary>
        /// If true the pool automatically adds new object when it runs out
        /// </summary>
        public bool AutoExpand => autoExpand;

        protected Func<T> addInstance;

        protected Action<T> resetInstance;

        protected ObjectPool(){}

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="initialPoolSize">The number of objects to pool initialy</param>
        /// <param name="autoExpand">Should the pool automatically add new object when it runs out</param>
        /// <param name="addInstance">Callback called when an instance needs to be added to the pool</param>
        /// <param name="resetInstance">Callback called every time an object is returned to the pool</param>
        public ObjectPool(int initialPoolSize, bool autoExpand, Func<T> addInstance, Action<T> resetInstance = null)
        {
            if (addInstance == null)
                throw new ArgumentNullException("addInstance");

            inUse = new List<T>(initialPoolSize);
            pooled = new Stack<T>(initialPoolSize);
            this.autoExpand = autoExpand;

            this.addInstance = addInstance;
            this.resetInstance = resetInstance;

            for (int i = 0; i < initialPoolSize; i++)
                AddPoolInstance();
        }

        protected void AddPoolInstance()
        {
            pooled.Push(addInstance());
            poolSize++;
        }


        protected virtual void ResetPoolInstance(T obj)
        {
            if (resetInstance != null)
                resetInstance(obj);
        }

        /// <summary>
        /// Ensures there are atleast <paramref name="minSize"/> number of objects in the pool
        /// </summary>
        /// <param name="minSize">The minimum number of objects in the pool</param>
        public void EnsurePoolMinimumSize(int minSize)
        {
            if (poolSize < minSize)
            {
                int diff = minSize - poolSize;
                for (int i = 0; i < diff; i++)
                    AddPoolInstance();
            }
        }

        /// <summary>
        /// Gets a pooled object from the pool
        /// </summary>
        /// <returns></returns>
        public T GetPooled()
        {
            if (pooled.Count == 0 && !autoExpand)
                throw new PoolExhaustedException();
            else if (pooled.Count == 0)
                AddPoolInstance();

            T obj = pooled.Pop();
            inUse.Add(obj);
            return obj;
        }

        /// <summary>
        /// Returns a pooled object to the pool
        /// </summary>
        /// <param name="obj">The object to return</param>
        public void ReturnToPool(T obj)
        {
            if (!inUse.Remove(obj))
                throw new InvalidOperationException("Object cannot be returned because it has not been extracted from the pool");

            ResetPoolInstance(obj);
            pooled.Push(obj);
        }

    }
}
