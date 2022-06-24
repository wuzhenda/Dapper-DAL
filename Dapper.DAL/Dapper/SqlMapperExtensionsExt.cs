using Dapper.DAL.Infrastructure.Enum;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using static Dapper.Contrib.Extensions.SqlMapperExtensions;

namespace Dapper.Contrib.Extensions
{
    public static partial class SqlMapperExtensionsExt
    {
        private static readonly ConcurrentDictionary<RuntimeTypeHandle, string> GetQueriesAll = new ConcurrentDictionary<RuntimeTypeHandle, string>();
        private static readonly ConcurrentDictionary<int, SqlWhereOrderCache> GetQueriesWhereOrder = new ConcurrentDictionary<int, SqlWhereOrderCache>();

        #region Extra Select

        /// <summary>
        /// </summary>
        /// <typeparam name="T">Interface type to create and populate</typeparam>
        /// <param name="connection">Open SqlConnection</param>
        /// <param name="where"></param>
        /// <param name="order"></param>
        /// <returns>Entity of T</returns>
        public static IEnumerable<T> GetBy<T>(this IDbConnection connection, object where = null, object order = null, IDbTransaction transaction = null, int? commandTimeout = null) where T : class
        {
            var type = typeof(T);
            var isUseWhere = where != null;
            var isUseOrder = order != null;
            if (!isUseWhere && !isUseOrder)
            {
                return SqlMapperExtensions.GetAll<T>(connection: connection, transaction: transaction, commandTimeout: commandTimeout);
            }
            var whereType = isUseWhere ? where.GetType() : null;
            var orderType = isUseOrder ? order.GetType() : null;
            SqlWhereOrderCache cache;
            var key = GetKeyTypeWhereOrder(type, whereType, orderType);
            if (!GetQueriesWhereOrder.TryGetValue(key, out cache))
            {
                cache = new SqlWhereOrderCache();
                if (isUseWhere)
                {
                    cache.Where = GetListOfNames(whereType.GetProperties());
                }
                if (isUseOrder)
                {
                    cache.Order = GetListOfNames(orderType.GetProperties());
                }
                var name = GetTableName(type);
                StringBuilder sb = new StringBuilder();
                sb.AppendFormat("select * from {0}", name);
                int cnt, last, i;
                if (isUseWhere)
                {
                    sb.Append(" where ");
                    cnt = cache.Where.Count();
                    last = cnt - 1;
                    for (i = 0; i < cnt; i++)
                    {
                        var prop = cache.Where.ElementAt(i);
                        sb.AppendFormat("[{0}]=@{1}", prop, prop);
                        if (i != last)
                        {
                            sb.Append(" and ");
                        }

                    }
                }
                if (isUseOrder)
                {
                    sb.Append(" order by ");
                    cnt = cache.Order.Count();
                    last = cnt - 1;
                    for (i = 0; i < cnt; i++)
                    {
                        var prop = cache.Order.ElementAt(i);
                        sb.AppendFormat("[{0}] #{1}", prop, prop);
                        if (i != last)
                        {
                            sb.Append(", ");
                        }
                    }
                }

                // TODO: pluralizer 
                // TODO: query information schema and only select fields that are both in information schema and underlying class / interface 
                cache.Sql = sb.ToString();
                GetQueriesWhereOrder[key] = cache;
            }

            IEnumerable<T> obj = null;
            var dynParms = new DynamicParameters();
            if (isUseWhere)
            {
                foreach (string name in cache.Where)
                {
                    dynParms.Add(name, whereType.GetProperty(name).GetValue(where, null));
                }
            }
            if (isUseOrder)
            {
                foreach (string name in cache.Order)
                {
                    SortAs enumVal = (SortAs)orderType.GetProperty(name).GetValue(order, null);
                    switch (enumVal)
                    {
                        case SortAs.Asc:
                            cache.Sql = cache.Sql.Replace("#" + name, "ASC");
                            break;
                        case SortAs.Desc:
                            cache.Sql = cache.Sql.Replace("#" + name, "DESC");
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            }
            if (type.IsInterface)
            {
                var res = connection.Query(cache.Sql);
                if (!res.Any())
                    return (IEnumerable<T>)((object)null);
                var objList = new List<T>();
                foreach (var item in res)
                {
                    T objItem = ProxyGenerator.GetInterfaceProxy<T>();

                    foreach (var property in TypePropertiesCache(type))
                    {
                        var val = item[property.Name];
                        property.SetValue(objItem, val, null);
                    }

                    ((IProxy)objItem).IsDirty = false;   //reset change tracking and return   
                    objList.Add(objItem);
                }
                obj = objList.AsEnumerable();
            }
            else
            {
                obj = connection.Query<T>(cache.Sql, dynParms, transaction: transaction, commandTimeout: commandTimeout);
            }
            return obj;
        }
        #endregion


        /// <summary>
        /// Specify a custom table name mapper based on the POCO type name
        /// </summary>
#pragma warning disable CA2211 // Non-constant fields should not be visible - I agree with you, but we can't do that until we break the API
        public static TableNameMapperDelegate TableNameMapper;
#pragma warning restore CA2211 // Non-constant fields should not be visible
        private static readonly ConcurrentDictionary<RuntimeTypeHandle, string> TypeTableName = new ConcurrentDictionary<RuntimeTypeHandle, string>();
        private static string GetTableName(Type type)
        {
            if (TypeTableName.TryGetValue(type.TypeHandle, out string name)) return name;

            if (TableNameMapper != null)
            {
                name = TableNameMapper(type);
            }
            else
            {
                //NOTE: This as dynamic trick falls back to handle both our own Table-attribute as well as the one in EntityFramework 
                var tableAttrName =
                    type.GetCustomAttribute<TableAttribute>(false)?.Name
                    ?? (type.GetCustomAttributes(false).FirstOrDefault(attr => attr.GetType().Name == "TableAttribute") as dynamic)?.Name;

                if (tableAttrName != null)
                {
                    name = tableAttrName;
                }
                else
                {
                    name = type.Name + "s";
                    if (type.IsInterface && name.StartsWith("I"))
                        name = name.Substring(1);
                }
            }

            TypeTableName[type.TypeHandle] = name;
            return name;
        }


        private static class ProxyGenerator
        {
            private static readonly Dictionary<Type, Type> TypeCache = new Dictionary<Type, Type>();

            private static AssemblyBuilder GetAsmBuilder(string name)
            {
#if !NET461
                return AssemblyBuilder.DefineDynamicAssembly(new AssemblyName { Name = name }, AssemblyBuilderAccess.Run);
#else
                return Thread.GetDomain().DefineDynamicAssembly(new AssemblyName { Name = name }, AssemblyBuilderAccess.Run);
#endif
            }

            public static T GetInterfaceProxy<T>()
            {
                Type typeOfT = typeof(T);

                if (TypeCache.TryGetValue(typeOfT, out Type k))
                {
                    return (T)Activator.CreateInstance(k);
                }
                var assemblyBuilder = GetAsmBuilder(typeOfT.Name);

                var moduleBuilder = assemblyBuilder.DefineDynamicModule("SqlMapperExtensions." + typeOfT.Name); //NOTE: to save, add "asdasd.dll" parameter

                var interfaceType = typeof(IProxy);
                var typeBuilder = moduleBuilder.DefineType(typeOfT.Name + "_" + Guid.NewGuid(),
                    TypeAttributes.Public | TypeAttributes.Class);
                typeBuilder.AddInterfaceImplementation(typeOfT);
                typeBuilder.AddInterfaceImplementation(interfaceType);

                //create our _isDirty field, which implements IProxy
                var setIsDirtyMethod = CreateIsDirtyProperty(typeBuilder);

                // Generate a field for each property, which implements the T
                foreach (var property in typeof(T).GetProperties())
                {
                    var isId = property.GetCustomAttributes(true).Any(a => a is KeyAttribute);
                    CreateProperty<T>(typeBuilder, property.Name, property.PropertyType, setIsDirtyMethod, isId);
                }

#if NETSTANDARD2_0
                var generatedType = typeBuilder.CreateTypeInfo().AsType();
#else
                var generatedType = typeBuilder.CreateType();
#endif

                TypeCache.Add(typeOfT, generatedType);
                return (T)Activator.CreateInstance(generatedType);
            }

            private static MethodInfo CreateIsDirtyProperty(TypeBuilder typeBuilder)
            {
                var propType = typeof(bool);
                var field = typeBuilder.DefineField("_" + nameof(IProxy.IsDirty), propType, FieldAttributes.Private);
                var property = typeBuilder.DefineProperty(nameof(IProxy.IsDirty),
                                               System.Reflection.PropertyAttributes.None,
                                               propType,
                                               new[] { propType });

                const MethodAttributes getSetAttr = MethodAttributes.Public | MethodAttributes.NewSlot | MethodAttributes.SpecialName
                                                  | MethodAttributes.Final | MethodAttributes.Virtual | MethodAttributes.HideBySig;

                // Define the "get" and "set" accessor methods
                var currGetPropMthdBldr = typeBuilder.DefineMethod("get_" + nameof(IProxy.IsDirty),
                                             getSetAttr,
                                             propType,
                                             Type.EmptyTypes);
                var currGetIl = currGetPropMthdBldr.GetILGenerator();
                currGetIl.Emit(OpCodes.Ldarg_0);
                currGetIl.Emit(OpCodes.Ldfld, field);
                currGetIl.Emit(OpCodes.Ret);
                var currSetPropMthdBldr = typeBuilder.DefineMethod("set_" + nameof(IProxy.IsDirty),
                                             getSetAttr,
                                             null,
                                             new[] { propType });
                var currSetIl = currSetPropMthdBldr.GetILGenerator();
                currSetIl.Emit(OpCodes.Ldarg_0);
                currSetIl.Emit(OpCodes.Ldarg_1);
                currSetIl.Emit(OpCodes.Stfld, field);
                currSetIl.Emit(OpCodes.Ret);

                property.SetGetMethod(currGetPropMthdBldr);
                property.SetSetMethod(currSetPropMthdBldr);
                var getMethod = typeof(IProxy).GetMethod("get_" + nameof(IProxy.IsDirty));
                var setMethod = typeof(IProxy).GetMethod("set_" + nameof(IProxy.IsDirty));
                typeBuilder.DefineMethodOverride(currGetPropMthdBldr, getMethod);
                typeBuilder.DefineMethodOverride(currSetPropMthdBldr, setMethod);

                return currSetPropMthdBldr;
            }

            private static void CreateProperty<T>(TypeBuilder typeBuilder, string propertyName, Type propType, MethodInfo setIsDirtyMethod, bool isIdentity)
            {
                //Define the field and the property 
                var field = typeBuilder.DefineField("_" + propertyName, propType, FieldAttributes.Private);
                var property = typeBuilder.DefineProperty(propertyName,
                                               System.Reflection.PropertyAttributes.None,
                                               propType,
                                               new[] { propType });

                const MethodAttributes getSetAttr = MethodAttributes.Public
                                                    | MethodAttributes.Virtual
                                                    | MethodAttributes.HideBySig;

                // Define the "get" and "set" accessor methods
                var currGetPropMthdBldr = typeBuilder.DefineMethod("get_" + propertyName,
                                             getSetAttr,
                                             propType,
                                             Type.EmptyTypes);

                var currGetIl = currGetPropMthdBldr.GetILGenerator();
                currGetIl.Emit(OpCodes.Ldarg_0);
                currGetIl.Emit(OpCodes.Ldfld, field);
                currGetIl.Emit(OpCodes.Ret);

                var currSetPropMthdBldr = typeBuilder.DefineMethod("set_" + propertyName,
                                             getSetAttr,
                                             null,
                                             new[] { propType });

                //store value in private field and set the isdirty flag
                var currSetIl = currSetPropMthdBldr.GetILGenerator();
                currSetIl.Emit(OpCodes.Ldarg_0);
                currSetIl.Emit(OpCodes.Ldarg_1);
                currSetIl.Emit(OpCodes.Stfld, field);
                currSetIl.Emit(OpCodes.Ldarg_0);
                currSetIl.Emit(OpCodes.Ldc_I4_1);
                currSetIl.Emit(OpCodes.Call, setIsDirtyMethod);
                currSetIl.Emit(OpCodes.Ret);

                //TODO: Should copy all attributes defined by the interface?
                if (isIdentity)
                {
                    var keyAttribute = typeof(KeyAttribute);
                    var myConstructorInfo = keyAttribute.GetConstructor(Type.EmptyTypes);
                    var attributeBuilder = new CustomAttributeBuilder(myConstructorInfo, Array.Empty<object>());
                    property.SetCustomAttribute(attributeBuilder);
                }

                property.SetGetMethod(currGetPropMthdBldr);
                property.SetSetMethod(currSetPropMthdBldr);
                var getMethod = typeof(T).GetMethod("get_" + propertyName);
                var setMethod = typeof(T).GetMethod("set_" + propertyName);
                typeBuilder.DefineMethodOverride(currGetPropMthdBldr, getMethod);
                typeBuilder.DefineMethodOverride(currSetPropMthdBldr, setMethod);
            }
        }

        private static readonly ConcurrentDictionary<RuntimeTypeHandle, IEnumerable<PropertyInfo>> TypeProperties = new ConcurrentDictionary<RuntimeTypeHandle, IEnumerable<PropertyInfo>>();
        private static List<PropertyInfo> TypePropertiesCache(Type type)
        {
            if (TypeProperties.TryGetValue(type.TypeHandle, out IEnumerable<PropertyInfo> pis))
            {
                return pis.ToList();
            }

            var properties = type.GetProperties().Where(IsWriteable).ToArray();
            TypeProperties[type.TypeHandle] = properties;
            return properties.ToList();
        }

        private static bool IsWriteable(PropertyInfo pi)
        {
            var attributes = pi.GetCustomAttributes(typeof(WriteAttribute), false).AsList();
            if (attributes.Count != 1) return true;

            var writeAttribute = (WriteAttribute)attributes[0];
            return writeAttribute.Write;
        }

        private static int GetKeyTypeWhereOrder(Type type, Type where, Type order)
        {
            var handler = type.TypeHandle;
            string whereCondition = @where != null ? @where.TypeHandle.Value.ToString() : string.Empty;
            string orderCondition = order != null ? order.TypeHandle.Value.ToString() : string.Empty; ;
            var str = string.Format("{0}{1}{2}", handler.Value, whereCondition, orderCondition);
            return str.GetHashCode();
        }

        private static IEnumerable<string> GetListOfNames(PropertyInfo[] list)
        {
            List<string> lst = new List<string>();
            foreach (PropertyInfo info in list)
            {
                lst.Add(info.Name);
            }
            return lst.AsEnumerable();
        }
    }

    public class SqlWhereOrderCache
    {
        public string Sql { get; set; }
        public IEnumerable<string> Where { get; set; }
        public IEnumerable<string> Order { get; set; }
    }
}
