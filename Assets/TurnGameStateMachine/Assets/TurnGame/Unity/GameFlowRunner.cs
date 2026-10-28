using TurnGame.Core;
using TurnGame.Flow.Events;
using TurnGame.Flow.Main;
using UnityEngine;

namespace TurnGame.Unity
{
    /// <summary>
    /// Thin Unity host for the state machines. Add it to ONE GameObject in your boot scene.
    /// Other systems should reach <see cref="Broker"/> and subscribe in Awake/OnEnable:
    /// the machine starts in Start(), so every Awake has already run and no early
    /// request (BootRequested...) is lost.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public sealed class GameFlowRunner : MonoBehaviour
    {
        public static GameFlowRunner Instance { get; private set; }

        public IMessageBroker Broker { get; private set; }

        private MainMachine _main;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            Broker = new MessageBroker();
            _main = new MainMachine(Broker, new UnityApplicationQuitter());
        }

        private void Start()
        {
            if (_main != null) _main.Start();
        }

        private void Update()
        {
            if (_main != null) _main.Update(Time.deltaTime);
        }

        // Losing focus (mobile background, alt-tab) asks for a pause. It is ignored in states that are not pausable.
        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus && Broker != null)
                Broker.Publish<PauseRequested>();
        }

        private void OnDestroy()
        {
            if (Instance != this) return;
            if (_main != null) _main.Stop();
            Instance = null;
        }
    }
}
