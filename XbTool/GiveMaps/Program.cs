using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace XbTool
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            Options options = CliArguments.Parse(args);
            if (options == null) return;
            Tasks.RunTask(options);
        }
    }

    public static class Stuff
    {
        public static string ReadUTF8Z(this BinaryReader reader)
        {
            var start = reader.BaseStream.Position;

            // Read until we hit the end of the stream (-1) or a zero
            while (reader.BaseStream.ReadByte() - 1 > 0) { }

            int size = (int)(reader.BaseStream.Position - start - 1);
            reader.BaseStream.Position = start;

            string text = reader.ReadUTF8(size);
            reader.BaseStream.Position++; // Skip the null byte
            return text;
        }

        public static string ReadUTF8(this BinaryReader reader, int size)
        {
            return Encoding.UTF8.GetString(reader.ReadBytes(size), 0, size);
        }

        public static void CopyStream(this Stream input, Stream output, int length)
        {
            int remaining = length;
            byte[] buffer = new byte[32768];
            int read;
            while ((read = input.Read(buffer, 0, Math.Min(buffer.Length, remaining))) > 0)
            {
                output.Write(buffer, 0, read);
                remaining -= read;
            }
        }

        public static string GetUTF8Z(byte[] value, int offset)
        {
            var length = 0;

            while (value[offset + length] != 0)
            {
                length++;
            }

            return Encoding.UTF8.GetString(value, offset, length);
        }

    }
}
