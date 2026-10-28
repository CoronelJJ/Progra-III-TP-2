using System;
using System.Collections.Generic;
using System.Linq;
using TurnGame.Core;

namespace TurnGame.Tests.Support
{
    /// <summary>The real MessageBroker plus a log of every published event type (in order).</summary>
    public sealed class RecordingBroker : MessageBroker
    {
        public readonly List<Type> Published = new List<Type>();

        public override void Publish<T>(T message)
        {
            Published.Add(typeof(T));
            base.Publish(message);
        }

        public int CountOf<T>()
        {
            return CountOf(typeof(T));
        }

        public int CountOf(Type eventType)
        {
            return Published.Count(t => t == eventType);
        }

        /// <summary>Index of the last publication of T in the log, or -1.</summary>
        public int LastIndexOf<T>()
        {
            return Published.LastIndexOf(typeof(T));
        }
    }
}
