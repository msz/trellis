using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace Trellis.Core
{
    [Serializable]
    internal class AutomaticMappingException : Exception
    {
        public AutomaticMappingException()
        {
        }

        public AutomaticMappingException(string message) : base(message)
        {
        }

        public AutomaticMappingException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected AutomaticMappingException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        private static string CreateStringFieldInfo(Type type, string fieldName)
        {
            return string.Format("{0}.{1}", type.Name, fieldName);
        }

        public static AutomaticMappingException Ambiguous(
            Type typeToMap, 
            string fieldToMap, 
            IEnumerable<Tuple<Type, string>> ambiguousTargets)
        {
            var origin = CreateStringFieldInfo(typeToMap, fieldToMap);
            var targets = ambiguousTargets.Select(x =>
                CreateStringFieldInfo(x.Item1, x.Item2));
            var msg = new StringBuilder("Mapping from field " + origin + "has the following ambiguous targets:");
            foreach (var target in targets)
            {
                msg.Append(" " + target);
            }
            return new AutomaticMappingException(msg.ToString());
        }

        public static AutomaticMappingException TargetNotFound(
            Type typeToMap,
            string fieldToMap)
        {
            return new AutomaticMappingException(
                "Cannot find suitable target for mapping " + CreateStringFieldInfo(typeToMap, fieldToMap));
        }

        public static AutomaticMappingException IncompatibleTypes(
            Type typeToMap,
            string fieldToMap,
            Type targetType,
            string targetField)
        {
            var origin = CreateStringFieldInfo(typeToMap, fieldToMap);
            var target = CreateStringFieldInfo(targetType, targetField);
            return new AutomaticMappingException(
                "Cannot map from " + origin + " to " + target + " due to type incompatibility");
        }

        public static AutomaticMappingException MissingForeignAggregatorConfig(
            Type typeToMap,
            string fieldToMap)
        {
            var origin = CreateStringFieldInfo(typeToMap, fieldToMap);
            return new AutomaticMappingException(
                "Field " + origin + "is another aggregator and there is no aggregator mapping configured for it.");
        }
    }
}