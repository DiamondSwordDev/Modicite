using System;
using System.Collections.Generic;

namespace Modicite.Unity.Serialization {

    class RTTIDatabaseMapping {

        public int NodeIndex;
        public int ClassID;
        public int VersionIndex;


        public RTTIDatabaseMapping(int NodeIndex, int ClassID, int VersionIndex) {
            this.NodeIndex = NodeIndex;
            this.ClassID = ClassID;
            this.VersionIndex = VersionIndex;
        }
    }
}
