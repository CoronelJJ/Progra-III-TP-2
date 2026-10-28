using System;
using TurnGame.Core;
using TurnGame.Flow.Events;
using TurnGame.Flow.Game;

namespace TurnGame.Tests.Support
{
    /// <summary>
    /// Drives a GameMachine with the real broker. "Send" = publish + one tick, which is exactly
    /// how the machine is used in the game (events arrive, the next Update applies the transition).
    /// </summary>
    internal sealed class GameMachineHarness
    {
        public readonly RecordingBroker Broker = new RecordingBroker();
        public readonly GameMachine Machine;

        public static readonly Type[] FlowStates =
        {
            typeof(TurnStartingState), typeof(RollingElementsState), typeof(BuildingPieceState),
            typeof(PlacingPieceState), typeof(UpdatingBoardState), typeof(CheckingBoardState),
            typeof(ResolvingPlayState), typeof(PlayingFeedbackState), typeof(TurnEndingState)
        };

        // Order of the happy path (one detected play). Index i is reached after executing steps 0..i-1.
        private static readonly Type[] HappyPath = FlowStates;

        public GameMachineHarness(bool start = true)
        {
            Machine = new GameMachine(Broker);
            if (start) Machine.Start();
        }

        public IState Current { get { return Machine.Current; } }
        public IState Suspended { get { return Machine.Suspended; } }

        public void Tick(float deltaTime = 0.016f) { Machine.Update(deltaTime); }

        public void Send<T>(T message) { Broker.Publish(message); Tick(); }

        public void Send<T>() where T : struct { Send(default(T)); }

        public void Send(Type eventType) { Broker.PublishEvent(eventType); Tick(); }

        /// <summary>Publishes without ticking (to build same-tick scenarios).</summary>
        public void Queue<T>() where T : struct { Broker.Publish(default(T)); }

        /// <summary>Moves a FRESH machine (at TurnStarting) to the requested state using only events.</summary>
        public void GoTo(Type target)
        {
            if (target == typeof(PausedState))
            {
                GoTo(typeof(RollingElementsState));
                Send<PauseRequested>();
                return;
            }
            if (target == typeof(InGameOptionsState))
            {
                GoTo(typeof(PausedState));
                Send<OptionsRequested>();
                return;
            }
            if (target == typeof(GameOverScreenState))
            {
                Send<GameOverReached>();
                return;
            }

            int index = Array.IndexOf(HappyPath, target);
            if (index < 0) throw new ArgumentException("Unknown game state: " + target.Name);
            if (!(Current is TurnStartingState)) throw new InvalidOperationException("GoTo expects a fresh machine.");

            for (int i = 0; i < index; i++) Step(i);
        }

        private void Step(int i)
        {
            switch (i)
            {
                case 0: Send<TurnIntroFinished>(); break;   // -> RollingElements
                case 1: Send<ElementsRolled>(); break;      // -> BuildingPiece
                case 2: Send<PieceBuilt>(); break;          // -> PlacingPiece
                case 3: Send<PiecePlaced>(); break;         // -> UpdatingBoard
                case 4: Send<BoardUpdated>(); break;        // -> CheckingBoard
                case 5: Send<PlayDetected>(); break;        // -> ResolvingPlay
                case 6: Send<PlayResolved>(); break;        // -> PlayingFeedback
                case 7: Send<FeedbackFinished>(); Send<NoPlayDetected>(); break; // -> CheckingBoard -> TurnEnding
                default: throw new ArgumentOutOfRangeException("i");
            }
        }
    }
}
