using System;
using System.Runtime.CompilerServices;

namespace Certera.Core.Concurrency
{
    public static class NamedLocker
    {
        private static readonly ConditionalWeakTable<string, object> _table = new ConditionalWeakTable<string, object>();

        public static object GetLock(string key) => _table.GetOrCreateValue(key);

        public static T RunWithLock<T>(string key, Func<T> func)
        {
            lock (GetLock(key))
            {
                return func();
            }
        }

        public static void RunWithLock(string key, Action a)
        {
            lock(GetLock(key))
            {
                a();
            }
        }
    }
}
