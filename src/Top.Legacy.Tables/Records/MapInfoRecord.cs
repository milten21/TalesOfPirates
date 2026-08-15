using System.Numerics;

namespace Top.Legacy.Tables.Records
{
    public class MapInfoRecord : TableRecord
    {
        public string DisplayName;
        public bool ShowSwitch;
        public int InitX;
        public int InitY;
        public Vector3 LightDirection = new Vector3(1f, 1f, -1f);
        public Vector3 LightColor = Vector3.One;
    }
}
