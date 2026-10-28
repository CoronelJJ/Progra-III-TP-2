using System;
using TurnGame.Flow.Events;
using TurnGame.Flow.Main;

namespace TurnGame.Tests.Support
{
    /// <summary>Drives a MainMachine with the real broker and a fake IApplicationQuitter.</summary>
    public sealed class MainMachineHarness
    {
        public readonly RecordingBroker Broker = new RecordingBroker();
        public readonly FakeApplicationQuitter Quitter = new FakeApplicationQuitter();
        public readonly MainMachine Machine;

        public MainMachineHarness(bool start = true)
        {
            Machine = new MainMachine(Broker, Quitter);
            if (start) Machine.Start();
        }

        public void Tick(float deltaTime = 0.016f) { Machine.Update(deltaTime); }

        public void Send<T>(T message) { Broker.Publish(message); Tick(); }

        public void Send<T>() where T : struct { Send(default(T)); }

        public void Send(Type eventType) { Broker.PublishEvent(eventType); Tick(); }

        public bool IsIn<TState>() { return Machine.Current is TState; }

        /// <summary>Moves a FRESH machine (at Booting) to the requested state using only events.</summary>
        public void GoTo(Type target)
        {
            if (target == typeof(BootingState)) return;

            Send<BootCompleted>();
            if (target == typeof(MainMenuState)) return;

            if (target == typeof(MenuOptionsState)) { Send<OptionsRequested>(); return; }
            if (target == typeof(QuittingState)) { Send<QuitRequested>(); return; }

            Send<PlayRequested>();
            if (target == typeof(GameSetupState)) return;

            if (target == typeof(InGameState))
            {
                Send(new GameModeSelected(TurnGame.Flow.Config.GameMode.SinglePlayer));
                Send<SetupFinished>();
                return;
            }

            throw new ArgumentException("Unknown main state: " + target.Name);
        }
    }
}
