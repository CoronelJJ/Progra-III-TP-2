namespace TurnGame.Flow.Services
{
    /// <summary>Abstraction over Application.Quit so QuittingState can be verified in tests.</summary>
    public interface IApplicationQuitter
    {
        void Quit();
    }
}
