using System;
using System.Collections.Generic;
using Akka.Actor;

namespace EventStoreClientTests
{
    class EventRouterActor : ReceiveActor
    {
        //private IActorRef consumerEventRouter;
        private Guid actorId = Guid.NewGuid();
        private List<IActorRef> children = new List<IActorRef>(50000);

        public EventRouterActor()
        {
            //var consumerEventRouterProps = Props.Create(() => new ConsumerAggregateActor()).WithRouter(new RoundRobinPool(5));
            //consumerEventRouter = Context.System.ActorOf(consumerEventRouterProps, "router");

            Receive<ResumeCommand>(e =>
            {
                foreach (var child in children)
                {
                    child.Tell(e);
                }
            });

            Receive<EventStoreEvent>(e =>
            {
                var streamDetails = EventStreamIdParser.Parse(e.EventStreamId);
                //Console.WriteLine(streamDetails.Aggregate);

                if (streamDetails == null)
                {
                    // Skip this as it's likely a not an actual domain event
                    return;
                }

                if (streamDetails.Category == "Consumer")
                {
                    var actualEvent = EventDeserializer.Deserialize(e.Data, e.EventType);
                    if (actualEvent == null)
                    {
                        System.Diagnostics.Trace.TraceWarning($"Skipping {e.EventType} for Consumer aggregate because it wasn't specified as an expected deserializable type.");
                    }

                    var aggregateActorName = nameof(ConsumerAggregateActor) + "_" + streamDetails.Aggregate;
                    var child = Context.Child(aggregateActorName);

                    if (child.Equals(ActorRefs.Nobody))
                    {
                        child = Context.ActorOf(
                            Props.Create(() => new ConsumerAggregateActor(streamDetails.LegacyId + "_" + streamDetails.Aggregate)), aggregateActorName);
                        children.Add(child);
                        Console.WriteLine(children.Count + " routees for router " + actorId);
                    }

                    child.Tell(actualEvent);
                }
            });
        }
    }

}