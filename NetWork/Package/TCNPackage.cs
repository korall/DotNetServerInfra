using System;
using TimeCriticalNetwork.Buffer;

namespace TimeCriticalNetwork.Package
{
    public class TCNPackageContext
    {
        public int PackageIdGens = 0;
        public int LastPakageId = 0;
    }

    public class TCNPackage
    {
        public int   IdGens = 0;
        public byte  Id = 0;
        public short Len = 0;

        public ArraySegment<byte> Data;

        public TCNPackage Clone()
        {
            var result = new TCNPackage() { IdGens = IdGens, Id = Id, Len = Len, Data = Data };
            return result;
        }

        public int GetPackageSize()
        {
            return Data.Count + 3;
        }

        public int SerializeTo(ArraySegment<byte> buffer)
        {
            int packageSize = GetPackageSize();
            if (buffer.Count < packageSize)
                return -1;

            buffer.Write(0, Id);
            buffer.Write(1, Len);

            Array.Copy(Data.Array, 0, buffer.Array, buffer.Offset + 3, Data.Count);

            return packageSize;
        }

        public void DeSerializeFrom(ArraySegment<byte> buffer)
        {
            buffer.Read(0, out Id);
            buffer.Read(1, out Len);

            Data = new ArraySegment<byte>(buffer.Array, buffer.Offset + 3, buffer.Count - 3);
        }

        public void LoadData(ArraySegment<byte> data)
        {
            Data = data;
            Id = 0;
            Len = (short)GetPackageSize();
        }

        public void LoadData(byte[] data, int offset = 0, int len = -1)
        {
            Data = new ArraySegment<byte>(data, offset, len < 0 ? data.Length : len);
            Id = 0;
            Len = (short)GetPackageSize();
        }
    }
}
