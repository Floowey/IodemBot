using LiteDB;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;

namespace IodemBot.Core
{
    public class PersistentStorage
    {
        private readonly string _dbFileName;

        public PersistentStorage()
        {
            _dbFileName = Path.Combine("Resources", "Accounts", "Accounts.db");
        }

        public IEnumerable<T> RestoreMany<T>(Expression<Func<T, bool>> predicate)
        {
            using var db = new LiteDatabase(_dbFileName);
            var collection = db.GetCollection<T>();
            return collection.Find(predicate).ToArray();
        }

        public IEnumerable<T> RestoreAll<T>()
        {
            using var db = new LiteDatabase(_dbFileName);
            var collection = db.GetCollection<T>();
            Console.WriteLine(collection.Count());
            Console.WriteLine(collection.FindAll().Count());
            return collection.FindAll().ToArray();
        }

        public T RestoreSingle<T>(Expression<Func<T, bool>> predicate)
        {
            using var db = new LiteDatabase(_dbFileName);
            var collection = db.GetCollection<T>();
            return collection.FindOne(predicate);
        }

        public bool Exists<T>(Expression<Func<T, bool>> predicate)
        {
            using var db = new LiteDatabase(_dbFileName);
            var collection = db.GetCollection<T>();
            return collection.Exists(predicate);
        }

        public void Store<T>(T item)
        {
            using var db = new LiteDatabase(_dbFileName);
            var collection = db.GetCollection<T>();
            _ = collection.Insert(item);
        }

        public void Update<T>(T item)
        {
            using var db = new LiteDatabase(_dbFileName);
            var collection = db.GetCollection<T>();
            _ = collection.Update(item);
        }

        public void Remove<T>(Expression<Func<T, bool>> predicate)
        {
            using var db = new LiteDatabase(_dbFileName);
            var collection = db.GetCollection<T>();
            _ = collection.DeleteMany(predicate);
        }
    }
}