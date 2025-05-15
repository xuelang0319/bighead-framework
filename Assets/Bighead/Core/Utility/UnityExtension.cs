//
// = The script is part of BigHead and the framework is individually owned by Eric Lee.
// = Cannot be commercially used without the authorization.
//
//  Author  |  UpdateTime     |   Desc  
//  Eric    |  2021年10月19日  |   Unity原生组件拓展方法
//

using UnityEngine;

namespace Bighead.Core.Utility
{
    public static class UnityExtension
    {
        /// <summary>
        /// 重置位移组件属性
        /// </summary>
        /// <param name="transform">被重置位移组件</param>
        public static void Reset(this Transform transform)
        {
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
        }

        /// <summary>
        /// 设置位移组件父物体，并重置位移组件属性
        /// </summary>
        /// <param name="transform">被重置位移组件</param>
        /// <param name="parent">位移组件父物体</param>
        public static void SetParentReset(this Transform transform, Transform parent)
        {
            transform.SetParent(parent);
            Reset(transform);
        }
        
        public static GameObject CreateQuadGameObject(string name, float width, float height, Material material)
        {
            var quad = new GameObject(name, typeof(MeshRenderer), typeof(MeshFilter));
            quad.GetComponent<MeshFilter>().mesh = CreateQuadMesh(width,height);
            var mesh = quad.GetComponent<MeshRenderer>();
            mesh.materials = new[] { material };
            return quad;
        }

        public static Mesh CreateQuadMesh(float width, float height)
        {
            width = width / 2f;
            height = height / 2f;
            Mesh mesh = new Mesh();

            Vector3[] vertices =
            {
                new Vector3(width, 0, height),

                new Vector3(width, 0, -height),

                new Vector3(-width, 0, height),

                new Vector3(-width, 0, -height),
            };

            Vector2[] uv =
            {
                new Vector2(1, 1),
                new Vector2(1, 0),
                new Vector2(0, 1),
                new Vector2(0, 0),
            };

            int[] triangles =
            {
                0, 1, 2,
                2, 1, 3,
            };

            mesh.vertices = vertices;
            mesh.uv = uv;
            mesh.triangles = triangles;

            return mesh;
        }
    }
}