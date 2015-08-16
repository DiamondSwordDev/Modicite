using System;
using System.IO;

namespace Modicite.Utilities {

    class DataReader {

        private FileStream stream;
        private long bytesBuffered = 0;
        private long maxBuffer = 0;
        public bool IsLittleEndian = true;


        private DataReader(FileStream stream, long maxBuffer) {
            this.stream = stream;
            this.maxBuffer = maxBuffer;
        }

        public static DataReader OpenFile(string filename, long maxBuffer) {
            FileStream stream = new FileStream(filename, FileMode.Open);
            stream.Position = 0;
            stream.Flush(); //Is this necessary?
            return new DataReader(stream, maxBuffer);
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

        private byte[] ReadBytes(int count) {
            if (bytesBuffered + count > maxBuffer) {
                stream.Flush();
                bytesBuffered = 0;
            }

            byte[] bytes = new byte[count];

            if (stream.Read(bytes, (int)stream.Position, count) < count) {
                throw new InvalidDataException("Not enough bytes were able to be read from the file");
            }

            bytesBuffered += count;

            return bytes;
        }

        private byte[] ReadEndianBytes(int count) {
            if (bytesBuffered + count > maxBuffer) {
                stream.Flush();
                bytesBuffered = 0;
            }

            byte[] bytes = new byte[count];

            if (stream.Read(bytes, (int)stream.Position, count) < count) {
                throw new InvalidDataException("Not enough bytes were able to be read from the file");
            }

            if (BitConverter.IsLittleEndian != IsLittleEndian) {
                Array.Reverse(bytes);
            }

            bytesBuffered += count;

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
    }
}
