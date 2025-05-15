using UnityEngine;

namespace framework_bighead.Utility
{
    public static class BigheadUtility
    {
        public static Mesh GetQuadMesh(float width, float height)
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


        public static bool CheckRangeLegal(this int index, int min, int max)
        {
            return index >= min && index <= max;
        }
    }
}