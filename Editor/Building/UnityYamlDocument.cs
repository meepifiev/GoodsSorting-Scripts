namespace _Project.Editor.Building
{
    public class UnityYamlDocument
    {
        public UnityYamlDocument(int classId, long fileId, string typeName, UnityYamlNode fields)
        {
            ClassId = classId;
            FileId = fileId;
            TypeName = typeName;
            Fields = fields;
        }

        public int ClassId { get; }
        public long FileId { get; }
        public string TypeName { get; }
        public UnityYamlNode Fields { get; }
    }
}
