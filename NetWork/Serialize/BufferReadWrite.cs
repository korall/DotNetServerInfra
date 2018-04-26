using System;

namespace Utility.Serielize
{
    // big endian
    public class BufferReadWrite
    {
        public static void Write(byte[] buffer, int offset, char d)
        {
            buffer[offset] = (byte)d;
        }

        public static void Write(byte[] buffer, int offset, ushort d)
        {
            buffer[offset] = (byte)((d >> 8) & 0xFF);
            buffer[offset + 1] = (byte)(d & 0xFF);
        }

        public static void Write(byte[] buffer, int offset, short d)
        {
            Write(buffer, offset, (ushort)d);
        }

        public static void Write(byte[] buffer, int offset, uint d)
        {
            buffer[offset] = (byte)((d >> 24) & 0xFF);
            buffer[offset + 1] = (byte)((d >> 16) & 0xFF);
            buffer[offset + 2] = (byte)((d >> 8) & 0xFF);
            buffer[offset + 3] = (byte)(d & 0xFF);
        }

        public static void Write(byte[] buffer, int offset, int d)
        {
            Write(buffer, offset, (uint)d);
        }

        public static void Write(byte[] buffer, int offset, ulong d)
        {
            buffer[offset] = (byte)((d >> 56) & 0xFF);
            buffer[offset + 1] = (byte)((d >> 48) & 0xFF);
            buffer[offset + 2] = (byte)((d >> 40) & 0xFF);
            buffer[offset + 3] = (byte)((d >> 32) & 0xFF);
            buffer[offset + 4] = (byte)((d >> 24) & 0xFF);
            buffer[offset + 5] = (byte)((d >> 16) & 0xFF);
            buffer[offset + 6] = (byte)((d >> 8) & 0xFF);
            buffer[offset + 7] = (byte)(d & 0xFF);
        }

        public static void Write(byte[] buffer, int offset, long d)
        {
            Write(buffer, offset, (ulong)d);
        }

        public static void Write(byte[] buffer, int offset, float d)
        {
            var temp = BitConverter.GetBytes(d);
            Array.Copy(temp, 0, buffer, offset, temp.Length);
        }

        public static void Write(byte[] buffer, int offset, double d)
        {
            var temp = BitConverter.GetBytes(d);
            Array.Copy(temp, 0, buffer, offset, temp.Length);
        }

        public static void Read(byte[] buffer, int offset, out byte d)
        {
            d = buffer[offset];
        }

        public static void Read(byte[] buffer, int offset, out char d)
        {
            d = (char)buffer[offset];
        }

        public static void Read(byte[] buffer, int offset, out ushort d)
        {
            ushort h = buffer[offset];
            ushort l= buffer[offset + 1];
            d = (ushort)(h << 8 | l);
        }

        public static void Read(byte[] buffer, int offset, out short d)
        {
            ushort h = buffer[offset];
            ushort l = buffer[offset + 1];
            d = (short)(h << 8 | l);
        }

        public static void Read(byte[] buffer, int offset, out uint d)
        {
            uint hh = buffer[offset];
            uint hl = buffer[offset + 1];
            uint lh = buffer[offset + 2];
            uint ll = buffer[offset + 3];
            d = hh << 24 | hl << 16 | lh << 8 | ll;
        }

        public static void Read(byte[] buffer, int offset, out int d)
        {
            uint temp;
            Read(buffer, offset, out temp);
            d = (int)temp;
        }

        public static void Read(byte[] buffer, int offset, out ulong d)
        {
            ulong hhh = buffer[offset];
            ulong hhl = buffer[offset + 1];
            ulong hlh = buffer[offset + 2];
            ulong hll = buffer[offset + 3];
            ulong lhh = buffer[offset + 4];
            ulong lhl = buffer[offset + 5];
            ulong llh = buffer[offset + 6];
            ulong lll = buffer[offset + 7];

            d = (hhh << 24 | hhl << 16 | hlh << 8 | hll) << 32 | (lhh << 24 | lhl << 16 | llh << 8 | lll);
        }

        public static void Read(byte[] buffer, int offset, out long d)
        {
            ulong temp;
            Read(buffer, offset, out temp);
            d = (long)temp;
        }

        public static void Read(byte[] buffer, int offset, out float d)
        {
            d = BitConverter.ToSingle(buffer, offset);
        }

        public static void Read(byte[] buffer, int offset, out double d)
        {
            d = BitConverter.ToDouble(buffer, offset);
        }
    }
}
