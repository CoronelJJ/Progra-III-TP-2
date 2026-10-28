using System;
using System.Collections.Generic;

namespace TurnGame.Core
{
    /// <summary>
    /// Synchronous, typed message broker. Safe against handlers subscribing/unsubscribing
    /// while a message is being published (the handler list is snapshotted per publish,
    /// and handlers removed mid-publish are skipped).
    /// </summary>
    public class MessageBroker : IMessageBroker
    {
        private readonly Dictionary<Type, List<Delegate>> _handlers = new Dictionary<Type, List<Delegate>>();

        public virtual void Publish<T>(T message)
        {
            if (!_handlers.TryGetValue(typeof(T), out var list) || list.Count == 0)
                return;

            var snapshot = list.ToArray();
            for (int i = 0; i < snapshot.Length; i++)
            {
                if (!list.Contains(snapshot[i]))
                    continue; // unsubscribed by a previous handler during this publish

                ((Action<T>)snapshot[i]).Invoke(message);
            }
        }

        public void Subscribe<T>(Action<T> handler)
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));

            if (!_handlers.TryGetValue(typeof(T), out var list))
            {
                list = new List<Delegate>();
                _handlers[typeof(T)] = list;
            }
            list.Add(handler);
        }

        public void Unsubscribe<T>(Action<T> handler)
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));

            if (_handlers.TryGetValue(typeof(T), out var list))
                list.Remove(handler);
        }

        /// <summary>Number of handlers currently subscribed to T (useful for leak tests).</summary>
        public int SubscriberCount<T>()
        {
            return _handlers.TryGetValue(typeof(T), out var list) ? list.Count : 0;
        }

        /// <summary>Total number of handlers subscribed to anything (useful for leak tests).</summary>
        public int TotalSubscriberCount
        {
            get
            {
                int total = 0;
                foreach (var list in _handlers.Values) total += list.Count;
                return total;
            }
        }

        public void Clear()
        {
            _handlers.Clear();
        }
    }
}
