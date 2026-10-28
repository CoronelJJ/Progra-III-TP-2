using System;
using System.Collections.Generic;
using TurnGame.Core;

namespace TurnGame.Tests.Support
{
    /// <summary>Test double for IState: counts calls and records their order in a shared log.</summary>
    public sealed class SpyState : IState
    {
        private readonly List<string> _log;

        public string Name { get; }
        public int EnterCount { get; private set; }
        public int UpdateCount { get; private set; }
        public int ExitCount { get; private set; }
        public float LastDeltaTime { get; private set; }

        public Action OnEnter;
        public Action OnExit;

        public SpyState(string name, List<string> sharedLog = null)
        {
            Name = name;
            _log = sharedLog;
        }

        public void Enter()
        {
            EnterCount++;
            if (_log != null) _log.Add(Name + ".Enter");
            if (OnEnter != null) OnEnter();
        }

        public void Update(float deltaTime)
        {
            UpdateCount++;
            LastDeltaTime = deltaTime;
        }

        public void Exit()
        {
            ExitCount++;
            if (_log != null) _log.Add(Name + ".Exit");
            if (OnExit != null) OnExit();
        }
    }
}
