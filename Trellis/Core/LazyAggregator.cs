using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Trellis.Utils;

namespace Trellis.Core
{
    public abstract class LazyAggregator
    {
        internal static readonly IDictionary<Type, MappingConfig> mappingDict = new DefaultDictionary<Type, MappingConfig>(()=> new MappingConfig());
        internal static readonly IDictionary<Type, List<Type>> usings = new DefaultDictionary<Type, List<Type>>(()=>new List<Type>());

        public Id Id { get;private set; }
        private readonly Dictionary<Type, LazyModel> models = new Dictionary<Type, LazyModel>();
        private IAggregatorProvider aggregatorProvider;
        private readonly IDictionary<string, LazyAggregator> aggregators = new Dictionary<string, LazyAggregator>();
        private readonly string[] modelFieldNames;
        private readonly string[] aggregatorFieldNames;
        private readonly MappingConfig mappingConfig;
        private AggregatorContext _context;

        internal LazyAggregator()
        {
            var type = GetType();
            mappingConfig = mappingDict[type];
            var declaredFieldNames = type.GetProperties(
                BindingFlags.Public |
                BindingFlags.Instance |
                BindingFlags.DeclaredOnly)
                .Select(x => x.Name).ToArray();
            aggregatorFieldNames = declaredFieldNames.Where(x =>
                typeof(LazyAggregator).IsAssignableFrom(type.GetProperty(x).PropertyType)).ToArray();
            modelFieldNames = declaredFieldNames.Except(aggregatorFieldNames).ToArray();
            var missingAggregatorConfigs = aggregatorFieldNames.Where(x => !mappingConfig.ContainsAggregatorMappingFor(x));
            if (missingAggregatorConfigs.Count() > 0)
            {
                throw AutomaticMappingException.MissingForeignAggregatorConfig(type, missingAggregatorConfigs.First());
            }
            foreach (var name in modelFieldNames.Where(x=>
                !mappingConfig.ContainsModelMappingFor(x)))
            {
                AutomaticMapField(mappingConfig, name);
            }
        }

        protected LazyAggregator(
            IAggregatorProvider aggregatorProvider,
            params LazyModel[] models) : this()
        {
            this.aggregatorProvider = aggregatorProvider;
            var dict = models.ToDictionary(x => x.GetType(), x => x);
            this.models.Update(dict);
            if (!models.All(x=>x.Id.Equals(models.First().Id)))
            {
                throw new ArgumentException("Incompatible IDs in models");
            }
            Id = models.First().Id;
        }

        private void AutomaticMapField(MappingConfig config, string fieldName)
        {
            var modelsContainingField = usings[GetType()].Where(
                x => x.GetProperties()
                .Select(y => y.Name)
                .Contains(fieldName))
                .ToList();
            if (modelsContainingField.Count() > 1)
            {
                throw AutomaticMappingException.Ambiguous(
                    GetType(),
                    fieldName,
                    modelsContainingField.Select(x =>
                        Tuple.Create(x, fieldName))
                        .ToList());
            }
            if (modelsContainingField.Count() == 0)
            {
                throw AutomaticMappingException.TargetNotFound(GetType(), fieldName);
            }

            var targetModelType = modelsContainingField.First();
            var originPropertyType = GetType().GetProperty(fieldName).PropertyType;
            var targetPropertyType = targetModelType.GetProperty(fieldName).PropertyType;
            if (!originPropertyType.IsAssignableFrom(targetPropertyType))
            {
                throw AutomaticMappingException.IncompatibleTypes(
                    GetType(), fieldName, targetModelType, fieldName);
            }

            config.FieldOneToOne(fieldName, targetModelType, fieldName);
        }

        internal T Model<T>() where T :LazyModel
        {
            return (T)Model(typeof(T));
        }

        internal LazyModel Model(Type type)
        {
            return models[type];
        }

        private AggregatorContext GetContext()
        {
            return _context ?? (_context = new AggregatorContext(this));
        }

        protected T PropertyGetter<T>(string name)
        {
            var type = typeof(T);
            if (typeof(LazyAggregator).IsAssignableFrom(type))
            {
                if (!aggregators.ContainsKey(name))
                {
                    var id = GetForeignAggregatorId(name);
                    aggregators[name] = aggregatorProvider.Get(type, id);
                }
                return (T)(object)aggregators[name];
            }
            var c = GetContext();
            return (T)mappingConfig.GetConfigs[name](c);
        }

        private Id GetForeignAggregatorId(string aggregatorFieldName)
        {
            var modelAndField = mappingConfig.ForeignAggregatorIds[aggregatorFieldName];
            var modelType = modelAndField.Item1;
            var modelFieldName = modelAndField.Item2;
            return (Id) modelType.GetProperty(modelFieldName).GetValue(Model(modelType));
        }

        protected void PropertySetter<T>(string name, T value)
        {
            var type = typeof(T);
            var agg = value as LazyAggregator;
            if (agg != null)
            {
                aggregators[name] = agg;
                var modelAndField = mappingConfig.ForeignAggregatorIds[name];
                var modelType = modelAndField.Item1;
                var modelFieldName = modelAndField.Item2;
                modelType.GetProperty(modelFieldName).SetValue(Model(modelType), agg.Id);
                return;
            }

            foreach (var modelType in mappingConfig.SetConfigs[name].Keys)
            {
                foreach (var fieldName in mappingConfig.SetConfigs[name][modelType].Keys)
                {
                    var valueFunc = mappingConfig.SetConfigs[name][modelType][fieldName];
                    modelType.GetProperty(fieldName).SetMethod.Invoke(
                        Model(modelType), 
                        new object[] { valueFunc(value) });
                }
            }
        }

        public void Commit()
        {
            foreach (var model in models.Values)
            {
                model.Commit();
            }
            foreach (var aggregator in aggregators.Values)
            {
                aggregator.Commit();
            }
        }

        public void PreloadAgg(params string[] fields)
        {
            if (fields.Length == 0)
                fields = modelFieldNames.Concat(aggregatorFieldNames).ToArray();

            var fromModels = fields.Where(x => modelFieldNames.Contains(x));
            var fromAggregators = fields.Where(x => aggregatorFieldNames.Contains(x));

            var modelsAndFieldsToPreload = new DefaultDictionary<Type, List<string>>(()=>new List<string>());
            foreach (var field in fromModels)
            {
                foreach (var modelAndFields in mappingConfig.Usings[field])
                {
                    modelsAndFieldsToPreload[modelAndFields.Key].AddRange(modelAndFields.Value);
                }
            }

            // add aggregator IDs to the preload
            var aggregatorIdLocationsToPreload = fromAggregators.Select(x => mappingConfig.ForeignAggregatorIds[x]);
            foreach (var modelType in modelsAndFieldsToPreload.Keys)
            {
                modelsAndFieldsToPreload[modelType].AddRange(aggregatorIdLocationsToPreload.Where(x => x.Item1.Equals(modelType)).Select(x => x.Item2));
            }

            foreach (var modelAndFields in modelsAndFieldsToPreload)
            {
                Model(modelAndFields.Key).Preload(modelAndFields.Value.ToArray());
            }

            foreach (var field in fromAggregators)
            {
                // invoke getter so aggregator gets fetched
                GetType().GetProperty(field).GetValue(this);

                aggregators[field].PreloadAgg();
            }
        }


        public static MappingConfig<T> Setup<T>() where T : LazyAggregator
        {
            return new MappingConfig<T>(mappingDict[typeof(T)]);
        }

        public static void UsingModel<TAgg, TModel>() 
            where TAgg : LazyAggregator 
            where TModel : LazyModel
        {
            usings[typeof(TAgg)].Add(typeof(TModel));
        }

        public static LazyAggregator New(Type type, AggregatorProvider provider, LazyModel[] models)
        {
            var ctor = type.GetConstructor(new[] { typeof(AggregatorProvider), typeof(LazyModel[]) });
            return (LazyAggregator)ctor.Invoke(new object[] { provider, models });
        }

        public static T New<T>(AggregatorProvider provider, LazyModel[] models) where T : LazyAggregator
        {
            return (T)New(typeof(T), provider, models);
        }
    }

    public static class LazyAggregatorExtensions
    {
        public static void PreloadAgg<T>(this T obj, params Expression<Func<T, object>>[] fields)
            where T : LazyAggregator
        {
            var fieldNames = fields.Select(x => typeof(T).GetPropertyInfo(x).Name).ToArray();
            obj.PreloadAgg(fieldNames);
        }
    }
}
