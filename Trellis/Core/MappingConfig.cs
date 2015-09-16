using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Trellis.Utils;

namespace Trellis.Core
{
    public class MappingConfig
    {
        /// <summary>
        /// { aggregator field name : mapping function using model fields }
        /// </summary>
        internal Dictionary<string, Func<AggregatorContext, object>> GetConfigs = new Dictionary<string, Func<AggregatorContext, object>>();
        /// <summary>
        /// { aggregator field name :
        ///     { model type : list of names of model fields used by aggregator field }
        /// }
        /// </summary>
        internal Dictionary<string, Dictionary<Type, List<string>>> Usings = new Dictionary<string, Dictionary<Type, List<string>>>();
        /// <summary>
        /// { aggregator field name :
        ///     { model type :
        ///         { model field name : mapping action from aggregator field to model field }
        ///     }
        /// }
        /// </summary>
        internal Dictionary<string, Dictionary<Type, Dictionary<string, Func<object, object>>>> SetConfigs = new Dictionary<string, Dictionary<Type, Dictionary<string, Func<object, object>>>>();
        /// <summary>
        /// { aggregator field name : (modelType, aggregatorIdFieldName) }
        /// </summary>
        internal Dictionary<string, Tuple<Type, string>> ForeignAggregatorIds = new Dictionary<string, Tuple<Type, string>>();

        public MappingConfig MapToField(
            string fieldName, 
            Func<AggregatorContext, object> valueFunc)
        {
            if (GetConfigs == null)
                GetConfigs = new Dictionary<string, Func<AggregatorContext, object>>();
            GetConfigs[fieldName] = valueFunc;
            return this;
        }

        public MappingConfig MapFromField(
            string fieldName, 
            Type targetType, 
            string targetFieldName, 
            Func<object, object> valueFunc)
        {
            if (SetConfigs == null)
                SetConfigs = new Dictionary<string, Dictionary<Type, Dictionary<string, Func<object, object>>>>();
            if (!SetConfigs.ContainsKey(fieldName))
                SetConfigs[fieldName] = new Dictionary<Type, Dictionary<string, Func<object, object>>>();
            if (!SetConfigs[fieldName].ContainsKey(targetType))
                SetConfigs[fieldName][targetType] = new Dictionary<string, Func<object, object>>();

            SetConfigs[fieldName][targetType][targetFieldName] = valueFunc;
            return this;
        }

        public MappingConfig FieldUsing(
            string fieldName,
            Type modelType,
            params string[] fieldNames)
        {
            if (Usings == null)
                Usings = new Dictionary<string, Dictionary<Type, List<string>>>();
            if (!Usings.ContainsKey(fieldName))
                Usings[fieldName] = new Dictionary<Type, List<string>>();
            if (!Usings[fieldName].ContainsKey(modelType))
                Usings[fieldName][modelType] = new List<string>();


            Usings[fieldName][modelType].AddRange(fieldNames);
            return this;
        }

        public MappingConfig FieldOneToOne(
            string fieldName,
            Type targetModelType,
            string targetFieldName
            )
        {
            MapFromField(fieldName, targetModelType, targetFieldName, x => x);
            MapToField(fieldName, x => targetModelType.GetProperty(targetFieldName).GetMethod.Invoke(x.M(targetModelType), new object[] { }));
            FieldUsing(fieldName, targetModelType, targetFieldName);
            return this;
        }

        public MappingConfig ForeignAggregator(string aggregatorFieldName, Type modelType, string modelFieldName)
        {
            if (ForeignAggregatorIds == null)
                ForeignAggregatorIds = new Dictionary<string, Tuple<Type, string>>();
            ForeignAggregatorIds[aggregatorFieldName] = Tuple.Create(modelType, modelFieldName);
            return this;
        }

        public FieldMappingConfig<TAgg, TField> Field<TAgg, TField>(Expression<Func<TAgg, TField>> fieldExp) where TAgg :LazyAggregator
        {
            return new FieldMappingConfig<TAgg, TField>(new MappingConfig<TAgg>(this), typeof(TAgg).GetPropertyInfo(fieldExp).Name);
        }

        public bool ContainsAggregatorMappingFor(string fieldName)
        {
            return ForeignAggregatorIds.ContainsKey(fieldName);
        }

        public bool ContainsModelMappingFor(string fieldName)
        {
            return SetConfigs.ContainsKey(fieldName) &&
                   GetConfigs.ContainsKey(fieldName) &&
                   Usings.ContainsKey(fieldName);
        }
    }

    public class MappingConfig<TAgg> where TAgg :LazyAggregator
    {
        MappingConfig config;
        public MappingConfig(MappingConfig config)
        {
            this.config = config;
        }

        public MappingConfig<TAgg> MapToField(
            string fieldName,
            Func<AggregatorContext, object> valueFunc)
        {
            config.MapToField(fieldName, valueFunc);
            return this;
        }

        public MappingConfig<TAgg> MapFromField(
            string fieldName,
            Type targetType,
            string targetFieldName,
            Func<object, object> valueFunc)
        {
            config.MapFromField(fieldName, targetType, targetFieldName, valueFunc);
            return this;
        }

        public MappingConfig<TAgg> FieldUsing(
            string fieldName,
            Type modelType,
            params string[] modelFieldNames)
        {
            config.FieldUsing(fieldName, modelType, modelFieldNames);
            return this;
        }

        public MappingConfig<TAgg> FieldOneToOne(
            string fieldName,
            Type modelType,
            string targetFieldName)
        {
            config.FieldOneToOne(fieldName, modelType, targetFieldName);
            return this;
        }

        public MappingConfig<TAgg> ForeignAggregator(
            string aggregatorFieldName, 
            Type modelType, 
            string modelFieldName)
        {
            config.ForeignAggregator(aggregatorFieldName, modelType, modelFieldName);
            return this;
        }

        public ForeignAggregatorConfig<TAgg> ForeignAggregator<TFAgg>(Expression<Func<TAgg, TFAgg>> fieldExp) where TFAgg : LazyAggregator
        {
            return new ForeignAggregatorConfig<TAgg>(this, typeof(TAgg).GetPropertyInfo(fieldExp).Name);
        }

        public FieldMappingConfig<TAgg, TProp> Field<TProp>(Expression<Func<TAgg, TProp>> fieldExp)
        {
            return new FieldMappingConfig<TAgg, TProp>(this, typeof(TAgg).GetPropertyInfo(fieldExp).Name);
        }
    }
}
