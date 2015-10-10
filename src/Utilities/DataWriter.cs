using System;
using System.IO;
using System.Text;
using System.Collections.Generic;

namespace Modicite.Utilities {

    class DataWriter {

        private Stream stream;
        private long bytesBuffered = 0;
        private long maxBuffer = 0;
        public bool IsLittleEndian = true;


        private DataWriter(Stream stream, long maxBuffer, bool IsLittleEndian) {
            this.stream = stream;
            this.maxBuffer = maxBuffer;
            this.IsLittleEndian = IsLittleEndian;
        }

        public static DataWriter OpenFile(string filename, bool littleEndian, long maxBuffer = 1000000) {
            FileStream stream = new FileStream(filename, FileMode.OpenOrCreate, FileAccess.Write);
            stream.Position = 0; //Is this necessary?
            return new DataWriter(stream, maxBuffer, littleEndian);
        }

        public static DataWriter FromList(List<byte> list, bool littleEndian, long maxBuffer = 1000000) {
            ListAdapterStream stream = new ListAdapterStream(list);
            return new DataWriter(stream, maxBuffer, littleEndian);
        }


        public void WriteByte(byte b) {
            if (bytesBuffered >= maxBuffer) {
                stream.Flush();
                bytesBuffered = 0;
            }
            
            stream.WriteByte(b);

            bytesBuffered++;
        }

        public void WriteBytes(byte[] ba) {
            foreach (byte b in ba) {
                WriteByte(b);
            }
        }

        private void WriteEndianBytes(byte[] ba) {
            byte[] bytes = ba;

            if (BitConverter.IsLittleEndian != IsLittleEndian) {
                Array.Reverse(bytes);
            }

            WriteBytes(bytes);
        }

        public void WriteInt16(short i) {
            WriteEndianBytes(BitConverter.GetBytes(i));
        }

        public void WriteInt32(int i) {
            WriteEndianBytes(BitConverter.GetBytes(i));
        }

        public void WriteInt64(long i) {
            WriteEndianBytes(BitConverter.GetBytes(i));
        }
        
        public void WriteBoolean(bool i) {
            WriteEndianBytes(BitConverter.GetBytes(i));
        }

        public void WriteString(string s) {
            WriteBytes(Encoding.UTF8.GetBytes(s));
            WriteByte(0);
        }


        public void Jump(int count) {
            if (!(stream is ListAdapterStream) && stream.Position + count >= stream.Length) {
                throw new InvalidOperationException("Cannot jump further than the total length of the stream.");
            }
            stream.Position += count;
        }

        public void JumpTo(int index) {
            if (!(stream is ListAdapterStream) && index >= stream.Length) {
                throw new InvalidOperationException("Cannot jump further than the total length of the stream.");
            }
            stream.Position = index;
        }


        public void Close() {
            if (bytesBuffered > 0) {
                stream.Flush();
            }
            stream.Close();
            stream.Dispose();
        }
    }
}
