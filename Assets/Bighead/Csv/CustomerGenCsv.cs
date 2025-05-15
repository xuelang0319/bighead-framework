//
// = The script is part of BigHead and the framework is individually owned by Eric Lee.
// = Cannot be commercially used without the authorization.
//
//  Author  |  UpdateTime     |   Desc  
//  Eric    |  2021年5月23日   |
//

using System;
using System.Collections.Generic;
using System.Linq;
using Bighead.Core.Utility;
using JetBrains.Annotations;
using UnityEngine;

/*
 * 【注意】由于使用CSV文件规范，所以的Excel数据中都不能含有','符号！！！
 * Excel自动生成工具使用方法：
 * 1、格式: 固定前三行， 第一行为生成代码时的变量名称， 第二行为变量类型， 第三行为中文注释
 * 例:
 * Id        Type        Desc        Value        IsStatic
 * :int      :str        :str        :Array:Str   :Bool
 * 编号       类型         注释        值            是否静态
 * 100001    机甲         示例        护胸|肩甲      TRUE
 *
 * 2、特殊类型：如果需要添加新的类型和解析方式，可以在Assets/BigHead/Customer/CustomerGenCsv中添加以下数据：
 * ① GenPropertyType: 转义方法，返回真实变量类型。
 * ② GetTransformFunc: 转义方法，返回调用的解析方法名称。
 * ③ 在CustomerGenCsv脚本中添加②中的对应名称的解析方法，返回类型需与①中的变量真实类型相同。
 *
 * 3、特殊引用：如果生成的类型需要引用一些特殊的NameSpace,可以在Assets/BigHead/Customer/CustomerGenCsv.GetUsings()中添加相应的引用名称。
 * 例： "System" (不需要添加using前缀，也不要添加';'结束符)
 */


namespace Bighead.Csv
{
    public static class CustomerGenCsv
    {
        public const string CommaSymbol = "▐Ⓑⓘⓖⓗⓔⓐⓓ▌";

        /// <summary>
        /// 生成脚本的引用类型
        /// </summary>
        public static string[] GetUsingArray()
        {
            return new string[]
            {
                "System",
                "System.Linq",
                "UnityEngine",
                "System.Collections.Generic",
            };
        }

        /// <summary>
        /// 转义方法获取真实类型
        /// </summary>
        public static string GetPropertyType(string type, out bool unity)
        {
            unity = false;

            var typeArray = type.Split(":");

            var result = string.Empty;
            foreach (var clip in typeArray)
            {
                if (string.Equals(clip.ToLower(), "uni")) unity = true;
                else if (string.IsNullOrEmpty(result)) result = GetType(clip);
                else $"[CustomerGenCsv][GetPropertyType][Unknown]{type}".Exception();
            }

            return result;

            string GetType(string propertyType)
            {
                if (string.Equals(propertyType, "dict")) return "Dictionary<string, SuperNumber>";
                if (string.Equals(propertyType, "dict[]")) return "Dictionary<string, SuperNumber>[]";
                if (string.Equals(propertyType, "number")) return "SuperNumber";
                if (string.Equals(propertyType, "number[]")) return "SuperNumber[]";
                return propertyType;
            }
        }

        public static List<int> GetMultiKeysIndexList(List<string> types)
        {
            var result = new List<int>();
            for (int i = 0; i < types.Count; i++)
            {
                GetPropertyType(types[i], out var unity);
                if (unity)
                {
                    result.Add(i);
                }
            }

            return result;
        }

        /// <summary>
        /// 转义方法获取解析方法名称
        /// </summary>
        public static string GetTransformFunc(string type, string value)
        {
            var propertyType = GetPropertyType(type, out _);
            switch (propertyType)
            {
                case "string":
                    return value.Trim();
                case "string[]":
                    return $"ToStringArray({value})";
                case "List<string[]>":
                    return $"ToListStringArray({value})";
                case "byte":
                    return $"ToByte({value})";
                case "int":
                    return $"ToInt({value})";
                case "int[]":
                    return $"ToIntArray({value})";
                case "int[,]":
                    return $"ToInt2Array({value})";
                case "List<int[]>":
                    return $"ToListIntArray({value})";
                case "Vector2Int":
                    return $"ToVector2Int({value})";
                case "Vector2Int[]":
                    return $"ToVector2IntArray({value})";
                case "bool":
                    return $"ToBool({value})";
                case "bool[]":
                    return $"ToBoolArray({value})";
                case "float":
                    return $"ToFloat({value})";
                case "float[]":
                    return $"ToFloatArray({value})";
                case "float[][]":
                    return $"ToFloat2Array({value})";
                case "List<float[]>":
                    return $"ToListFloatArray({value})";
                case "Vector3[]":
                    return $"ToVector3Array({value})";
                default:
                    throw new Exception($"转换CSV数据类型错误，指定类型： {type}, 值: {value}");
            }
        }

        public static string ConvertCommaSymbol(string str)
        {
            return str.Trim().Replace(CommaSymbol, ",");
        }

        public static bool ToBool(string str)
        {
            if (string.IsNullOrEmpty(str) || string.IsNullOrWhiteSpace(str))
                return false;

            if (bool.TryParse(str, out var b))
            {
                return b;
            }

            if (int.TryParse(str, out var i))
            {
                return i > 0;
            }

            $"Can't convert boolean type - {str}.".Error();
            return false;
        }

        public static byte ToByte(string str)
        {
            try
            {
                return string.IsNullOrEmpty(str) || string.IsNullOrWhiteSpace(str) ? (byte)0 : byte.Parse(str);
            }
            catch (Exception e)
            {
                $"Parse string error: {str} - Exception : {e}".Error();
                return 0;
            }
        }

        public static int ToInt(string str)
        {
            try
            {
                return string.IsNullOrEmpty(str) || string.IsNullOrWhiteSpace(str) ? 0 : int.Parse(str);
            }
            catch (Exception e)
            {
                $"Parse string error: {str} - Exception : {e}".Error();
                return 0;
            }
        }

        public static Vector2Int ToVector2Int(string str)
        {
            try
            {
                if (string.IsNullOrEmpty(str) || string.IsNullOrWhiteSpace(str)) return Vector2Int.zero;
                var part = str.Split('|');
                if (part.Length != 2)
                {
                    $"Parse ToVector2Int error: {str}".Error();
                    return Vector2Int.zero;
                }

                return new Vector2Int(int.Parse(part[0]), int.Parse(part[1]));
            }
            catch (Exception e)
            {
                $"Parse string error: {str} - Exception : {e}".Error();
                return Vector2Int.zero;
            }
        }

        public static Vector2Int[] ToVector2IntArray(string str)
        {
            try
            {
                if (string.IsNullOrEmpty(str) || string.IsNullOrWhiteSpace(str)) return Array.Empty<Vector2Int>();
                var parts = str.Split('|');
                var array = new Vector2Int[parts.Length];
                for (int i = 0; i < parts.Length; i++)
                {
                    var part  = parts[i].Split(',');
                    if (part.Length != 2)
                    {
                        $"Parse ToVector2Int error: {str}".Error();
                        continue;
                    }
                    
                    array[i] = new Vector2Int(int.Parse(part[0]), int.Parse(part[1]));
                }

                return array;
            }
            catch (Exception e)
            {
                $"Parse string error: {str} - Exception : {e}".Error();
                return Array.Empty<Vector2Int>();
            }
        }

        public static float ToFloat(string str)
        {
            try
            {
                return string.IsNullOrEmpty(str) ? 0 : float.Parse(str);
            }
            catch (Exception e)
            {
                $"Parse string error: {str} - Exception : {e}".Error();
                return 0;
            }
        }

        public static int[] ToIntArray(string str)
        {
            try
            {
                if (string.IsNullOrEmpty(str)) return new int[0];
                return Array.ConvertAll(str.Split('|'), int.Parse);
            }
            catch (Exception e)
            {
                $"Parse string error: {str} - Exception : {e}".Error();
                return new int[0];
            }
        }

        public static List<int[]> ToListIntArray(string str)
        {
            if (string.IsNullOrEmpty(str)) return new List<int[]>();
            var part = str.Split('|');
            var list = new List<int[]>(part.Length);
            for (int i = 0; i < part.Length; i++)
            {
                list.Add(Array.ConvertAll(part[i].Split(','), int.Parse));
            }

            return list;
        }

        public static List<float[]> ToListFloatArray(string str)
        {
            if (string.IsNullOrEmpty(str)) return new List<float[]>();
            var part = str.Split('|');
            var list = new List<float[]>(part.Length);
            for (int i = 0; i < part.Length; i++)
            {
                list.Add(Array.ConvertAll(part[i].Split(','), float.Parse));
            }

            return list;
        }

        public static Vector3[] ToVector3Array(string str)
        {
            if (string.IsNullOrEmpty(str)) return new Vector3[0];
            var x = str.Split('|');
            var length = x.Length;
            var result = new Vector3[length];
            for (int i = 0; i < length; i++)
            {
                var array = Array.ConvertAll(x[i].Split(','), float.Parse);
                result[i] = new Vector3(array[0], 0, array[1]);
            }

            return result;
        }

        public static float[][] ToFloat2Array(string str)
        {
            if (string.IsNullOrEmpty(str)) return new float[0][];
            var x = str.Split('|');
            var result = new float[x.Length][];
            for (int i = 0; i < x.Length; i++)
            {
                result[i] = Array.ConvertAll(x[i].Split(','), float.Parse);
            }

            return result;
        }

        public static int[,] ToInt2Array(string str)
        {
            if (string.IsNullOrEmpty(str)) return new int[0, 0];
            var x = str.Split('|');
            var y = x[0].Split('_');
            int[,] temp = new int[x.Length, y.Length];
            for (int i = 0; i < x.Length; i++)
            {
                y = x[i].Split('_');
                for (int j = 0; j < y.Length; j++)
                {
                    int value = 0;
                    int.TryParse(y[j], out value);
                    temp[i, j] = value;
                }
            }

            return temp;
        }

        public static string[] ToStringArray(string str)
        {
            if (string.IsNullOrEmpty(str)) return new string[0];
            return str.Split('|');
        }

        public static List<string[]> ToListStringArray(string str)
        {
            var list = new List<string[]>();
            if (string.IsNullOrEmpty(str)) return list;
            var array = str.Split('|');
            for (int i = 0; i < array.Length; i++)
            {
                list.Add(array[i].Split(',').ToArray());
            }

            return list;
        }

        public static bool[] ToBoolArray(string str)
        {
            if (string.IsNullOrEmpty(str)) return new bool[0];
            return Array.ConvertAll(str.Split('|'), bool.Parse);
        }

        public static float[] ToFloatArray(string str)
        {
            if (string.IsNullOrEmpty(str)) return new float[0];
            return Array.ConvertAll(str.Split('|'), float.Parse);
        }


        [CanBeNull]
        public static string ToNull()
        {
            return null;
        }
    }
}