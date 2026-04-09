using UnityEngine;

namespace TileMap
{
    public class PtUtil
    {
        public static float zScale = 1;
        public static readonly float TileW = 2;
        public static readonly float TileH = 0.2f;
        public static readonly float HalfTileW = 2;
        public static readonly float HalfTileH = 0.6f;

        public static Pt MapToPt(Vector3 v)
        {
            var x = v.x;
            var z = -v.z;
            var tx = x / TileW + z / TileH;
            var tz = -(x / TileW - z / TileH);
            return new Pt(Mathf.FloorToInt(tx), Mathf.FloorToInt(tz));
        }

        public static Vector3 PtToMap(float tx, float tz)
        {
            float x = (tx - tz) * HalfTileW;
            float z = (tx + tz) * HalfTileH;
            return new Vector3(x, 0, -z);
        }

        public static Pt WorldToPt(Vector3 v)
        {
            v.z /= zScale;
            return MapToPt(v);
        }

        public static Vector3 PtToWorld(int tx, int tz)
        {
            var v = PtToMap(tx, tz);
            v.z *= zScale;
            return v;
        }

        public static Vector3 GetMapObjPos(float x, float y)
        {
            float wx = (x - y) * 5 / 6;
            float wy = (x + y) * 0.5f;
            return new Vector3(wx, 0, -wy);
        }
    }
}