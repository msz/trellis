using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace Trellis.Core
{
    public interface IDBCollection
    {
        T GetModelField<T>(Id id, string fieldName);
        object GetModelField(Id id, string fieldName);
        IDictionary<string, object> GetFields(Id id, params string[] fieldNames);
        void UpdateFields(Id id, IDictionary<string, object> fieldNamesAndValues);
        Id GetNewId();
        Dictionary<Id, Dictionary<string, object>> GetFieldsByQuery<T>(Expression<Func<T, bool>> query, params string[] fieldNames) where T : LazyModel;
        Tuple<Id, Dictionary<string, object>> GetOnesFieldsByQuery<T>(Expression<Func<T, bool>> query, params string[] fieldNames) where T : LazyModel;
        object ArrayElem(Id id, string fieldName, int index);
        long Count<T>(Expression<Func<T,bool>> query) where T : LazyModel;
        bool Exists<T>(Expression<Func<T, bool>> query) where T : LazyModel;
        void Delete (Id id);
        void Delete<T>(Expression<Func<T, bool>> query) where T :LazyModel;
        int ArraySize(Id id, string fieldName);
        bool ArrayContains<T>(Id id, string fieldName, T item);
        void ArrayAppend(Id id, string fieldName, object[] items);
        object[] ArrayGet(Id id, string fieldName, int[] indexes);
    }
}
