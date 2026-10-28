using TurnGame.Flow.Services;

namespace TurnGame.Tests.Support
{
    public sealed class FakeApplicationQuitter : IApplicationQuitter
    {
        public int QuitCount { get; private set; }

        public void Quit()
        {
            QuitCount++;
        }
    }
}
