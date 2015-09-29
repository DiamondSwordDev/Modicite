using Modicite.Utilities;

namespace Modicite.Unity {

    class UnityFileMetadata {

        public RTTIClassHierarchyDescriptor ClassHierarchyDescriptor;
        public int NumberOfObjectInfoListMembers;
        public ObjectInfo[] ObjectInfoList;
        public int NumberOfFileIdentifiers;
        public FileIdentifier[] FileIdentifiers;


        private UnityFileMetadata() {

        }

        public static UnityFileMetadata Read(DataReader reader) {
            UnityFileMetadata ufm = new UnityFileMetadata();

            ufm.ClassHierarchyDescriptor = RTTIClassHierarchyDescriptor.Read(reader);

            ufm.NumberOfObjectInfoListMembers = reader.ReadInt32();
            ufm.ObjectInfoList = new ObjectInfo[ufm.NumberOfObjectInfoListMembers];

            for (int i = 0; i < ufm.NumberOfObjectInfoListMembers; i++) {
                ufm.ObjectInfoList[i] = ObjectInfo.Read(reader);
            }

            ufm.NumberOfFileIdentifiers = reader.ReadInt32();
            ufm.FileIdentifiers = new FileIdentifier[ufm.NumberOfFileIdentifiers];

            for (int i = 0; i < ufm.NumberOfFileIdentifiers; i++) {
                ufm.FileIdentifiers[i] = FileIdentifier.Read(reader);
            }

            return ufm;
        }
    }
}
