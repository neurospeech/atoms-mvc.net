using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using System.Web.Mvc;
using System.Web.Script.Serialization;
using NeuroSpeech.Atoms.Entity;
using System.Collections.Concurrent;
using System.Xml.Serialization;
using System.Diagnostics;

namespace NeuroSpeech.Atoms.Linq {


    public interface IEntityWrapper { 
    }

    public class LinqField
    {
        public string Name { get; set; }
        public Type Type { get; set; }

        public Type OwnerType { get; set; }

        public PropertyInfo Property { get; set; }

        public string PropertyPath { get; set; }

        public Expression Expression { get; set; }

        public bool Protected { get; set; }
   }


    public class LinqFields : List<LinqField>
    {
        public LinqFields()
        {

        }

        public LinqFields(Dictionary<string,PropertyInfo> fields)
        {
            foreach (var item in fields.OrderBy(x=>x.Key))
            {
                this.Add(new LinqField { Name = item.Key, Type = item.Value.PropertyType , Property = item.Value });
            }
        }

        public LinqFields(Dictionary<string, Type> fields)
        {
            foreach (var item in fields.OrderBy(x => x.Key))
            {
                this.Add(new LinqField { Name = item.Key, Type = item.Value });
            }
        }

    }

    public static class LinqRuntimeTypeBuilder
    {
        //private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private static AssemblyName assemblyName = new AssemblyName() { Name = "DynamicLinqTypes" };
        private static ModuleBuilder moduleBuilder = null;
        private static ThreadSafeDictionary<string, Type> builtTypes = new ThreadSafeDictionary<string, Type>();
        private static ThreadSafeDictionary<string, string> typeNames = new ThreadSafeDictionary<string, string>();

        static LinqRuntimeTypeBuilder()
        {
            moduleBuilder = Thread.GetDomain().DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run).DefineDynamicModule(assemblyName.Name);
        }

        internal static Type GetDynamicType(Type pt, IEnumerable<LinqField> fields = null)
        {
            string className = pt.Name + "Wrapper";

            return builtTypes.GetOrAdd(className, x => {
                TypeBuilder typeBuilder = moduleBuilder.DefineType(className, TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.Serializable);
                typeBuilder.SetParent(pt);
                typeBuilder.AddInterfaceImplementation(typeof(IEntityWrapper));


                var cm = pt.GetConstructors().FirstOrDefault();

                var constructor = typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, fields.Select(p => p.Type).ToArray());
                var il = constructor.GetILGenerator();

                // call base class constructor...

                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Call, cm);

                int i = 1;
                // add constructor
                foreach (var field in fields)
                {
                    PropertyInfo prop = pt.GetProperty(field.Name);
                    var sm = prop.GetSetMethod();
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldarg, i++);
                    il.Emit(OpCodes.Call, sm);
                }
                il.Emit(OpCodes.Ret);

                constructor = typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, Type.EmptyTypes);
                constructor.GetILGenerator().Emit(OpCodes.Ret);


                if (fields != null)
                {
                    foreach (var field in fields)
                    {
                        PropertyInfo pbase = pt.GetProperty(field.Name);

                        var gm = typeBuilder.DefineMethod("get_" + field.Name, MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig | MethodAttributes.Virtual, field.Type, Type.EmptyTypes);

                        il = gm.GetILGenerator();
                        il.Emit(OpCodes.Ldarg_0);
                        il.Emit(OpCodes.Call, pbase.GetGetMethod());
                        il.Emit(OpCodes.Ret);


                        var p = typeBuilder.DefineProperty(field.Name, PropertyAttributes.HasDefault, field.Type, Type.EmptyTypes);

                        p.SetGetMethod(gm);

                        if (field.Protected)
                        {
                            var c = typeof(XmlIgnoreAttribute).GetConstructors().FirstOrDefault();
                            p.SetCustomAttribute(new CustomAttributeBuilder(c, new object[] { }));

                            c = typeof(ScriptIgnoreAttribute).GetConstructors().FirstOrDefault();
                            p.SetCustomAttribute(new CustomAttributeBuilder(c, new object[] { }));
                        }

                    }
                }

                return typeBuilder.CreateType();
            });
        }

        private static int TypeCount = 1;

        private static string GetTypeKey(IEnumerable<LinqField> fields, Type baseType = null)
        {
            //TODO: optimize the type caching -- if fields are simply reordered, that doesn't mean that they're actually different types, so this needs to be smarter
            string key = baseType == null ? string.Empty : baseType.FullName + "_";

            foreach (var field in fields.OrderBy(x=>x.Name))
                key += field.Name + "_" + field.Type.FullName + "_";

            return typeNames.GetOrAdd(key, k => {
                int c = Interlocked.Increment(ref TypeCount);
                var a = "SelectType" + c;
                return a;
            });

        }


        public static Type GetDynamicType(IEnumerable<LinqField> fields, Type baseType = null)
        {
            if (null == fields)
                throw new ArgumentNullException("fields");
            if (!fields.Any())
                throw new ArgumentOutOfRangeException("fields", "fields must have at least 1 field definition");

            string className = GetTypeKey(fields, baseType );


            return builtTypes.GetOrAdd(className, cname => {
                TypeBuilder typeBuilder = moduleBuilder.DefineType(cname, TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.Serializable);

                ILGenerator il;
                int i = 0;
                if (baseType != null)
                {
                    typeBuilder.SetParent(baseType);

                    // add constructor...
                    foreach(var c in baseType.GetConstructors())
                    {

                        var nc =typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, c.GetParameters().Select(x => x.ParameterType).ToArray());

                        il = nc.GetILGenerator();
                        il.Emit(OpCodes.Ldarg_0);
                        i = 1;
                        foreach (var item in c.GetParameters())
                        {
                            il.Emit(OpCodes.Ldarg, i++);
                        }
                        il.Emit(OpCodes.Call, c);
                        il.Emit(OpCodes.Ret);

                    }
                }



                foreach (var field in fields)
                {
                    var f = typeBuilder.DefineField("_" + field.Name, field.Type, FieldAttributes.Public);
                    var c = typeof(ScriptIgnoreAttribute).GetConstructors().FirstOrDefault();
                    f.SetCustomAttribute(new CustomAttributeBuilder( c, new object[] {} ));

                    c = typeof(XmlIgnoreAttribute).GetConstructors().FirstOrDefault();
                    f.SetCustomAttribute(new CustomAttributeBuilder(c, new object[] {}));
                    

                    var gm = typeBuilder.DefineMethod("get_" + field.Name, MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig, field.Type, Type.EmptyTypes);

                    il = gm.GetILGenerator();
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldfld, f);
                    il.Emit(OpCodes.Ret);


                    var p = typeBuilder.DefineProperty(field.Name, PropertyAttributes.HasDefault  , field.Type, Type.EmptyTypes);

                    if (field.Protected) {
                        c = typeof(XmlIgnoreAttribute).GetConstructors().FirstOrDefault();
                        p.SetCustomAttribute(new CustomAttributeBuilder(c, new object[] { }));

                        c = typeof(ScriptIgnoreAttribute).GetConstructors().FirstOrDefault();
                        p.SetCustomAttribute(new CustomAttributeBuilder(c, new object[] { }));
                    }

                    p.SetGetMethod(gm);

                    field.Property = p;
                }

                return typeBuilder.CreateType();
            });


        }


        //private static string GetTypeKey(Dictionary<string,PropertyInfo> fields)
        //{
        //    return GetTypeKey(fields.ToDictionary(f => f.Name, f => f.PropertyType));
        //}

        //public static Type GetDynamicType(Dictionary<string,PropertyInfo> fields)
        //{
        //    return GetDynamicType(fields.ToDictionary(f => f.Name, f => f.PropertyType));
        //}

        public static PropertyInfo GetDynamicProperty(this Type type, string name) {
            int index = name.IndexOf('.');
            if (index == -1)
                return type.GetProperty(name);
            string p = name.Substring(0, index);
            name = name.Substring(index + 1);

            type = type.GetProperty(p).PropertyType;
            return GetDynamicProperty(type, name);
        }

        public static MemberExpression NestedProperty(Expression source, Type sourceType, string name) {
            int index = name.IndexOf('.');
            if (index == -1)
                return Expression.Property(source, sourceType.GetProperty(name));
            string p = name.Substring(0, index);
            PropertyInfo pi = sourceType.GetProperty(p);
            name = name.Substring(index + 1);
            return NestedProperty( Expression.Property( source, pi), pi.PropertyType , name);
        }

    }

}