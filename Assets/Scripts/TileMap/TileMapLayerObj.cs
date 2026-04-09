using LitJson;
using UnityEngine;

namespace TileMap
{
    public class TileMapLayerObj
    {
        public Transform transform;
        public int layerIndex;

        public TileMapLayerObj(int index)
        {
            layerIndex = index;
        }

        public void Reset(TileMap map, JsonData vo)
        {
            var name = vo["name"].ToString();
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

            var objects = vo["objects"];
            for (int i = 0; i < objects.Count; i++)
            {
                var objVo = objects[i];
                CreateObj(map, objVo);
            }
        }

        private void CreateObj(TileMap map, JsonData vo)
        {
            var objGo = new TileMapObj();
            objGo.Reset(map, this, vo);
        }
    }
}