using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Trellis.Utils;

namespace Trellis.Core
{
    public abstract class LazyModel
    {
        public Id Id {get; private set;}
        public bool AreSetEventsEnabled { get;set; }

        IDBCollection collection;
        readonly IDictionary<string, BackingField> backingFields = new Dictionary<string, BackingField>();
        readonly IList<FieldSetEvent> pendingSetEvents = new List<FieldSetEvent>();
        readonly string[] modelFieldNames;

        protected LazyModel(Id id, IDBCollection _collection)
        {
            modelFieldNames = GetType().GetProperties(
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.DeclaredOnly)
                .Select(x => x.Name).ToArray();
            AreSetEventsEnabled = true;
            Initialize(id, _collection);
        }

        private void Initialize(Id id, IDBCollection collection)
        {
            Id = id;
            this.collection = collection;
            foreach (var name in modelFieldNames)
            {
                backingFields.Add(name, new BackingField());
            }
        }
        internal static LazyModel New(Type type, Id id, IDBCollection collection)
        {
            var argTypes = new Type[] { typeof(Id), typeof(IDBCollection) };
            var argValues = new object[] { id, collection };
            var ctor = type.GetConstructor(argTypes);
            return (LazyModel)ctor.Invoke(argValues);
        }
        internal static T New<T>(Id id, IDBCollection collection) where T : LazyModel
        {
            return (T)New(typeof(T), id, collection);
        }

        protected dynamic PropertyGetter(string name)
        {
            var backingField = backingFields[name];
            if(!backingField.IsLoaded)
            {
                backingField.Value = collection.GetModelField(Id, name);
                backingField.IsLoaded = true;
            }
            return backingField.Value;
        }

        protected void PropertySetter<T>(string name, T value)
        {
            if (AreSetEventsEnabled == true)
            {
                pendingSetEvents.Add(new FieldSetEvent { PropertyName = name, Value = value });
            }
            backingFields[name].IsLoaded = true;
            backingFields[name].Value = value;
        }

        public void Commit()
        {
            var fieldsToUpdate = new Dictionary<string, object>();
            var events = pendingSetEvents.Reverse();
            foreach(var setEvent in pendingSetEvents
                .Where(x => !fieldsToUpdate.ContainsKey(x.PropertyName)))
            {
                fieldsToUpdate[setEvent.PropertyName] = setEvent.Value;
            }
            collection.UpdateFields(Id, fieldsToUpdate);
            pendingSetEvents.Clear();
        }

        public void Preload(params string[] fieldNames)
        {
            if (fieldNames.Length == 0)
                fieldNames = modelFieldNames;
            var values = collection.GetFields(Id, fieldNames);
            foreach(var fieldName in fieldNames)
            {
                backingFields[fieldName].Value = values[fieldName];
                backingFields[fieldName].IsLoaded = true;
            }
        }

        internal void InitializeFields(IDictionary<string, object> fieldsAndValues)
        {
            var oldval = AreSetEventsEnabled;
            AreSetEventsEnabled = false;
            foreach (var kvp in fieldsAndValues)
            {
                backingFields[kvp.Key].Value = kvp.Value;
                backingFields[kvp.Key].IsLoaded = true;
            }
            AreSetEventsEnabled = oldval;
        }

    }

    public static class LazyModelExtensions
    {
        public static void Preload<T>(this T obj, params Expression<Func<T, object>>[] fields)
            where T : LazyModel
        {
            var fieldNames = fields.Select(x => typeof(T).GetPropertyInfo(x).Name).ToArray();
            obj.Preload(fieldNames);
        }
    }
}
