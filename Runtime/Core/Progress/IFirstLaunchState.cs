namespace _Project.Core.Progress
{
    public interface IFirstLaunchState
    {
        bool IsFirstLaunch { get; }

        void MarkLaunched();
    }
}
