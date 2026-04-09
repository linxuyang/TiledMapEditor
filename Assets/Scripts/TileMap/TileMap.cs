using System;
using System.Collections.Generic;
using System.IO;
using LitJson;
using UnityEngine;

namespace TileMap
{
    public class TileMap : MonoBehaviour
    {
        private Dictionary<int, string> prefabDic = new Dictionary<int, string>();
        private Dictionary<int, string> gidResDic = new Dictionary<int, string>();
        private Dictionary<int, int> gidIndexDic = new Dictionary<int, int>();
        private Dictionary<int, Vector4> tilingOffsetDic = new Dictionary<int, Vector4>();
        private Dictionary<string, string> areaNameDic;
        private Dictionary<string, string> atlasNameDic;
        private Dictionary<string, string> tilesetNameDic;


        private void Awake()
        {
            var cam = Camera.main;
            transform.up = -cam.transform.forward;
            transform.position -= transform.up * 100;
            var cameraAngle = cam.transform.localEulerAngles.x;
            var zScale = 1 / Mathf.Cos((90 - cameraAngle) * Mathf.Deg2Rad);
            Debug.Log(zScale);
        }

        public void SetData(TextAsset mapData)
        {
            prefabDic.Clear();
            gidResDic.Clear();
            gidIndexDic.Clear();

            JsonData json = JsonMapper.ToObject(mapData.text);
            int width = (int)json["width"];
            int height = (int)json["height"];
            var layers = json["layers"];
            var tileSets = json["tilesets"];
            ParseTileSet(tileSets);
            ParseLayers(layers);
        }

        private void ParseTileSet(JsonData sets)
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

        private int layerIndex = 0;

        private void ParseLayers(JsonData layers)
        {
            for (int i = layers.Count - 1; i >= 0; i--)
            {
                var item = layers[i];
                var type = (string)item["type"];
                if (type == "tilelayer")
                {
                    var layerGo = new TileMapLayer(layerIndex);
                    layerGo.Reset(this, item);
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

        public void OnAddChunk(int x, int y, int w, int h)
        {
            //更新地图边界
        }

        public string GidToResName(int gid)
        {
            return "";
        }

        public GameObject GetObjIns(int gid, Transform parent)
        {
            return new GameObject();
        }

        public GameObject GetTileIns(int gid, Transform parent)
        {
            // if (pre)
            // {
            //     
            // }
            return null;
        }

        public Vector4 GetTilingOffset(int gid)
        {
            return new Vector4(1, 1, 0, 0);
        }
    }
}