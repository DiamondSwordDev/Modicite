using System;
using System.Collections.Generic;

namespace Modicite.Core {

    class TargetFile {

        public List<TargetFileEntry> unityDataFiles = new List<TargetFileEntry>();
    }

    class TargetFileEntry {
        public string name = null;
        public string path = null;
    }
}
