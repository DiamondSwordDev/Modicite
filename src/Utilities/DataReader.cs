using System;
using System.IO;
using System.Text;
using System.Collections.Generic;

namespace Modicite.Utilities {

    class DataReader {

        private Stream stream;
        private long bytesBuffered = 0;
        private long maxBuffer = 0;
        public bool IsLittleEndian = true;


        private DataReader(Stream stream, long maxBuffer) {
            this.stream = stream;
            this.maxBuffer = maxBuffer;
        }

        public static DataReader OpenFile(string filename, long maxBuffer) {
            FileStream stream = new FileStream(filename, FileMode.Open);
            stream.Position = 0;
            stream.Flush(); //Is this necessary?
            return new DataReader(stream, maxBuffer);
        }

        public static DataReader FromBytes(byte[] data) {
            MemoryStream stream = new MemoryStream(data, false);
            stream.Position = 0;
            stream.Flush(); //Is this necessary?
            return new DataReader(stream, Int32.MaxValue);
        }


        public byte ReadByte() {
            if (bytesBuffered + 1 > maxBuffer) {
                stream.Flush();
                bytesBuffered = 0;
            }

            byte readByte = (byte)stream.ReadByte();

            bytesBuffered++;

            return readByte;
        }

        public byte[] ReadBytes(int count) {
            List<byte> bytes = new List<byte>();

            for (int i = 0; i < count; i++) {
                bytes.Add(ReadByte());
            }
            
            return bytes.ToArray();
        }

        private byte[] ReadEndianBytes(int count) {
            byte[] bytes = ReadBytes(count);

            if (BitConverter.IsLittleEndian != IsLittleEndian) {
                Array.Reverse(bytes);
            }
            
            return bytes;
        }

        public short ReadInt16() {
            return BitConverter.ToInt16(ReadEndianBytes(2), 0);
        }

        public int ReadInt32() {
            return BitConverter.ToInt32(ReadEndianBytes(4), 0);
        }

        public long ReadInt64() {
            return BitConverter.ToInt64(ReadEndianBytes(8), 0);
        }

        public bool ReadBoolean() {
            return BitConverter.ToBoolean(new byte[] { ReadByte() }, 0);
        }

        public string ReadString() {
            List<byte> bytes = new List<byte>();
            byte b = 0;
            while ((b = ReadByte()) != 0) {
                bytes.Add(b);
            }
            return Encoding.UTF8.GetString(bytes.ToArray());
        }

        public byte[] ReadRemainingBytes() {
            if (stream.Length - stream.Position == 0) {
                return new byte[0];
            } else {
                return ReadBytes((int)(stream.Length - stream.Position));
            }
        }


        public void Jump(int count) {
            if (stream.Position + count >= stream.Length) {
                throw new InvalidOperationException("Cannot jump furthur than the total length of the stream");
            }
            stream.Position += count;
        }

        public void JumpTo(int index) {
            if (index >= stream.Length) {
                throw new InvalidOperationException("Cannot jump furthur than the total length of the stream");
            }
            stream.Position = index;
        }


        public void Close() {
            stream.Close();
            stream.Dispose();
        }
    }
}
