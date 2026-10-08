namespace _Project.Core.Progress
{
    public interface IProgressStorage
    {
        int Level { get; set; }

        void Save();
    }
}
