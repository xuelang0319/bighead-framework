//
// = The script is part of BigHead and the framework is individually owned by Eric Lee.
// = Cannot be commercially used without the authorization.
//
//  Author  |  UpdateTime     |   Desc  
//  Eric    |  2020年12月18日  |   类帮助器
//  Eric    |  2020年12月24日  |   1、 Assembly.GetCallingAssembly() 修改为 Type.Assembly 以解决当调用程序和检索类不在同一程序集时获取为空的问题。
//                            |   2、 添加HasImplementedRawGeneric方法，以获取继承泛型和接口的子类。
//  Eric    |  2020年01月13日  |   新增XML序列化方法。
//  Eric    |  2020年01月13日  |   新增二进制序列化及反序列化方法。
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Xml.Serialization;

namespace Bighead.Core.Utility
{
    public static class ClassHelper
    {
        /// <summary>
        /// 通过基类类型获取所有继承类类型
        /// </summary>
        public static Type[] GetAllDerivedTypes(this Type baseclass)
        {
            return baseclass.Assembly.GetTypes()
                .Where(type => type.HasImplementedRawGeneric(baseclass))
                .Where(t => !t.IsAbstract && t.IsClass)
                .ToArray();
        }
        
        /// <summary>
        /// 通过基类获取所有继承类并创建。
        /// </summary>
        public static IEnumerable<T> CreateAllDerivedClass<T>()
        {
            return GetAllDerivedTypes(typeof(T))
                .Select(t => (T)Activator.CreateInstance(t));
        }
        
        /// <summary>
        /// 通过基类获取所有继承类并创建。
        /// </summary>
        public static IEnumerable<T> CreateAllDerivedClass<T>(params object[] parameters)
        {
            return GetAllDerivedTypes(typeof(T))
                .Select(t => (T)Activator.CreateInstance(t, args: parameters));
        }
        
        /// <summary>
        /// 通过基类获取所有继承类并创建。
        /// </summary>
        public static IEnumerable<T> CreateAllDerivedClass<T>(this Type baseclass)
        {
            return GetAllDerivedTypes(baseclass)
                .Select(t => (T)Activator.CreateInstance(t));
        }
        
        /// <summary>
        /// 判断指定的类型 <paramref name="type"/> 是否是指定泛型类型的子类型，或实现了指定泛型接口。
        /// </summary>
        /// <param name="type">需要测试的类型。</param>
        /// <param name="generic">泛型接口类型，传入 typeof(IXxx&lt;&gt;)</param>
        /// <returns>如果是泛型接口的子类型，则返回 true，否则返回 false。</returns>
        public static bool HasImplementedRawGeneric(this Type type, Type generic)
        {
            if (Equals(null, type) || Equals(null, generic)) return false;
            
            var isTheRawGenericType = type.GetInterfaces().Any(IsTheRawGenericType);
            if (isTheRawGenericType) return true;

            while (type != null && type != typeof(object))
            {
                isTheRawGenericType = IsTheRawGenericType(type);
                if (isTheRawGenericType) return true;
                type = type.BaseType;
            }

            return false;
            
            bool IsTheRawGenericType(Type test)
                => generic == (test.IsGenericType ? test.GetGenericTypeDefinition() : test);
        }

        /// <summary>
        /// 二进制序列化类
        /// </summary>
        /// <param name="c">需要进行序列化的类。</param>
        /// <returns> 序列化结果，当序列化失败时返回string.Empty </returns>
        public static string StartSerializer<T>(this T c) where T : class
        {
            try
            {
                using (var stream = new MemoryStream())
                {
                    IFormatter formatter = new BinaryFormatter();
                    formatter.Serialize(stream, c);
                    stream.Position = 0;
                    var buffer = new byte[stream.Length];
                    stream.Read(buffer, 0, buffer.Length);
                    return Convert.ToBase64String(buffer);
                }
            }
            catch (Exception e)
            {
                e.Exception();
                return string.Empty;
            }
        }

        
        /// <summary>
        /// 反序列化
        /// </summary>
        /// <param name="str"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T StartDeserialize<T>(this string str) where T : class
        {
            try
            {
                T result;
                var buffer = Convert.FromBase64String(str);
                using (var stream = new MemoryStream(buffer))
                {
                    IFormatter formatter = new BinaryFormatter(); 
                    result = (T)formatter.Deserialize(stream);
                }
                
                return result;
            }
            catch (Exception e)
            {
                e.Exception();
                return default;
            }
        }

        /// <summary>
        /// Xml序列化
        /// </summary>
        /// <param name="c">
        /// 需要进行Xml序列化的类。
        /// 1、需添加Serializer特性或继承
        /// 2、序列化的属性需要添加Xml类型特性。如: [XmlElement]
        /// </param>
        /// <param name="path">创建文件全路径</param>
        public static void StartXmlSerializer<T>(this T c, string path) where T : class
        {
            try
            {
                var serializer = new XmlSerializer(c.GetType());
                using (Stream s = File.Create(path))
                {
                    serializer.Serialize(s, c);
                }
            }
            catch(Exception e)
            {
                e.Exception();
            }
        }
        
        /// <summary>
        /// 深拷贝，只能拷贝不嵌套的类型。
        /// </summary>
        public static T DeepCopy<T>(this T t)
        {
            if (t is string || t.GetType().IsValueType) return t;

            var result = Activator.CreateInstance(t.GetType());
            var fields = t.GetType().GetFields(
                BindingFlags.Public | 
                BindingFlags.NonPublic | 
                BindingFlags.Instance | 
                BindingFlags.Static);
            
            foreach (FieldInfo field in fields)
            {
                try { field.SetValue(result, DeepCopy(field.GetValue(t))); }
                catch { }
            }
            return (T)result;
        }
    }
}