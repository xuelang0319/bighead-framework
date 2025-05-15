﻿//
// = The script is part of BigHead and the framework is individually owned by Eric Lee.
// = Cannot be commercially used without the authorization.
//
//  Author  |  UpdateTime     |   Desc  
//  Eric    |  2021年2月28日   |   Log封装类
//  Eric    |  2021年6月25日   |   使用编译指令优化Log封装类
//  Eric    |  2025年5月15日   |   添加回调式打印，只在Editor模式下生效，非Editor模式不产生GC
//

using System;
using UnityEngine;

namespace Bighead.Core.Utility
{
    public static class Speaker
    {
        public static void Log(Func<object> callback)
        {
#if UNITY_EDITOR
            var message = callback.Invoke();
            Debug.Log($"[>>] {message}");
#endif
        }

        public static void Warning(Func<object> callback)
        {
#if UNITY_EDITOR
            var message = callback.Invoke();
            Debug.LogWarning($"<color=yellow>[>>] {message}</color>");
#endif
        }
        public static void Highlight(Func<object> callback)
        {
#if UNITY_EDITOR
            var message = callback.Invoke();
            Debug.Log($"<color=green>[>>] {message}</color>");
#endif
        }

        public static void Error(Func<object> callback)
        {
#if UNITY_EDITOR
            var message = callback.Invoke();
            Debug.LogError($"<color=red>[>>] {message}</color>");
#endif
        }
        
        public static void Exception(Func<Exception> callback)
        {
#if UNITY_EDITOR
            var message = callback.Invoke();
            Debug.LogError($"<color=red>[Exception] {message}</color>");
#endif
        }
        
        public static void Log(this object message)
        {
            Debug.Log($"[>>] {message}");
        }

        public static void Warning(this object message)
        {
            Debug.LogWarning($"<color=yellow>[>>] {message}</color>");
        }

        public static void Error(this object message)
        {
            Debug.LogError($"<color=red>[>>] {message}</color>");
        }

        public static void Highlight(this object message)
        {
            Debug.Log($"<color=green>[>>] {message}</color>");
        }

        public static void Exception(this string exception)
        {
            Debug.LogError($"<color=red>[Exception] {exception}</color>");
        }

        public static void Exception(this Exception exception)
        {
            Debug.LogError($"<color=red>[Exception] {exception.Message}</color>");
        }
    }
}