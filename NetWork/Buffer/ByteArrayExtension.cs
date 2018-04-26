using System;
using System.Text;
using Utility.Serielize;

namespace TimeCriticalNetwork.Buffer
{
    public static class ByteArrayExtension
    {
        public static ArraySegment<byte> GetSegment(this byte[] buffer)
        {
            return new ArraySegment<byte>(buffer, 0, buffer.Length);
        }

        public static ArraySegment<byte> GetSegment(this byte[] buffer, int index, int len)
        {
            return new ArraySegment<byte>(buffer, index, len);
        }

        public static byte[] ToArray(this ArraySegment<byte> bufferSeg)
        {
            var result = bufferSeg.Array;
            if (result != null)
            {
                result = new byte[bufferSeg.Count];
                Array.Copy(bufferSeg.Array, result, result.Length);
            }
            return result;
        }

        public static string ToHexString(this ArraySegment<byte> buffSeg, int dilimitLen = 8)
        {
            byte zero = (byte)'0';
            byte hexZero = (byte)'A';

            StringBuilder builder = new StringBuilder();

            int bytes = 0;
            for (int i = 0; i < buffSeg.Count; i++)
            {
                if (bytes > 0)
                {
                    if (bytes % 4 == 0)
                        builder.Append(' ');

                    if (dilimitLen > 0 && bytes % dilimitLen == 0)
                        builder.Append(" | ");
                }

                byte d = buffSeg.Array[buffSeg.Offset + i];
                byte l = (byte)(d & 0xF);
                byte h = (byte)((d >> 4) & 0xF);
                char ch1 = (char)(l > 9 ? hexZero + l - 10 : zero + l);
                char ch2 = (char)(h > 9 ? hexZero + h - 10 : zero + h);

                builder.Append(ch2);
                builder.Append(ch1);
                builder.Append(' ');
                bytes++;
            }
            return builder.ToString().TrimEnd();

            //return BitConverter.ToString(buffSeg.Array, buffSeg.Offset, buffSeg.Count);
        }

        public static void Write(this ArraySegment<byte> buffSeg, int offset, char d)
        {
            BufferReadWrite.Write(buffSeg.Array, buffSeg.Offset + offset, d);
        }

        public static void Write(this ArraySegment<byte> buffSeg, int offset, byte d)
        {
            BufferReadWrite.Write(buffSeg.Array, buffSeg.Offset + offset, (char)d);
        }

        public static void Write(this ArraySegment<byte> buffSeg, int offset, ushort d)
        {
            BufferReadWrite.Write(buffSeg.Array, buffSeg.Offset + offset, d);
        }

        public static void Write(this ArraySegment<byte> buffSeg, int offset, short d)
        {
            BufferReadWrite.Write(buffSeg.Array, buffSeg.Offset + offset, d);
        }

        public static void Write(this ArraySegment<byte> buffSeg, int offset, int d)
        {
            BufferReadWrite.Write(buffSeg.Array, buffSeg.Offset + offset, d);
        }

        public static void Write(this ArraySegment<byte> buffSeg, int offset, uint d)
        {
            BufferReadWrite.Write(buffSeg.Array, buffSeg.Offset + offset, d);
        }

        public static void Write(this ArraySegment<byte> buffSeg, int offset, long d)
        {
            BufferReadWrite.Write(buffSeg.Array, buffSeg.Offset + offset, d);
        }

        public static void Write(this ArraySegment<byte> buffSeg, int offset, ulong d)
        {
            BufferReadWrite.Write(buffSeg.Array, buffSeg.Offset + offset, d);
        }

        public static void Write(this ArraySegment<byte> buffSeg, int offset, float d)
        {
            BufferReadWrite.Write(buffSeg.Array, buffSeg.Offset + offset, d);
        }

        public static void Write(this ArraySegment<byte> buffSeg, int offset, double d)
        {
            BufferReadWrite.Write(buffSeg.Array, buffSeg.Offset + offset, d);
        }

        public static void Read(this ArraySegment<byte> buffSeg, int offset, out char d)
        {
            BufferReadWrite.Read(buffSeg.Array, buffSeg.Offset + offset, out d);
        }

        public static void Read(this ArraySegment<byte> buffSeg, int offset, out byte d)
        {
            char d_;
            BufferReadWrite.Read(buffSeg.Array, buffSeg.Offset + offset, out d_);
            d = (byte)d_;
        }

        public static void Read(this ArraySegment<byte> buffSeg, int offset, out ushort d)
        {
            BufferReadWrite.Read(buffSeg.Array, buffSeg.Offset + offset, out d);
        }

        public static void Read(this ArraySegment<byte> buffSeg, int offset, out short d)
        {
            BufferReadWrite.Read(buffSeg.Array, buffSeg.Offset + offset, out d);
        }

        public static void Read(this ArraySegment<byte> buffSeg, int offset, out uint d)
        {
            BufferReadWrite.Read(buffSeg.Array, buffSeg.Offset + offset, out d);
        }

        public static void Read(this ArraySegment<byte> buffSeg, int offset, out int d)
        {
            BufferReadWrite.Read(buffSeg.Array, buffSeg.Offset + offset, out d);
        }

        public static void Read(this ArraySegment<byte> buffSeg, int offset, out ulong d)
        {
            BufferReadWrite.Read(buffSeg.Array, buffSeg.Offset + offset, out d);
        }

        public static void Read(this ArraySegment<byte> buffSeg, int offset, out long d)
        {
            BufferReadWrite.Read(buffSeg.Array, buffSeg.Offset + offset, out d);
        }

        public static void Read(this ArraySegment<byte> buffSeg, int offset, out float d)
        {
            BufferReadWrite.Read(buffSeg.Array, buffSeg.Offset + offset, out d);
        }

        public static void Read(this ArraySegment<byte> buffSeg, int offset, out double d)
        {
            BufferReadWrite.Read(buffSeg.Array, buffSeg.Offset + offset, out d);
        }
    }
}
