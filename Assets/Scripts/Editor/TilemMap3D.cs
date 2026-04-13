using UnityEditor;
using UnityEngine;

namespace Editor
{
    public class TilemMap3D
    {
        private const int SIZE = 10;
        private const float LAYER_HEIGHT = .5f;
        private static Vector2Int leftBottom = new Vector2Int(3, 3);
        private static Vector2Int rightTop = new Vector2Int(6, 6);


        private static void PreCreate()
        {
            var path = $"Assets/TileMap3D.asset";
            var m = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (m != null)
            {
                AssetDatabase.DeleteAsset(path);
            }
        }

        [MenuItem("Map/创建网格")]
        private static void Create()
        {
            // var path = Path.Combine(Application.dataPath, "DamageMesh.asset");
            var path = $"Assets/TileMap3D.asset";
            var m = Create3DMap();
            var so = new SerializedObject(m);
            var pro = so.FindProperty("m_IsReadable");
            pro.boolValue = false;
            so.ApplyModifiedProperties();
            AssetDatabase.CreateAsset(m, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            var go = new GameObject("TiledMap");
            var filter = go.AddComponent<MeshFilter>();
            filter.mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            var render = go.AddComponent<MeshRenderer>();
            render.sharedMaterial = new Material(Shader.Find("Standard"));
        }

        private static Mesh Create3DMap()
        {
            var mesh = new Mesh();
            int vertCount = SIZE * SIZE * 4;
            int triCount = SIZE * SIZE * 6;
            var vertices = new Vector3[vertCount];
            var triangles = new int[triCount];
            var uvs = new Vector2[vertCount];
            for (int i = 0; i < SIZE; i++)
            {
                for (int j = 0; j < SIZE; j++)
                {
                    var isUp1 = CheckIsUp(i, j);
                    var isUp2 = CheckIsUp(i, j + 1);
                    var isUp3 = CheckIsUp(i + 1, j + 1);
                    var isUp4 = CheckIsUp(i + 1, j);
                    var v1 = new Vector3(i, isUp1 ? LAYER_HEIGHT : 0, j);
                    var v2 = new Vector3(i, isUp2 ? LAYER_HEIGHT : 0, j + 1);
                    var v3 = new Vector3(i + 1, isUp3 ? LAYER_HEIGHT : 0, j + 1);
                    var v4 = new Vector3(i + 1, isUp4 ? LAYER_HEIGHT : 0, j);
                    //第几个格子
                    var index = (i * SIZE + j) * 4;
                    vertices[index] = v1;
                    vertices[index + 1] = v2;
                    vertices[index + 2] = v3;
                    vertices[index + 3] = v4;
                    uvs[index] = new Vector2(0, 0);
                    uvs[index + 1] = new Vector2(1, 0);
                    uvs[index + 2] = new Vector2(1, 1);
                    uvs[index + 3] = new Vector2(0, 1);
                    var flag = isUp2 != isUp4;
                    if (flag)
                    {
                        triangles[(i * SIZE + j) * 6] = index;
                        triangles[(i * SIZE + j) * 6 + 1] = index + 1;
                        triangles[(i * SIZE + j) * 6 + 2] = index + 3;
                        triangles[(i * SIZE + j) * 6 + 3] = index + 1;
                        triangles[(i * SIZE + j) * 6 + 4] = index + 2;
                        triangles[(i * SIZE + j) * 6 + 5] = index + 3;
                    }
                    else
                    {
                        triangles[(i * SIZE + j) * 6] = index;
                        triangles[(i * SIZE + j) * 6 + 1] = index + 1;
                        triangles[(i * SIZE + j) * 6 + 2] = index + 2;
                        triangles[(i * SIZE + j) * 6 + 3] = index;
                        triangles[(i * SIZE + j) * 6 + 4] = index + 2;
                        triangles[(i * SIZE + j) * 6 + 5] = index + 3;
                    }
                }
            }

            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.uv = uvs;
            // mesh.RecalculateBounds();
            // mesh.RecalculateNormals();

            return mesh;
        }

        private static bool CheckIsUp(int x, int z)
        {
            return x >= leftBottom.x && x < rightTop.x && z >= leftBottom.y && z < rightTop.y;
        }
    }
}