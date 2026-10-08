namespace _Project.Core.StateMachine
{
    public interface IGameLauncher
    {
        void StartGame(bool instant = false);
        void GoToMenu();
        void GoToBuilding();
    }
}
