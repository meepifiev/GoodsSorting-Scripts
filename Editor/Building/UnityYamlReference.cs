namespace _Project.Editor.Building
{
    public readonly struct UnityYamlReference
    {
        public static readonly UnityYamlReference Null = new UnityYamlReference(0, null, 0);

        public readonly long FileId;
        public readonly string Guid;
        public readonly int Type;

        public UnityYamlReference(long fileId, string guid, int type)
        {
            FileId = fileId;
            Guid = guid;
            Type = type;
        }

        public bool IsNull => FileId == 0;
        public bool IsExternal => string.IsNullOrEmpty(Guid) == false;
    }
}
