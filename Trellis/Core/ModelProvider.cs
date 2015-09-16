using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Trellis.Core
{
    public class ModelProvider
    {
        IDB db;
        internal IDictionary<Type, string> collectionNameDict;
        public ModelProvider(IDB db, IDictionary<Type, string> collectionNameDict)
        {
            this.db = db;
            this.collectionNameDict = collectionNameDict ?? new Dictionary<Type, string>();
        }

        private IDBCollection GetCollection(Type type)
        {
            if (!collectionNameDict.ContainsKey(type))
                collectionNameDict[type] = CreateDefaultCollectionName(type);
            return db.GetCollection(collectionNameDict[type]);
        }

        private static string CreateDefaultCollectionName(Type type)
        {
            string name = type.Name;
            return name.EndsWith("s") ? name + "es" : name + "s";
        }

        public LazyModel Get(Type type)
        {
            var collection = GetCollection(type);
            Id id = collection.GetNewId();
            return Get(type, id);
        }

        public LazyModel Get(Type type, Id id)
        {
            var collection = GetCollection(type);
            return LazyModel.New(type, id, collection);
        }

        public IEnumerable<LazyModel> Get(Type type, int count)
        {
            return Enumerable.Repeat(Get(type), count);
        }

        public T GetOneByQuery<T>(Expression<Func<T, bool>> query, params string[] fieldNames) where T :LazyModel
        {
            var collection = GetCollection(typeof(T));
            var result = collection.GetOnesFieldsByQuery(query, fieldNames);
            var model = LazyModel.New<T>(result.Item1, collection);
            model.InitializeFields(result.Item2);
            return model;
        }

        public IEnumerable<T> GetByQuery<T>(Expression<Func<T, bool>> query, params string[] fieldNames) where T :LazyModel
        {
            var collection = GetCollection(typeof(T));
            var result = collection.GetFieldsByQuery(query, fieldNames);
            var models = result.Select(x => LazyModel.New<T>(x.Key, collection));
            foreach (var model in models)
            {
                model.InitializeFields(result[model.Id]);
            }
            return models;
        }

        public long Count<T>(Expression<Func<T, bool>> query) where T :LazyModel
        {
            var collection = GetCollection(typeof(T));
            return collection.Count(query);
        }

        public bool Exists<T>(Expression<Func<T, bool>> query) where T :LazyModel
        {
            var collection = GetCollection(typeof(T));
            return collection.Exists(query);
        }

        public void Delete(Type type, Id id)
        {
            var collection = GetCollection(type);
            collection.Delete(id);
        }

        public void Delete<T>(Expression<Func<T, bool>> query) where T :LazyModel
        {
            var collection = GetCollection(typeof(T));
            collection.Delete(query);
        }
    }
    public class ModelProvider<T> where T : LazyModel
    {
        ModelProvider provider;
        public ModelProvider(ModelProvider provider)
        {
            this.provider = provider;
        }

        public T Get()
        {
            return (T)provider.Get(typeof(T));
        }

        public T Get(Id id)
        {
            return (T)provider.Get(typeof(T), id);
        }
    }
}
