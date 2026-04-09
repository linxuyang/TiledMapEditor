using System;
using System.Collections.Generic;
using LitJson;
using UnityEngine;

namespace TileMap
{
    public class TileMapObj
    {
        private static CullingGroup _cullingGroup;
        private static int chunkCount = 0;
        private static BoundingSphere[] _boundingSpheres = new BoundingSphere[50000];
        private static List<TileMapObj> _allObj = new List<TileMapObj>();

        public static void SetupCullGroup()
        {
            if (_cullingGroup!=null)
            {
                _cullingGroup.Dispose();
            }

            _cullingGroup = new CullingGroup();
            _cullingGroup.targetCamera = Camera.main;
            _cullingGroup.onStateChanged = CullingStateChanged;
            chunkCount = _allObj.Count;
            _cullingGroup.SetBoundingSpheres(_boundingSpheres);
            _cullingGroup.SetBoundingSphereCount(chunkCount);
        }

        private static void CullingStateChanged(CullingGroupEvent evt)
        {
            if (evt.hasBecomeVisible)
            {
                _allObj[evt.index].visible = true;
                _allObj[evt.index].Show();
            }

            if (evt.hasBecomeInvisible)
            {
                _allObj[evt.index].visible = false;
            }
        }

        private static void Dispose()
        {
            if (_cullingGroup!=null)
            {
                _cullingGroup.onStateChanged -= CullingStateChanged;
                _cullingGroup.Dispose();
                _cullingGroup = null;
                _boundingSpheres = new BoundingSphere[50000];
            }
            _allObj.Clear();
        }

        public static void SetCullingActive(bool b)
        {
            if (_cullingGroup == null)
            {
                return;
            }

            _cullingGroup.enabled = b;
        }

        private bool visible;
        private bool inited;
        private JsonData vo;
        private TileMap map;
        private TileMapLayerObj layer;
        private int gid;
        private Vector3 objPos;
        private Vector3 objScale;
        private GameObject ins;

        public void Reset(TileMap map, TileMapLayerObj layer, JsonData objVo)
        {
            this.map = map;
            this.layer = layer;
            var x = float.Parse(objVo["x"].ToString());
            var y = float.Parse(objVo["y"].ToString());
            var width = float.Parse(objVo["width"].ToString());
            var height = float.Parse(objVo["height"].ToString());
            objPos = PtUtil.GetMapObjPos(x / 100, y / 100);
            objScale = new Vector3(width, 1, height);
            _allObj.Add(this);
            var index = _allObj.Count - 1;
            _boundingSpheres[index].position = map.transform.TransformPoint(objPos + new Vector3(0, 0, height / 2));
            _boundingSpheres[index].radius = Mathf.Max(width, height) / 2;
            string objRes = map.GidToResName(gid);
            // int objId = Application.GetObjIdByRes(objRes);
            // if (objId>0)
            // {
            //     map.AddBuildingPos(objId,objPos);
            // }
        }

        private void Show()
        {
            if (map.gameObject.activeSelf==false)
            {
                return;
            }

            if (inited)
            {
                return;
            }

            inited = true;
            ins = map.GetObjIns(gid, layer.transform);
            if (ins==null)
            {
                return;
            }

            ins.transform.localPosition = objPos;
            var pt = PtUtil.MapToPt(objPos);
            ins.name = $"{pt.tx}_{pt.tz}";
        }
    }
}