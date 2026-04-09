using System.Collections.Generic;
using LitJson;
using UnityEngine;

namespace TileMap
{
    public class TileMapLayer
    {
        public string name;
        public Transform transform;
        public int layerIndex;
        public JsonData vo;
        private List<TileMapChunk> _chunks;

        public TileMapLayer(int index)
        {
            layerIndex = index;
            _chunks = new List<TileMapChunk>();
        }

        public void Reset(TileMap map, JsonData vo)
        {
            this.vo = vo;
            name = vo["name"].ToString();
            var go = new GameObject(name);
            transform = go.transform;
            transform.SetParent(map.transform);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            if (vo.ContainsKey("offsetx") && vo.ContainsKey("offsety"))
            {
                var x = float.Parse(vo["offsetx"].ToString());
                var y = float.Parse(vo["offsety"].ToString());
                transform.localPosition = new Vector3(x / 100, 0, -y / 100);
            }

            var areaChunks = vo["chunks"];
            for (int i = 0; i < areaChunks.Count; i++)
            {
                var chunkVo = areaChunks[i];
                var chunk = new TileMapChunk();
                chunk.Reset(map, this, chunkVo);
                _chunks.Add(chunk);
            }
        }
    }
}