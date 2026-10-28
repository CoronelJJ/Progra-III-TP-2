using System;

namespace TurnGame.Core
{
    /// <summary>Typed publish/subscribe bus. All non-sequential communication goes through it.</summary>
    public interface IMessageBroker
    {
        void Publish<T>(T message);
        void Subscribe<T>(Action<T> handler);
        void Unsubscribe<T>(Action<T> handler);
    }

    public static class MessageBrokerExtensions
    {
        /// <summary>Publishes an event without payload: <c>broker.Publish&lt;BootCompleted&gt;()</c>.</summary>
        public static void Publish<T>(this IMessageBroker broker) where T : struct
        {
            broker.Publish(default(T));
        }
    }
}
