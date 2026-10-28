using System;
using System.Collections.Generic;
using NUnit.Framework;
using TurnGame.Flow.Events;
using TurnGame.Flow.Main;
using TurnGame.Tests.Support;

namespace TurnGame.Tests.Main
{
    /// <summary>The transition table of the MainMachine, as executable specification.</summary>
    [TestFixture]
    public class MainMachineTransitionTests
    {
        private MainMachineHarness _h;

        [SetUp]
        public void SetUp()
        {
            _h = new MainMachineHarness();
        }

        [Test]
        public void StartsInBooting()
        {
            Assert.IsInstanceOf<BootingState>(_h.Machine.Current);
        }

        // from-state, event, expected state
        [TestCase(typeof(BootingState), typeof(BootCompleted), typeof(MainMenuState))]
        [TestCase(typeof(MainMenuState), typeof(PlayRequested), typeof(GameSetupState))]
        [TestCase(typeof(MainMenuState), typeof(OptionsRequested), typeof(MenuOptionsState))]
        [TestCase(typeof(MainMenuState), typeof(QuitRequested), typeof(QuittingState))]
        [TestCase(typeof(MenuOptionsState), typeof(OptionsClosed), typeof(MainMenuState))]
        [TestCase(typeof(GameSetupState), typeof(SetupCancelled), typeof(MainMenuState))]
        [TestCase(typeof(InGameState), typeof(GameFinished), typeof(MainMenuState))]
        public void Event_MovesToExpectedState(Type from, Type trigger, Type expected)
        {
            _h.GoTo(from);
            Assert.IsInstanceOf(from, _h.Machine.Current, "precondition");

            _h.Send(trigger);

            Assert.IsInstanceOf(expected, _h.Machine.Current);
        }

        // events that must be ignored in a given state
        [TestCase(typeof(BootingState), typeof(PlayRequested))]
        [TestCase(typeof(BootingState), typeof(QuitRequested))]
        [TestCase(typeof(BootingState), typeof(GameFinished))]
        [TestCase(typeof(MainMenuState), typeof(BootCompleted))]
        [TestCase(typeof(MainMenuState), typeof(OptionsClosed))]
        [TestCase(typeof(MainMenuState), typeof(SetupFinished))]
        [TestCase(typeof(MainMenuState), typeof(GameFinished))]
        [TestCase(typeof(MainMenuState), typeof(PauseRequested))]
        [TestCase(typeof(MainMenuState), typeof(PiecePlaced))]
        [TestCase(typeof(MenuOptionsState), typeof(PlayRequested))]
        [TestCase(typeof(MenuOptionsState), typeof(QuitRequested))]
        [TestCase(typeof(MenuOptionsState), typeof(OptionsRequested))]
        [TestCase(typeof(GameSetupState), typeof(PlayRequested))]
        [TestCase(typeof(GameSetupState), typeof(SetupFinished))]
        [TestCase(typeof(GameSetupState), typeof(GameFinished))]
        [TestCase(typeof(GameSetupState), typeof(QuitToMenuRequested))]
        [TestCase(typeof(InGameState), typeof(PlayRequested))]
        [TestCase(typeof(InGameState), typeof(BootCompleted))]
        [TestCase(typeof(InGameState), typeof(SetupCancelled))]
        [TestCase(typeof(InGameState), typeof(QuitRequested))]
        [TestCase(typeof(InGameState), typeof(QuitToMenuRequested))] // not paused -> ignored
        [TestCase(typeof(QuittingState), typeof(BootCompleted))]
        [TestCase(typeof(QuittingState), typeof(PlayRequested))]
        public void EventOutOfContext_IsIgnored(Type from, Type trigger)
        {
            _h.GoTo(from);

            _h.Send(trigger);

            Assert.IsInstanceOf(from, _h.Machine.Current);
        }

        [Test]
        public void EntersPublishTheirScreenRequests()
        {
            Assert.AreEqual(1, _h.Broker.CountOf<BootRequested>());

            _h.Send<BootCompleted>();
            Assert.AreEqual(1, _h.Broker.CountOf<MainMenuScreenRequested>());

            _h.Send<OptionsRequested>();
            Assert.AreEqual(1, _h.Broker.CountOf<OptionsScreenRequested>());

            _h.Send<OptionsClosed>();
            Assert.AreEqual(2, _h.Broker.CountOf<MainMenuScreenRequested>());

            _h.Send<PlayRequested>();
            Assert.AreEqual(1, _h.Broker.CountOf<GameModeSelectionRequested>());
        }

        [Test]
        public void Quitting_CallsTheQuitterExactlyOnce()
        {
            _h.GoTo(typeof(QuittingState));
            _h.Tick();
            _h.Tick();

            Assert.AreEqual(1, _h.Quitter.QuitCount);
        }

        [Test]
        public void MenuOptions_RoundTripsBackToTheMenu_AsManyTimesAsNeeded()
        {
            _h.GoTo(typeof(MainMenuState));

            for (int i = 0; i < 3; i++)
            {
                _h.Send<OptionsRequested>();
                Assert.IsInstanceOf<MenuOptionsState>(_h.Machine.Current);
                _h.Send<OptionsClosed>();
                Assert.IsInstanceOf<MainMenuState>(_h.Machine.Current);
            }
        }

        // ------------------------------------------------------------ no leaks

        public static IEnumerable<Type> AllMainStates()
        {
            yield return typeof(BootingState);
            yield return typeof(MainMenuState);
            yield return typeof(MenuOptionsState);
            yield return typeof(GameSetupState);
            yield return typeof(InGameState);
            yield return typeof(QuittingState);
        }

        [TestCaseSource(nameof(AllMainStates))]
        public void Stop_LeavesNoSubscriptions(Type state)
        {
            _h.GoTo(state);

            _h.Machine.Stop();

            Assert.AreEqual(0, _h.Broker.TotalSubscriberCount);
        }

        [Test]
        public void Stop_WhileInGamePaused_LeavesNoSubscriptions()
        {
            _h.GoTo(typeof(InGameState));
            _h.Send<PauseRequested>();

            _h.Machine.Stop();

            Assert.AreEqual(0, _h.Broker.TotalSubscriberCount);
        }

        [Test]
        public void Stop_WhileInGameOptions_LeavesNoSubscriptions()
        {
            _h.GoTo(typeof(InGameState));
            _h.Send<PauseRequested>();
            _h.Send<OptionsRequested>();

            _h.Machine.Stop();

            Assert.AreEqual(0, _h.Broker.TotalSubscriberCount);
        }
    }
}
