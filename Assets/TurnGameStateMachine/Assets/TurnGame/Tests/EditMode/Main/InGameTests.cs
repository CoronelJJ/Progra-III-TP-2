using NUnit.Framework;
using TurnGame.Flow.Events;
using TurnGame.Flow.Main;
using TurnGame.Tests.Support;

namespace TurnGame.Tests.Main
{
    /// <summary>InGame owns the GameMachine: integration through the real MainMachine.</summary>
    [TestFixture]
    public class InGameTests
    {
        private MainMachineHarness _h;

        [SetUp]
        public void SetUp()
        {
            _h = new MainMachineHarness();
            _h.GoTo(typeof(InGameState));
        }

        [Test]
        public void Entering_StartsTheGameMachine_AtTurnStarting()
        {
            Assert.AreEqual(1, _h.Broker.CountOf<TurnStartRequested>());
        }

        [Test]
        public void Update_IsDelegatedToTheGameMachine()
        {
            _h.Send<TurnIntroFinished>();

            Assert.AreEqual(1, _h.Broker.CountOf<RollElementsRequested>());
        }

        [Test]
        public void Pause_ThenQuitToMenu_ReturnsToMainMenu()
        {
            _h.Send<PauseRequested>();

            _h.Send<QuitToMenuRequested>();

            Assert.IsInstanceOf<MainMenuState>(_h.Machine.Current);
        }

        [Test]
        public void QuitToMenu_WhilePlaying_IsIgnored()
        {
            _h.Send<QuitToMenuRequested>();

            Assert.IsInstanceOf<InGameState>(_h.Machine.Current);
        }

        [Test]
        public void QuitToMenu_FromInGameOptions_IsIgnored()
        {
            _h.Send<PauseRequested>();
            _h.Send<OptionsRequested>();

            _h.Send<QuitToMenuRequested>();

            Assert.IsInstanceOf<InGameState>(_h.Machine.Current);
        }

        [Test]
        public void GameOver_ThenDismissed_ReturnsToMainMenu()
        {
            _h.Send<GameOverReached>();
            Assert.IsInstanceOf<InGameState>(_h.Machine.Current, "still in game while the results screen is shown");

            _h.Send<GameOverDismissed>();

            Assert.AreEqual(1, _h.Broker.CountOf<GameFinished>());
            Assert.IsInstanceOf<MainMenuState>(_h.Machine.Current);
        }

        [Test]
        public void GameOver_FromPause_ThenDismissed_ReturnsToMainMenu_WithoutLeaks()
        {
            _h.Send<PauseRequested>();
            _h.Send<GameOverReached>();
            _h.Send<GameOverDismissed>();

            Assert.IsInstanceOf<MainMenuState>(_h.Machine.Current);

            _h.Machine.Stop();
            Assert.AreEqual(0, _h.Broker.TotalSubscriberCount);
        }

        [Test]
        public void LeavingInGame_ClearsTheSession()
        {
            Assert.IsTrue(_h.Machine.Session.HasConfig);

            _h.Send<GameOverReached>();
            _h.Send<GameOverDismissed>();

            Assert.IsFalse(_h.Machine.Session.HasConfig);
        }

        [Test]
        public void GameEvents_AfterLeavingInGame_AreNotHandledByAnyone()
        {
            _h.Send<PauseRequested>();
            _h.Send<QuitToMenuRequested>();
            int subscribersInMenu = _h.Broker.TotalSubscriberCount;

            _h.Send<ElementsRolled>();
            _h.Send<PauseRequested>();
            _h.Send<GameOverReached>();

            Assert.IsInstanceOf<MainMenuState>(_h.Machine.Current);
            Assert.AreEqual(subscribersInMenu, _h.Broker.TotalSubscriberCount);
        }

        [Test]
        public void PlayingAgainAfterAGame_StartsAFreshGameMachine()
        {
            _h.Send<PauseRequested>();
            _h.Send<QuitToMenuRequested>();
            int turnStartsBefore = _h.Broker.CountOf<TurnStartRequested>();

            _h.Send<PlayRequested>();
            _h.Send(new GameModeSelected(TurnGame.Flow.Config.GameMode.SinglePlayer));
            _h.Send<SetupFinished>();

            Assert.IsInstanceOf<InGameState>(_h.Machine.Current);
            Assert.AreEqual(turnStartsBefore + 1, _h.Broker.CountOf<TurnStartRequested>());
            Assert.AreEqual(1, _h.Broker.CountOf<GamePaused>(), "only the first game was paused");
        }
    }
}
