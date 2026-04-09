namespace TileMap
{
    public struct Pt
    {
        public int tx;
        public int tz;

        private string _key;

        public Pt(int x, int z)
        {
            this.tx = x;
            this.tz = z;
            this._key = null;
        }

        public string key
        {
            get
            {
                if (string.IsNullOrEmpty(_key))
                {
                    _key = $"{this.tx}_{this.tz}";
                }

                return _key;
            }
        }
    }
}