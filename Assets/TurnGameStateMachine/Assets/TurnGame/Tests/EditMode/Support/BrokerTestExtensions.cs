using System;
using System.Reflection;
using System.Runtime.ExceptionServices;
using TurnGame.Core;

namespace TurnGame.Tests.Support
{
    public static class BrokerTestExtensions
    {
        /// <summary>
        /// Publishes a default instance of an event given its runtime type. Lets [TestCase] tables
        /// refer to events by typeof(...). Exceptions thrown by handlers are rethrown unwrapped.
        /// </summary>
        public static void PublishEvent(this IMessageBroker broker, Type eventType)
        {
            var method = typeof(IMessageBroker).GetMethod("Publish").MakeGenericMethod(eventType);
            try
            {
                method.Invoke(broker, new[] { Activator.CreateInstance(eventType) });
            }
            catch (TargetInvocationException ex)
            {
                ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
            }
        }
    }
}
