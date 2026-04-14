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

        [MenuItem("Assets/生成地图网格/整体生成")]
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

        [MenuItem("Assets/生成地图网格/分块生成")]
        public static void ConvertMapDataChunk()
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
            CreateAllMesh();
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

        private static void CreateAllMesh()
        {
            var chunkDic = new Dictionary<string, List<TileVo>>();
            foreach (var tileVo in tileVoDic.Values)
            {
                var chunkX = Mathf.FloorToInt(tileVo.x / 16.0f);
                var chunkZ = Mathf.FloorToInt(tileVo.z / 16.0f);
                var chunkKey = chunkX + "_" + chunkZ;
                if (chunkDic.TryGetValue(chunkKey, out var list))
                {
                    list.Add(tileVo);
                }
                else
                {
                    chunkDic.TryAdd(chunkKey, new List<TileVo> { tileVo });
                }
            }

            foreach (var chunk in chunkDic.Values)
            {
                CreateMeshByChunk(chunk);
            }
        }

        private static void CreateMeshByChunk(List<TileVo> list)
        {
            var mesh = new Mesh();
            var vertList = new List<Vector3>();
            var triList = new List<int>();
            var uvList = new List<Vector2>();
            var vertDic = new Dictionary<string, int>(); // key: "x_z_y" -> vertIndex

            // 生成表面
            foreach (var tileVo in list)
            {
                var layerIndex1 = GetDrawLayerIndex(tileVo.x, tileVo.z, tileVo.layerIndex);
                var layerIndex2 = GetDrawLayerIndex(tileVo.x - 1, tileVo.z, tileVo.layerIndex);
                var layerIndex3 = GetDrawLayerIndex(tileVo.x - 1, tileVo.z - 1, tileVo.layerIndex);
                var layerIndex4 = GetDrawLayerIndex(tileVo.x, tileVo.z - 1, tileVo.layerIndex);

                // 获取或创建4个顶点
                int v0 = GetOrCreateVertex(tileVo.x, tileVo.z, layerIndex1, vertList, vertDic);
                int v1 = GetOrCreateVertex(tileVo.x - 1, tileVo.z, layerIndex2, vertList, vertDic);
                int v2 = GetOrCreateVertex(tileVo.x - 1, tileVo.z - 1, layerIndex3, vertList, vertDic);
                int v3 = GetOrCreateVertex(tileVo.x, tileVo.z - 1, layerIndex4, vertList, vertDic);

                // 斜面判断
                bool needsSlope = layerIndex1 != layerIndex3;


                if (needsSlope)
                {
                    // 斜面
                    triList.Add(v0);
                    triList.Add(v1);
                    triList.Add(v2);
                    triList.Add(v0);
                    triList.Add(v2);
                    triList.Add(v3);
                }
                else
                {
                    // 平面
                    triList.Add(v0);
                    triList.Add(v1);
                    triList.Add(v3);
                    triList.Add(v1);
                    triList.Add(v2);
                    triList.Add(v3);
                }
            }

            // 生成墙面
            foreach (var tileVo in list)
            {
                // 左侧墙面 
                if (tileVoDic.TryGetValue(tileVo.x + "_" + (tileVo.z + 1), out var leftTileVo))
                {
                    if (leftTileVo.layerIndex > tileVo.layerIndex)
                    {
                        continue; //这是右上侧的墙面
                    }

                    if (leftTileVo.layerIndex < tileVo.layerIndex)
                        AddWallTriangles(tileVo, leftTileVo, true, vertList, vertDic, triList);
                }

                // 右侧墙面 
                if (tileVoDic.TryGetValue((tileVo.x + 1) + "_" + tileVo.z, out var rightTileVo))
                {
                    if (rightTileVo.layerIndex > tileVo.layerIndex)
                    {
                        continue; //这是左上侧的墙面
                    }

                    if (rightTileVo.layerIndex < tileVo.layerIndex)
                        AddWallTriangles(tileVo, rightTileVo, false, vertList, vertDic, triList);
                }
            }

            // 填充UV
            for (int i = 0; i < vertList.Count; i++)
            {
                uvList.Add(new Vector2(0, 0));
            }

            mesh.vertices = vertList.ToArray();
            mesh.triangles = triList.ToArray();
            mesh.uv = uvList.ToArray();
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();

            var so = new SerializedObject(mesh);
            var pro = so.FindProperty("m_IsReadable");
            pro.boolValue = false;
            so.ApplyModifiedProperties();

            var chunkTile = list[0];
            var chunkX = Mathf.FloorToInt(chunkTile.x / 16.0f);
            var chunkZ = Mathf.FloorToInt(chunkTile.z / 16.0f);
            var chunkKey = chunkX + "_" + chunkZ;
            var path = $"Assets/{chunkKey}.asset";
            AssetDatabase.CreateAsset(mesh, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            var oldGo = GameObject.Find($"{chunkKey}");
            if (oldGo != null) Object.DestroyImmediate(oldGo);

            var go = new GameObject($"{chunkKey}");
            var filter = go.AddComponent<MeshFilter>();
            filter.mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            var render = go.AddComponent<MeshRenderer>();
            render.sharedMaterial = new Material(Shader.Find("Standard"));
        }

        private static void CreateMesh()
        {
            var mesh = new Mesh();
            var vertList = new List<Vector3>();
            var triList = new List<int>();
            var uvList = new List<Vector2>();
            var vertDic = new Dictionary<string, int>(); // key: "x_z_y" -> vertIndex

            // 生成表面
            foreach (var tileVo in tileVoDic.Values)
            {
                var layerIndex1 = GetDrawLayerIndex(tileVo.x, tileVo.z, tileVo.layerIndex);
                var layerIndex2 = GetDrawLayerIndex(tileVo.x - 1, tileVo.z, tileVo.layerIndex);
                var layerIndex3 = GetDrawLayerIndex(tileVo.x - 1, tileVo.z - 1, tileVo.layerIndex);
                var layerIndex4 = GetDrawLayerIndex(tileVo.x, tileVo.z - 1, tileVo.layerIndex);

                // 获取或创建4个顶点
                int v0 = GetOrCreateVertex(tileVo.x, tileVo.z, layerIndex1, vertList, vertDic);
                int v1 = GetOrCreateVertex(tileVo.x - 1, tileVo.z, layerIndex2, vertList, vertDic);
                int v2 = GetOrCreateVertex(tileVo.x - 1, tileVo.z - 1, layerIndex3, vertList, vertDic);
                int v3 = GetOrCreateVertex(tileVo.x, tileVo.z - 1, layerIndex4, vertList, vertDic);

                // 斜面判断
                bool needsSlope = layerIndex1 != layerIndex3;

                // if (tileVoDic.TryGetValue(tileVo.x + "_" + (tileVo.z - 1), out var downTileVo))
                // {
                //     if (downTileVo.layerIndex < tileVo.layerIndex)
                //         needsSlope = true;
                // }

                if (needsSlope)
                {
                    // 斜面
                    triList.Add(v0);
                    triList.Add(v1);
                    triList.Add(v2);
                    triList.Add(v0);
                    triList.Add(v2);
                    triList.Add(v3);
                }
                else
                {
                    // 平面
                    triList.Add(v0);
                    triList.Add(v1);
                    triList.Add(v3);
                    triList.Add(v1);
                    triList.Add(v2);
                    triList.Add(v3);
                }
            }

            // 生成墙面
            foreach (var tileVo in tileVoDic.Values)
            {
                // 左侧墙面 
                if (tileVoDic.TryGetValue(tileVo.x + "_" + (tileVo.z + 1), out var leftTileVo))
                {
                    if (leftTileVo.layerIndex > tileVo.layerIndex)
                    {
                        continue; //这是右上侧的墙面
                    }

                    if (leftTileVo.layerIndex < tileVo.layerIndex)
                        AddWallTriangles(tileVo, leftTileVo, true, vertList, vertDic, triList);
                }

                // 右侧墙面 
                if (tileVoDic.TryGetValue((tileVo.x + 1) + "_" + tileVo.z, out var rightTileVo))
                {
                    if (rightTileVo.layerIndex > tileVo.layerIndex)
                    {
                        continue; //这是左上侧的墙面
                    }

                    if (rightTileVo.layerIndex < tileVo.layerIndex)
                        AddWallTriangles(tileVo, rightTileVo, false, vertList, vertDic, triList);
                }
            }

            // 填充UV
            for (int i = 0; i < vertList.Count; i++)
            {
                uvList.Add(new Vector2(0, 0));
            }

            mesh.vertices = vertList.ToArray();
            mesh.triangles = triList.ToArray();
            mesh.uv = uvList.ToArray();
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();

            var so = new SerializedObject(mesh);
            var pro = so.FindProperty("m_IsReadable");
            pro.boolValue = false;
            so.ApplyModifiedProperties();

            var path = $"Assets/RTSMap.asset";
            AssetDatabase.CreateAsset(mesh, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            var oldGo = GameObject.Find("RTSMap");
            if (oldGo != null) Object.DestroyImmediate(oldGo);

            var go = new GameObject("RTSMap");
            var filter = go.AddComponent<MeshFilter>();
            filter.mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            var render = go.AddComponent<MeshRenderer>();
            render.sharedMaterial = new Material(Shader.Find("Standard"));
        }

        private static int GetOrCreateVertex(int tx, int tz, int layerIndex, List<Vector3> vertList,
            Dictionary<string, int> vertDic)
        {
            var key = $"{tx}_{tz}_{layerIndex:F2}";
            if (vertDic.TryGetValue(key, out var index))
                return index;

            var pos = PtToMap(tx, tz, layerIndex * LAYER_HEIGHT);
            vertList.Add(pos);
            vertDic[key] = vertList.Count - 1;
            return vertList.Count - 1;
        }

        private static void AddWallTriangles(TileVo vo1, TileVo vo2, bool isLeft,
            List<Vector3> vertList, Dictionary<string, int> vertDic, List<int> triList)
        {
            var indexGap = Mathf.Abs(vo1.layerIndex - vo2.layerIndex);
            if (indexGap < 2) return;

            var topTileVo = vo1.layerIndex > vo2.layerIndex ? vo1 : vo2;
            var bottomTileVo = vo1.layerIndex < vo2.layerIndex ? vo1 : vo2;

            int v0, v1, v2, v3;

            if (isLeft)
            {
                v0 = GetOrCreateVertex(topTileVo.x, topTileVo.z, topTileVo.layerIndex - indexGap, vertList, vertDic);
                v1 = GetOrCreateVertex(topTileVo.x - 1, topTileVo.z, topTileVo.layerIndex - indexGap, vertList,
                    vertDic);
                v2 = GetOrCreateVertex(topTileVo.x - 1, topTileVo.z, topTileVo.layerIndex, vertList, vertDic);
                v3 = GetOrCreateVertex(topTileVo.x, topTileVo.z, topTileVo.layerIndex, vertList, vertDic);
            }
            else
            {
                v0 = GetOrCreateVertex(topTileVo.x, topTileVo.z, topTileVo.layerIndex - indexGap, vertList, vertDic);
                v1 = GetOrCreateVertex(topTileVo.x, topTileVo.z, topTileVo.layerIndex, vertList, vertDic);
                v2 = GetOrCreateVertex(topTileVo.x, topTileVo.z - 1, topTileVo.layerIndex, vertList, vertDic);
                v3 = GetOrCreateVertex(topTileVo.x, topTileVo.z - 1, topTileVo.layerIndex - indexGap, vertList,
                    vertDic);
            }

            // 法线朝外：左墙面法线朝-x，右墙面法线朝+z
            if (isLeft)
            {
                // 从-x看：v0(后上) -> v1(前上) -> v2(前下) -> v3(后下)，逆时针法线朝左
                triList.Add(v0);
                triList.Add(v1);
                triList.Add(v2);
                triList.Add(v0);
                triList.Add(v2);
                triList.Add(v3);
            }
            else
            {
                // 从+z看：v0(前下) -> v1(前上) -> v2(后上) -> v3(后下)，逆时针法线朝前
                triList.Add(v0);
                triList.Add(v1);
                triList.Add(v2);
                triList.Add(v0);
                triList.Add(v2);
                triList.Add(v3);
            }
        }


        private static readonly float HalfTileW = 0.5f;
        private static readonly float HalfTileH = 0.5f;

        private static Vector3 PtToMap(float tx, float tz, float y)
        {
            float x = (tx - tz) * HalfTileW;
            float z = (tx + tz) * HalfTileH;
            return new Vector3(x, y, -z);
        }

        private static int GetDrawLayerIndex(int tx, int tz, int curIndex)
        {
            var maxIndex = GetMaxLayerIndex(tx, tz);
            if (maxIndex - curIndex > 1)
            {
                return curIndex;
            }

            return maxIndex;
        }

        //检查覆盖指定顶点的所有格子，返回最高的层级
        private static int GetMaxLayerIndex(int tx, int tz)
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