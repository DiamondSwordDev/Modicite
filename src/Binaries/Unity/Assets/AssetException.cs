using System;
using System.Collections.Generic;

namespace Modicite.Binaries.Unity.Assets {

    class AssetException : Exception {

        public AssetException() {

        }

        public AssetException(string message) : base(message) {

        }
    }
}
