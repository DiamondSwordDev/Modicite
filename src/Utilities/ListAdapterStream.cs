using System;
using System.IO;
using System.Collections.Generic;

namespace Modicite.Utilities {

    class ListAdapterStream : Stream {

        private List<byte> list = null;

        public ListAdapterStream(List<byte> list) : base() {
            this.list = list;
        }


        public override bool CanRead {
            get {
                return false;
            }
        }

        public override bool CanSeek {
            get {
                return false;
            }
        }

        public override bool CanWrite {
            get {
                return list != null;
            }
        }

        public override long Length {
            get {
                return list.Count;
            }
        }

        public override long Position {
            get {
                throw new InvalidOperationException("ListAdapterStream does not implement Position.");
            }

            set {
                throw new InvalidOperationException("ListAdapterStream does not implement Position.");
            }
        }

        public override void Flush() {
            return;
        }

        public override int Read(byte[] buffer, int offset, int count) {
            throw new InvalidOperationException("ListAdapterStream does not implement Read(byte[], int, int).");
        }

        public override long Seek(long offset, SeekOrigin origin) {
            throw new InvalidOperationException("ListAdapterStream does not implement Seek(long, SeekOrigin).");
        }

        public override void SetLength(long value) {
            throw new InvalidOperationException("ListAdapterStream does not implement SetLength().");
        }

        public override void Write(byte[] buffer, int offset, int count) {
            throw new InvalidOperationException("ListAdapterStream does not implement Write(byte[], int, int).  Use WriteByte(byte) instead.");
        }

        public override void WriteByte(byte value) {
            list.Add(value);
        }
    }
}
