using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace IodemBot.Core
{
    public interface IPersistentStorage<T>
    {
        public IEnumerable<T> RestoreAll();

        public T RestoreSingle(Expression<Func<T, bool>> predicate);
        public bool Exists(Expression<Func<T, bool>> predicate);
        public void Store(T item);
        public void Update(T item);
        public void Remove(Expression<Func<T, bool>> predicate);

        public T RestoreSingle(ulong id);
        public bool Exists(ulong id);
        public void Remove(ulong id);


    }
}
