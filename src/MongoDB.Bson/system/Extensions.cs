using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace MongoDB.Bson
{
    public static class TypeExtensions
    {
        public static IEnumerable<Attribute> GetCustomAttributes(this Type type, bool inherit)
        {
            return type.GetTypeInfo().GetCustomAttributes(inherit);
        }

        public static IEnumerable<Attribute> GetCustomAttributes(this Type type)
        {
            return type.GetTypeInfo().GetCustomAttributes();
        }

        public static TypeCode GetTypeCode(this Type type)
        {
            if (type == null) return TypeCode.Empty;
            TypeCode result;
            if (typeCodeLookup.TryGetValue(type, out result)) return result;

            if (type.GetTypeInfo().IsEnum)
            {
                type = Enum.GetUnderlyingType(type);
                if (typeCodeLookup.TryGetValue(type, out result)) return result;
            }
            return TypeCode.Object;
        }


        public static List<T> GetAttributesWithImplementedInterface<T>(this MethodBase member) where T : class
        {
            return member.GetCustomAttributes(false)
                                    .Where(a => a.GetType().GetInterfaces().Any(x => x.Name.Equals(typeof(T).Name)))
                                    .Select(a => a as T)
                                    .ToList();
        }

        public static List<T> GetAttributesWithImplementedInterface<T>(this MemberInfo member) where T : class
        {
            return member.GetCustomAttributes(false)
                                    .Where(a => a.GetType().GetInterfaces().Any(x => x.Name.Equals(typeof (T).Name)))
                                    .Select(a => a as T)
                                    .ToList();
        }

        public static List<T> GetAttributesWithImplementedInterface<T>(this ConstructorInfo member) where T : class
        {
            return member.GetCustomAttributes(false)
                                    .Where(a => a.GetType().GetInterfaces().Any(x => x.Name.Equals(typeof(T).Name)))
                                    .Select(a => a as T)
                                    .ToList();
        }

        public static List<T> GetAttributesWithImplementedInterface<T>(this Type member) where T : class
        {
            return member.GetCustomAttributes(false)
                                    .Where(a => a.GetType().GetInterfaces().Any(x => x.Name.Equals(typeof(T).Name)))
                                    .Select(a => a as T)
                                    .ToList();
        }

        static readonly Dictionary<Type, TypeCode> typeCodeLookup = new Dictionary<Type, TypeCode>
        {
            {typeof (bool), TypeCode.Boolean},
            {typeof (byte), TypeCode.Byte},
            {typeof (char), TypeCode.Char},
            {typeof (DateTime), TypeCode.DateTime},
            {typeof (decimal), TypeCode.Decimal},
            {typeof (double), TypeCode.Double},
            {typeof (short), TypeCode.Int16},
            {typeof (int), TypeCode.Int32},
            {typeof (long), TypeCode.Int64},
            {typeof (object), TypeCode.Object},
            {typeof (sbyte), TypeCode.SByte},
            {typeof (float), TypeCode.Single},
            {typeof (string), TypeCode.String},
            {typeof (ushort), TypeCode.UInt16},
            {typeof (uint), TypeCode.UInt32},
            {typeof (ulong), TypeCode.UInt64},
        };
    }
}