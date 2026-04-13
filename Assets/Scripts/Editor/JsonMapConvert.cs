using System.Collections.Generic;
using System.IO;
using LitJson;
using UnityEditor;
using UnityEngine;

namespace Editor
{
    public class JsonMapConvert
    {
        private const float LAYER_HEIGHT = .5f;
        private static Dictionary<int, string> prefabDic = new Dictionary<int, string>();
        private static Dictionary<int, string> gidResDic = new Dictionary<int, string>();
        private static Dictionary<int, int> gidIndexDic = new Dictionary<int, int>();
        private static Dictionary<int, Vector4> tilingOffsetDic = new Dictionary<int, Vector4>();
        private static Dictionary<string, string> areaNameDic;
        private static Dictionary<string, string> atlasNameDic;

        private static Dictionary<string, string> tilesetNameDic;

        // private static Dictionary<int, Dictionary<string, int>> tileDic; //全部格子，key是layer，key是x_z，value是格子gid
        private static Dictionary<string, TileVo> tileVoDic; //全部格子，key是layer，key是x_z，value是格子gid

        [MenuItem("Assets/转换地图")]
        public static void ConvertMapData()
        {
            // tileDic = new Dictionary<int, Dictionary<string, int>>();
            tileVoDic = new Dictionary<string, TileVo>();
            var arr = Selection.GetFiltered(typeof(System.Object), SelectionMode.Assets);
            TextAsset mapData = (TextAsset)arr[0];
            JsonData json = JsonMapper.ToObject(mapData.text);
            int width = (int)json["width"];
            int height = (int)json["height"];
            var tileSets = json["tilesets"];
            var layers = json["layers"];
            // ParseTileSet(tileSets);
            ParseLayers(layers);
            CreateMesh();
            Debug.Log(tileVoDic);
        }

        private static void ParseTileSet(JsonData sets)
        {
            int tileCount = 1;
            int count = sets.Count;
            for (int i = 0; i < count; i++)
            {
                var item = sets[i];
                //图集
                if (item.ContainsKey("image"))
                {
                    var image = item["image"].ToString();
                    var name = item["name"].ToString();
                    var picName = Path.GetFileNameWithoutExtension(image);
                    tilesetNameDic.TryAdd(picName, name);
                    var col = (int)item["columns"];
                    var itemTileCount = (int)item["tilecount"];
                    var row = itemTileCount / col;
                    var gid = tileCount;
                    for (int j = 0; j < itemTileCount; j++)
                    {
                        var tx = j % col;
                        var ty = j / col;
                        string prefabName = "Pre_" + picName;
                        prefabDic.Add(gid, prefabName);
                        gidResDic.Add(gid, picName);
                        var tilingOffset = new Vector4(1.0f / col, 1.0f / row, tx * 1.0f / col,
                            (row - ty - 1) * 1.0f / row);
                        tilingOffsetDic.TryAdd(gid, tilingOffset);
                        gid++;
                    }
                }
                else
                {
                    //散图
                    var finalGid = 0;
                    var picTiles = item["tiles"];
                    if (picTiles == null)
                    {
                        continue;
                    }

                    for (int j = 0; j < picTiles.Count; j++)
                    {
                        var pic = picTiles[j];
                        var picId = (int)pic["id"];
                        var imageName = pic["image"].ToString();
                        var imageWidth = (int)pic["imagewidth"];
                        var imageHeight = (int)pic["imageheight"];
                        var picName = Path.GetFileNameWithoutExtension(imageName);
                        var gid = tileCount + picId;
                        var prefabName = "Pre_" + picName;
                        prefabDic.Add(gid, prefabName);
                        gidResDic.Add(gid, picName);
                        finalGid = gid;
                    }

                    tileCount = finalGid + 1;
                }
            }
        }

        private static void ParseLayers(JsonData layers)
        {
            for (int i = 0; i < layers.Count; i++)
            {
                var item = layers[i];
                var type = (string)item["type"];
                var layerIndex = (int)item["id"];
                if (type == "tilelayer")
                {
                    ParseLayer(item, layerIndex);
                }
                else if (type == "objectgroup")
                {
                }
                else if (type == "group")
                {
                    ParseLayers(item["layers"]);
                }
            }
        }

        private static void ParseLayer(JsonData vo, int layerIndex)
        {
            if (vo.ContainsKey("offsetx") && vo.ContainsKey("offsety"))
            {
                var x = float.Parse(vo["offsetx"].ToString());
                var y = float.Parse(vo["offsety"].ToString());
            }

            var chunks = vo["chunks"];
            for (int i = 0; i < chunks.Count; i++)
            {
                var chunkVo = chunks[i];
                ParseChunk(chunkVo, layerIndex);
            }
        }

        private static void ParseChunk(JsonData vo, int layerIndex)
        {
            var x = (int)vo["x"];
            var y = (int)vo["y"];
            var w = (int)vo["width"];
            var h = (int)vo["height"];
            var chunks = vo["data"];
            for (int i = 0; i < chunks.Count; i++)
            {
                uint rawGid = (uint)chunks[i];
                if (rawGid == 0)
                {
                    continue;
                }

                var col = i % w;
                var row = i / w;
                var globalCol = x + col;
                var globalRow = y + row;
                var tileKey = globalCol + "_" + globalRow;
                // tileDic.TryAdd(layerIndex, new Dictionary<string, int>());
                // if (tileDic.TryGetValue(layerIndex, out var tileDicValue))
                // {
                //     tileDicValue.TryAdd(tileKey, (int)rawGid);
                // }
                // else
                // {
                //     tileDic.TryAdd(layerIndex, new Dictionary<string, int>());
                //     tileDic[layerIndex].TryAdd(tileKey, (int)rawGid);
                // }
                Debug.Log(tileKey);
                if (tileVoDic.TryGetValue(tileKey, out var tile))
                {
                    if (layerIndex > tile.layerIndex)
                    {
                        var tileVo = new TileVo
                        {
                            layerIndex = layerIndex,
                            gid = (int)rawGid,
                            x = globalCol,
                            z = globalRow
                        };
                        tileVoDic[tileKey] = tileVo;
                    }
                }
                else
                {
                    var tileVo = new TileVo
                    {
                        layerIndex = layerIndex,
                        gid = (int)rawGid,
                        x = globalCol,
                        z = globalRow
                    };
                    tileVoDic.TryAdd(tileKey, tileVo);
                }
            }
        }

        private static void CreateMesh()
        {
            var path = $"Assets/RTSMap.asset";
            var mesh = new Mesh();
            var SIZE = tileVoDic.Count;
            int vertCount = SIZE * 4;
            int triCount = SIZE * 6;
            var vertices = new Vector3[vertCount];
            var triangles = new int[triCount];
            var uvs = new Vector2[vertCount];
            var index = 0; //第几个格子
            foreach (var tileVo in tileVoDic.Values)
            {
                var layerIndex1 = CheckLowerTile(tileVo.x, tileVo.z);
                var layerIndex2 = CheckLowerTile(tileVo.x - 1, tileVo.z);
                var layerIndex3 = CheckLowerTile(tileVo.x - 1, tileVo.z - 1);
                var layerIndex4 = CheckLowerTile(tileVo.x, tileVo.z - 1);
                var v1 = PtToMap(tileVo.x, tileVo.z, layerIndex1 * LAYER_HEIGHT);
                var v2 = PtToMap(tileVo.x - 1, tileVo.z, layerIndex2 * LAYER_HEIGHT);
                var v3 = PtToMap(tileVo.x - 1, tileVo.z - 1, layerIndex3 * LAYER_HEIGHT);
                var v4 = PtToMap(tileVo.x, tileVo.z - 1, layerIndex4 * LAYER_HEIGHT);
                vertices[index * 4] = v1;
                vertices[index * 4 + 1] = v2;
                vertices[index * 4 + 2] = v3;
                vertices[index * 4 + 3] = v4;
                uvs[index * 4] = new Vector2(0, 0);
                uvs[index * 4 + 1] = new Vector2(1, 0);
                uvs[index * 4 + 2] = new Vector2(1, 1);
                uvs[index * 4 + 3] = new Vector2(0, 1);

                // 检查当前格子是否需要斜面（有相邻格子高度低于当前格子）
                bool needsSlope = false;
                if (tileVoDic.TryGetValue((tileVo.x - 1) + "_" + tileVo.z, out var leftTileVo))
                {
                    if (leftTileVo.layerIndex < tileVo.layerIndex)
                        needsSlope = true;
                }

                if (tileVoDic.TryGetValue(tileVo.x + "_" + (tileVo.z - 1), out var downTileVo))
                {
                    if (downTileVo.layerIndex < tileVo.layerIndex)
                        needsSlope = true;
                }

                if (needsSlope)
                {
                    // 斜面：两个三角形沿对角线 1-2 分割
                    triangles[index * 6] = index * 4;
                    triangles[index * 6 + 1] = index * 4 + 1;
                    triangles[index * 6 + 2] = index * 4 + 2;
                    triangles[index * 6 + 3] = index * 4;
                    triangles[index * 6 + 4] = index * 4 + 2;
                    triangles[index * 6 + 5] = index * 4 + 3;
                }
                else
                {
                    // 平面：两个三角形沿对角线 0-2 分割
                    triangles[index * 6] = index * 4;
                    triangles[index * 6 + 1] = index * 4 + 1;
                    triangles[index * 6 + 2] = index * 4 + 3;
                    triangles[index * 6 + 3] = index * 4 + 1;
                    triangles[index * 6 + 4] = index * 4 + 2;
                    triangles[index * 6 + 5] = index * 4 + 3;
                }

                index++;
            }

            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.uv = uvs;
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();

            var so = new SerializedObject(mesh);
            var pro = so.FindProperty("m_IsReadable");
            pro.boolValue = false;
            so.ApplyModifiedProperties();
            AssetDatabase.CreateAsset(mesh, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            var oldGo = GameObject.Find("RTSMap");
            if (oldGo != null)
            {
                Object.DestroyImmediate(oldGo);
            }

            var go = new GameObject("RTSMap");
            var filter = go.AddComponent<MeshFilter>();
            filter.mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            var render = go.AddComponent<MeshRenderer>();
            render.sharedMaterial = new Material(Shader.Find("Standard"));
        }

        private static readonly float HalfTileW = 0.5f;
        private static readonly float HalfTileH = 0.5f;

        private static Vector3 PtToMap(float tx, float tz, float y)
        {
            float x = (tx - tz) * HalfTileW;
            float z = (tx + tz) * HalfTileH;
            return new Vector3(x, y, -z);
        }

        //检查覆盖指定顶点的所有格子，返回最高的层级
        private static int CheckLowerTile(int tx, int tz)
        {
            // 顶点位置被以下格子可能覆盖：
            // 格子(tx, tz)的右下角 v1
            // 格子(tx, tz+1)的左下角 v4
            // 格子(tx+1, tz)的右上角 v2
            // 格子(tx+1, tz+1)的左上角 v3

            int maxLayer = -1;

            // 检查格子(tx, tz)
            var tileKey1 = tx + "_" + tz;
            if (tileVoDic.TryGetValue(tileKey1, out var tile1))
            {
                maxLayer = Mathf.Max(maxLayer, tile1.layerIndex);
            }

            // 检查格子(tx, tz+1)
            var tileKey2 = tx + "_" + (tz + 1);
            if (tileVoDic.TryGetValue(tileKey2, out var tile2))
            {
                maxLayer = Mathf.Max(maxLayer, tile2.layerIndex);
            }

            // 检查格子(tx+1, tz)
            var tileKey3 = (tx + 1) + "_" + tz;
            if (tileVoDic.TryGetValue(tileKey3, out var tile3))
            {
                maxLayer = Mathf.Max(maxLayer, tile3.layerIndex);
            }

            // 检查格子(tx+1, tz+1)
            var tileKey4 = (tx + 1) + "_" + (tz + 1);
            if (tileVoDic.TryGetValue(tileKey4, out var tile4))
            {
                maxLayer = Mathf.Max(maxLayer, tile4.layerIndex);
            }

            return maxLayer;
        }

        struct TileVo
        {
            public int layerIndex;
            public int gid;
            public int x;
            public int z;
        }
    }
}