//
// = The script is part of BigHead and the framework is individually owned by Eric Lee.
// = Cannot be commercially used without the authorization.
//
//  Author  |  UpdateTime     |   Desc  
//  Eric    |  2021年5月30日   |   正交摄像机助手
//

using UnityEngine;

namespace Bighead.Core.Utility
{
    public static class CameraHelper
    {
        /// <summary>
        /// Calculate the world position in ugui object's local position. 
        /// </summary>
        public static Vector2 WorldPositionToRectTransformPosition(Canvas canvas, RectTransform ParentRect,
            Vector3 worldPos)
        {
            if (!canvas.worldCamera)
            {
                $"Can't find Canvas world camera, please check. Canvas name: {canvas.name}".Exception();
                return Vector2.zero;
            }

            Camera worldCamera = canvas.worldCamera;
            var screenPoint = worldCamera.WorldToScreenPoint(worldPos);

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                ParentRect, screenPoint, worldCamera, out var pos);

            return pos;
        }

        /// <summary>
        /// Reset camera transform.
        /// </summary>
        public static Camera ResetCameraTransform(this Camera camera)
        {
            var transform = camera.transform;
            transform.position = new Vector3(0, 0, -10);
            transform.rotation = Quaternion.identity;
            return camera;
        }

        /// <summary>
        /// Auto calculate vertical camera orthographic size. If screen scale greater than design scale then calculate with height, other wise will calculate with width.
        /// </summary>
        public static void AutoCalculateVerticalCameraOrthographicSize(this Camera camera, int designWidth,
            int designHeight)
        {
            var designScale = (float) designWidth / (float) designHeight;
            var screenScale = (float) Screen.width / (float) Screen.height;

            camera.orthographic = true;
            camera.orthographicSize = designScale < screenScale
                ? __calculateCameraOrthographicSizeWithDesignHeight(designHeight)
                : __calculateCameraOrthographicSizeWithDesignWidthAndDesignScale(designWidth);
        }

        /// <summary>
        /// Use design width and design scale calculate camera orthographic mode size, which screen is vertical mode.
        /// </summary>
        public static void CalculateCameraOrthographicSizeWithDesignWidthAndDesignScale(this Camera camera,
            int designWidth, float designScale)
        {
            camera.orthographic = true;
            camera.orthographicSize = __calculateCameraOrthographicSizeWithDesignWidthAndDesignScale(designWidth);
        }

        /// <summary>
        /// Use design height calculate camera orthographic mode size, which screen is vertical mode.
        /// </summary>
        private static void CalculateCameraOrthographicSizeWithDesignHeight(this Camera camera,
            int designHeight)
        {
            camera.orthographic = true;
            camera.orthographicSize = __calculateCameraOrthographicSizeWithDesignHeight(designHeight);
        }

        private static float __calculateCameraOrthographicSizeWithDesignWidthAndDesignScale(int designWidth)
        {
            var screenScale = (float) Screen.width / (float) Screen.height;
            return (float) designWidth / screenScale / 2f / 100f;

        }

        private static float __calculateCameraOrthographicSizeWithDesignHeight(int designHeight)
        {
            return (float) designHeight / 2f / 100f;
        }
    }
}