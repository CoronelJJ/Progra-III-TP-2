using System.Collections.Generic;
using TurnGame.Core;

namespace TurnGame.Tests.Support
{
    /// <summary>IStateChanger fake: records requests instead of executing them.</summary>
    public sealed class FakeStateChanger : IStateChanger
    {
        public sealed class Request
        {
            public IState Source;
            public IState Target;
        }

        public readonly List<Request> Requests = new List<Request>();

        public void ChangeState(IState source, IState target)
        {
            Requests.Add(new Request { Source = source, Target = target });
        }
    }
}
