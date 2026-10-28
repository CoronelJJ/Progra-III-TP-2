using TurnGame.Core;
using NUnit.Framework;
using TurnGame.Flow.Config;
using TurnGame.Flow.Events;
using TurnGame.Flow.Main;
using TurnGame.Tests.Support;

namespace TurnGame.Tests.Main
{
    [TestFixture]
    public class GameSetupTests
    {
        private MainMachineHarness _h;

        [SetUp]
        public void SetUp()
        {
            _h = new MainMachineHarness();
            _h.GoTo(typeof(GameSetupState));
        }

        [Test]
        public void ModeSelected_BuildsConfig_StoresItInSession_AndRequestsSetup()
        {
            GameConfig requested = null;
            _h.Broker.Subscribe<SetupRequested>(r => requested = r.Config);

            _h.Send(new GameModeSelected(GameMode.Versus));

            Assert.IsNotNull(requested);
            Assert.AreEqual(GameMode.Versus, requested.Mode);
            Assert.AreSame(requested, _h.Machine.Session.Config);
        }

        [Test]
        public void ModeSelected_DoesNotStartTheGameUntilSetupFinishes()
        {
            _h.Send(new GameModeSelected(GameMode.SinglePlayer));

            Assert.IsInstanceOf<GameSetupState>(_h.Machine.Current);
        }

        [Test]
        public void SetupFinished_AfterModeSelected_GoesInGame()
        {
            _h.Send(new GameModeSelected(GameMode.SinglePlayer));

            _h.Send<SetupFinished>();

            Assert.IsInstanceOf<InGameState>(_h.Machine.Current);
        }

        [Test]
        public void SetupFinished_BeforeModeSelected_IsIgnored()
        {
            _h.Send<SetupFinished>();

            Assert.IsInstanceOf<GameSetupState>(_h.Machine.Current);
        }

        [Test]
        public void SecondModeSelection_IsIgnored()
        {
            _h.Send(new GameModeSelected(GameMode.SinglePlayer));
            _h.Send(new GameModeSelected(GameMode.Versus));

            Assert.AreEqual(1, _h.Broker.CountOf<SetupRequested>());
            Assert.AreEqual(GameMode.SinglePlayer, _h.Machine.Session.Config.Mode);
        }

        [Test]
        public void SynchronousSetupSystem_IsSupported()
        {
            // An instantaneous setup answers inside the SetupRequested handler.
            _h.Broker.Subscribe<SetupRequested>(_ => _h.Broker.Publish<SetupFinished>());

            _h.Send(new GameModeSelected(GameMode.SinglePlayer));

            Assert.IsInstanceOf<InGameState>(_h.Machine.Current);
        }

        [Test]
        public void SetupCancelled_ReturnsToMenu_AndClearsTheSession()
        {
            _h.Send(new GameModeSelected(GameMode.SinglePlayer));

            _h.Send<SetupCancelled>();

            Assert.IsInstanceOf<MainMenuState>(_h.Machine.Current);
            Assert.IsFalse(_h.Machine.Session.HasConfig);
        }

        [Test]
        public void SetupCancelled_BeforeSelectingMode_ReturnsToMenu()
        {
            _h.Send<SetupCancelled>();

            Assert.IsInstanceOf<MainMenuState>(_h.Machine.Current);
        }
    }
}
