using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Xml.Serialization;

namespace Bighead.Core.Utility
{
    public static class FormatHelper
    {
        /// <summary>
        /// 字符串序列化
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static string ObjectToString<T>(T obj)
        {
            using var stream = new MemoryStream();
            var formatter = new BinaryFormatter();
            formatter.Serialize(stream, obj);
            stream.Position = 0;
            var buffer = new byte[stream.Length];
            stream.Read(buffer, 0, buffer.Length);
            return Convert.ToBase64String(buffer);
        }

        /// <summary>
        /// 字符串转对象（Base64编码字符串反序列化为对象）
        /// </summary>
        /// <param name="str">字符串</param>
        /// <returns>对象</returns>
        public static T StringToObject<T>(string str)
        {
            using var stream = new MemoryStream();
            var bytes = Convert.FromBase64String(str);
            stream.Write(bytes, 0, bytes.Length);
            stream.Position = 0;
            var formatter = new BinaryFormatter();
            return (T) formatter.Deserialize(stream);
        }

        /// <summary>
        /// XML序列化
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name"></param>
        /// <param name="o"></param>
        public static void XMLserializer<T>(string name, object o)
        {
            using var stream = new FileStream(name, FileMode.OpenOrCreate);
            var s = new XmlSerializer(typeof(T));
            s.Serialize(stream, o);
        }

        /// <summary>
        /// XML 反序列化为Repair
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name"></param>
        /// <returns></returns>
        public static T XmlReserializer<T>(string name)
        {
            using var stream = new FileStream(name, FileMode.OpenOrCreate);
            var s = new XmlSerializer(typeof(T));
            var o = s.Deserialize(stream);
            return (T) o;
        }
    }
}