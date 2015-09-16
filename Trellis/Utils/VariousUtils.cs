using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Trellis.Utils
{
    public static class VariousUtils
    {
        private static Random rnd = new Random();
        public static T Clamp<T>(this T val, T min, T max) where T : IComparable
        {
            if (val.CompareTo(min) < 0)
                return min;
            if (val.CompareTo(max) > 0)
                return max;
            return val;
        }

        public static bool IsNullOrEmpty<T>(this ICollection<T> collection)
        {
            return collection == null || !collection.Any();
        }

        public static bool IsBetween<T>(this T val, T min, T max, bool inclusive=true) where T : IComparable
        {
            return inclusive
                ? val.CompareTo(min) >= 0 && val.CompareTo(max) <= 0
                : val.CompareTo(min) > 0 && val.CompareTo(max) < 0;
        }

        public static bool ImplementsInterface<IT>(this Type type)
        {
            return type.ImplementsInterface(typeof (IT));
        }

        public static bool ImplementsInterface(this Type type, Type itype)
        {
            return type.GetInterfaces().Contains(itype);
        }

        public static bool SubclassesGenericType(this Type type, Type generic)
        {
            while (type != null && type != typeof(object))
            {
                var cur = type.IsGenericType ? type.GetGenericTypeDefinition() : type;
                if (generic == cur)
                {
                    return true;
                }
                type = type.BaseType;
            }
            return false;
        }

        public static bool ImplementsGenericInterface(this Type type, Type igenType)
        {
            return type.GetInterfaces().Any(x =>
                x.IsGenericType &&
                x.GetGenericTypeDefinition() == igenType);
        }

        public static TProp GetProperty<T, TProp>(this T obj, Expression<Func<T, TProp>> property)
        {
            return (TProp)obj.GetPropertyInfo(property).GetValue(obj);   
        }
        public static void SetProperty<T, TProp> (this T obj, Expression<Func<T, TProp>> property, TProp value)
        {
            obj.GetPropertyInfo(property).SetValue(obj, value);
        }

        public static PropertyInfo GetPropertyInfo<T, TProp>(
            this T source, 
            Expression<Func<T, TProp>> property)
        {
            return GetPropertyInfo(typeof(T), property);
        }

        public static PropertyInfo GetPropertyInfo<T, TProp>(
            this Type type,
            Expression<Func<T, TProp>> property)
        {
            var member = property.Body as MemberExpression;
            if (member == null)
                throw new ArgumentException(string.Format(
                    "Expression '{0}' does not refer to a member of class.",
                    property.ToString()));
            return GetPropertyInfo(type, member);
        }

        private static PropertyInfo GetPropertyInfo(
            this Type type,
            MemberExpression member)
        {
            var propInfo = member.Member as PropertyInfo;
            if (propInfo == null)
                throw new ArgumentException(string.Format(
                    "Member expression '{0}' refers to a field, not a property.",
                    member.ToString()));

            if (type != propInfo.ReflectedType &&
                !type.IsSubclassOf(propInfo.ReflectedType))
                throw new ArgumentException(string.Format(
                    "Member expression '{0}' refers to a property that is not from type {1}.",
                    member.ToString(),
                    type));

            return propInfo;
        }

        public static PropertyInfo GetPropertyInfo<T>(
            this Type type,
            Expression<Func<T, object>> property)
        {
            try
            {
                return GetPropertyInfo<T, object>(type, property);
            }
            catch (ArgumentException)
            {
                var converted = property.Body as UnaryExpression;
                if (converted == null || converted.NodeType != ExpressionType.Convert)
                    throw new ArgumentException(string.Format(
                        "Expression '{0}' does not refer to a member of class.",
                        property.ToString()));
                var member = converted.Operand as MemberExpression;
                return GetPropertyInfo(type, member);
            }
        }

        public static void AddRange<T>(this ICollection<T> collection, IEnumerable<T> items)
        {
            foreach (var item in items)
                collection.Add(item);
        }

        public static bool IsCastableTo(this Type from, Type to)
        {
            if (to.IsAssignableFrom(from))
            {
                return true;
            }
            var methods = from.GetMethods(BindingFlags.Public | BindingFlags.Static)
                              .Where(
                                  m => m.ReturnType == to &&
                                       (m.Name == "op_Implicit" ||
                                        m.Name == "op_Explicit")
                              );
            return methods.Count() > 0;
        }

        public static bool IsAnyExceptionInHierarchyOfType(this Exception ex, Type type)
        {
            if (type.IsAssignableFrom(ex.GetType()))
                return true;
            var currentEx = ex.InnerException;
            while (currentEx!=null)
            {
                if (type.IsAssignableFrom(currentEx.GetType()))
                    return true;
                currentEx = currentEx.InnerException;
            }
            return false;
        }

        public static void Shuffle<T>(this IList<T> list)
        {
            var n = list.Count;
            while (n > 1)
            {
                n--;
                var k = rnd.Next(n + 1);
                var value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }

        public static bool CanConvertTo(this Type fromType, Type toType)
        {
            try
            {
                // Throws an exception if there is no conversion from fromType to toType
                Expression.Convert(Expression.Parameter(fromType, null), toType);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static int FindIndex<T>(this IEnumerable<T> items, Func<T, bool> predicate)
        {
            if (items == null) throw new ArgumentNullException("items");
            if (predicate == null) throw new ArgumentNullException("predicate");

            int retVal = 0;
            foreach (var item in items)
            {
                if (predicate(item)) return retVal;
                retVal++;
            }
            return -1;
        }

        public static void Update<TKey, TVal>(
            this IDictionary<TKey, TVal> orig,
            IDictionary<TKey, TVal> patch)
        {
            foreach(var kvp in patch)
            {
                orig[kvp.Key] = kvp.Value;
            }
        }

        public static Tuple<T1, T2> ToTuple<T1, T2>(this KeyValuePair<T1,T2> kvp)
        {
            return Tuple.Create(kvp.Key, kvp.Value);
        }

        public static KeyValuePair<T1, T2> ToKVP<T1, T2>(this Tuple<T1, T2> tuple)
        {
            return new KeyValuePair<T1, T2>(tuple.Item1, tuple.Item2);
        }
    }
}
