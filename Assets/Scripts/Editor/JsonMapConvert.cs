using System.Collections.Generic;
using System.IO;
using LitJson;
using TileMap;
using UnityEditor;
using UnityEngine;

namespace Editor
{
    public class JsonMapConvert
    {
        private static Dictionary<int, string> prefabDic = new Dictionary<int, string>();
        private static Dictionary<int, string> gidResDic = new Dictionary<int, string>();
        private static Dictionary<int, int> gidIndexDic = new Dictionary<int, int>();
        private static Dictionary<int, Vector4> tilingOffsetDic = new Dictionary<int, Vector4>();
        private static Dictionary<string, string> areaNameDic;
        private static Dictionary<string, string> atlasNameDic;
        private static Dictionary<string, string> tilesetNameDic;

        [MenuItem("Map/转换地图")]
        public static void ConvertMapData()
        {
            var arr = Selection.GetFiltered(typeof(System.Object), SelectionMode.Assets);
            TextAsset mapData = (TextAsset)arr[0];
            JsonData json = JsonMapper.ToObject(mapData.text);
            int width = (int)json["width"];
            int height = (int)json["height"];
            var tileSets = json["tilesets"];
            var layers = json["layers"];
            ParseTileSet(tileSets);
            ParseLayers(layers);
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

        private static int layerIndex = 0;

        private static void ParseLayers(JsonData layers)
        {
            for (int i = layers.Count - 1; i >= 0; i--)
            {
                var item = layers[i];
                var type = (string)item["type"];
                if (type == "tilelayer")
                {
                    var layerGo = new TileMapLayer(layerIndex);
                    layerGo.Reset(null, item);
                    //特殊处理各层
                    if (layerGo.name.Contains("obstacle"))
                    {
                    }
                    else if (layerGo.name.Contains("area"))
                    {
                    }
                    else if (layerGo.name.Contains("ground"))
                    {
                    }

                    layerIndex++;
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
    }
}