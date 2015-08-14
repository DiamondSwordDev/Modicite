using System;
using System.Collections.Generic;

namespace Modicite.Binaries.Unity.Assets {

    class BaseClass {

        private int classID;
        private UnityHash128 scriptID;
        private UnityHash128 oldTypeHash;
        private TypeNode typeTree;

        public int classID() {
            return classID;
        }

        public void classID(int classID) {
            this.classID = classID;
        }

        public UnityHash128 scriptID() {
            return scriptID;
        }

        public void scriptID(UnityHash128 scriptID) {
            this.scriptID = scriptID;
        }

        public UnityHash128 oldTypeHash() {
            return oldTypeHash;
        }

        public void oldTypeHash(UnityHash128 oldTypeHash) {
            this.oldTypeHash = oldTypeHash;
        }

        public TypeNode typeTree() {
            return typeTree;
        }

        public void typeTree(TypeNode typeTree) {
            this.typeTree = typeTree;
        }

    }
}
