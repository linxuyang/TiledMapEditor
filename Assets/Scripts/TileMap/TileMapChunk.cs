using System.Collections.Generic;
using LitJson;
using UnityEngine;

namespace TileMap
{
    public class TileMapChunk
    {
        private static CullingGroup _cullingGroup;
        private static int chunkCount = 0;
        private static BoundingSphere[] _boundingSpheres = new BoundingSphere[50000];
        private static List<TileMapChunk> _allChunk = new List<TileMapChunk>();

        public static void SetupCullGroup()
        {
            if (_cullingGroup != null)
            {
                _cullingGroup.Dispose();
            }

            _cullingGroup = new CullingGroup();
            _cullingGroup.targetCamera = Camera.main;
            _cullingGroup.onStateChanged = CullingStateChanged;
            chunkCount = _allChunk.Count;
            _cullingGroup.SetBoundingSpheres(_boundingSpheres);
            _cullingGroup.SetBoundingSphereCount(chunkCount);
        }

        private static void CullingStateChanged(CullingGroupEvent evt)
        {
            if (evt.hasBecomeVisible)
            {
                _allChunk[evt.index].visible = true;
                _allChunk[evt.index].Show();
            }

            if (evt.hasBecomeInvisible)
            {
                _allChunk[evt.index].visible = false;
            }
        }

        private static void Dispose()
        {
            if (_cullingGroup != null)
            {
                _cullingGroup.onStateChanged -= CullingStateChanged;
                _cullingGroup.Dispose();
                _cullingGroup = null;
                _boundingSpheres = new BoundingSphere[50000];
            }

            _allChunk.Clear();
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
        private TileMapLayer layer;
        private Vector3 chunkPos;
        private GameObject ins;
        private int x = 0;
        private int y = 0;
        private int w = 16;
        private int h = 16;
        private Vector3 chunkCenter;

        public void Reset(TileMap map, TileMapLayer layer, JsonData chunkVo)
        {
            this.map = map;
            this.layer = layer;
            x = (int)chunkVo["x"];
            y = (int)chunkVo["y"];
            w = (int)chunkVo["w"];
            h = (int)chunkVo["h"];
            vo = chunkVo;
            chunkPos = PtUtil.PtToMap(x, y);
            _allChunk.Add(this);
            var index = _allChunk.Count - 1;
            chunkCenter = map.transform.TransformPoint(chunkPos + new Vector3(0, 0, -PtUtil.TileH * 8));
            _boundingSpheres[index].position = chunkCenter;
            _boundingSpheres[index].radius = PtUtil.TileW * 8;
            map.OnAddChunk(x, y, w, h);
        }

        private void Show()
        {
            if (map.gameObject.activeSelf == false)
            {
                return;
            }

            if (inited)
            {
                return;
            }

            inited = true;
            ins = new GameObject("Chunk" + x + "_" + y);
            ins.transform.SetParent(layer.transform);
            ins.transform.localPosition = chunkPos;
            ins.transform.localRotation = Quaternion.identity;
            ins.transform.localScale = Vector3.one;
            bool H = false;
            bool V = false;
            bool D = false;
            for (int i = vo.Count - 1; i >= 0; i--)
            {
                uint rawGid = (uint)vo[i];
                if (rawGid == 0)
                {
                    continue;
                }

                int gid = (int)rawGid;
                H = false;
                V = false;
                D = false;
                if (rawGid > 10000)
                {
                    // TileGidU
                }

                var ins = map.GetTileIns(gid, this.ins.transform);
                if (ins == null)
                {
                    continue;
                }

                var col = i % w;
                var row = i / w;
                var tileKey = (x + col) + "_" + (y + row);
                var pos = PtUtil.PtToMap(col, row);
                ins.name = tileKey;
                var st = map.GetTilingOffset(gid);
                var mr = ins.GetComponent<MeshRenderer>();
                var pb = new MaterialPropertyBlock();
                mr.GetPropertyBlock(pb);
                pb.SetVector("_MainTex_ST", st);
                mr.SetPropertyBlock(pb);
            }
        }
    }
}