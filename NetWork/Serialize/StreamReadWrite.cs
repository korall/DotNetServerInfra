
using System;
using System.IO;
using System.Text;

namespace Utility.Serielize
{
    public static class StreamReadWrite
    {
        public static void Write(this Stream stream, char d)
        {
            stream.WriteByte((byte)d);
        }

        public static void Write(this Stream stream, ushort d)
        {
            stream.WriteByte((byte)((d >> 8) & 0xFF));
            stream.WriteByte((byte)(d & 0xFF));
        }

        public static void Write(this Stream stream, short d)
        {
            Write(stream, (ushort)d);
        }

        public static void Write(this Stream stream, uint d)
        {
            stream.WriteByte((byte)((d >> 24) & 0xFF));
            stream.WriteByte((byte)((d >> 16) & 0xFF));
            stream.WriteByte((byte)((d >> 8) & 0xFF));
            stream.WriteByte((byte)(d & 0xFF));
        }

        public static void Write(this Stream stream, int d)
        {
            Write(stream, (uint)d);
        }

        public static void Write(this Stream stream, ulong d)
        {
            stream.WriteByte((byte)((d >> 56) & 0xFF));
            stream.WriteByte((byte)((d >> 48) & 0xFF));
            stream.WriteByte((byte)((d >> 40) & 0xFF));
            stream.WriteByte((byte)((d >> 32) & 0xFF));
            stream.WriteByte((byte)((d >> 24) & 0xFF));
            stream.WriteByte((byte)((d >> 16) & 0xFF));
            stream.WriteByte((byte)((d >> 8) & 0xFF));
            stream.WriteByte((byte)(d & 0xFF));
        }

        public static void Write(this Stream stream, long d)
        {
            Write(stream, (ulong)d);
        }

        public static void Write(this Stream stream, byte[] buff)
        {
            stream.Write(buff, 0, buff.Length);
        }

        public static void Write(this Stream stream, float d)
        {
            var temp = BitConverter.GetBytes(d);
            Write(stream, temp);
        }

        public static void Write(this Stream stream, double d)
        {
            var temp = BitConverter.GetBytes(d);
            Write(stream, temp);
        }

        public static void Write(this Stream stream, string d)
        {
            var temp = Encoding.UTF8.GetBytes(d);
            Write(stream, temp.Length);
            Write(stream, temp);
        }

        public static void Read(this Stream stream, out byte d)
        {
            d = (byte)stream.ReadByte();
        }

        public static void Read(this Stream stream, out char d)
        {
            d = (char)stream.ReadByte();
        }

        public static void Read(this Stream stream, out ushort d)
        {
            ushort h = (ushort)stream.ReadByte();
            ushort l = (ushort)stream.ReadByte();
            d = (ushort)(h << 8 | l);
        }

        public static void Read(this Stream stream, out short d)
        {
            ushort h = (ushort)stream.ReadByte();
            ushort l = (ushort)stream.ReadByte();
            d = (short)(h << 8 | l);
        }

        public static void Read(this Stream stream, out uint d)
        {
            uint hh = (uint)stream.ReadByte();
            uint hl = (uint)stream.ReadByte();
            uint lh = (uint)stream.ReadByte();
            uint ll = (uint)stream.ReadByte();
            d = hh << 24 | hl << 16 | lh << 8 | ll;
        }

        public static void Read(this Stream stream, out int d)
        {
            uint temp;
            Read(stream, out temp);
            d = (int)temp;
        }

        public static void Read(this Stream stream, out ulong d)
        {
            ulong hhh = (ulong)stream.ReadByte();
            ulong hhl = (ulong)stream.ReadByte();
            ulong hlh = (ulong)stream.ReadByte();
            ulong hll = (ulong)stream.ReadByte();
            ulong lhh = (ulong)stream.ReadByte();
            ulong lhl = (ulong)stream.ReadByte();
            ulong llh = (ulong)stream.ReadByte();
            ulong lll = (ulong)stream.ReadByte();

            d = (hhh << 24 | hhl << 16 | hlh << 8 | hll) << 32 | (lhh << 24 | lhl << 16 | llh << 8 | lll);
        }

        public static void Read(this Stream stream, out long d)
        {
            ulong temp;
            Read(stream, out temp);
            d = (long)temp;
        }

        public static void Read(this Stream stream, byte[] buffer)
        {
            stream.Read(buffer, 0, buffer.Length);
        }

        public static void Read(this Stream stream, out float d)
        {
            byte[] buffer = BitConverter.GetBytes(0.0f);
            Read(stream, buffer);
            d = BitConverter.ToSingle(buffer, 0);
        }

        public static void Read(this Stream stream, out double d)
        {
            byte[] buffer = BitConverter.GetBytes(0.0);
            Read(stream, buffer);
            d = BitConverter.ToDouble(buffer, 0);
        }

        public static void Read(this MemoryStream stream, out string d)
        {
            int len = 0;
            Read(stream, out len);
            if (len <= 0 || stream.Position + len > stream.Length)
            {
                d = null;
                return;
            }

            byte[] buffer = stream.GetBuffer();
            d = Encoding.UTF8.GetString(buffer, (int)stream.Position, len);
            stream.Seek(len, SeekOrigin.Current);
        }
    }
}