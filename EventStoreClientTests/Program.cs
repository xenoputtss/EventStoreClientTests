﻿using System;
using System.Net;
using Akka.Actor;
using EventStore.ClientAPI;
using EventStore.ClientAPI.SystemData;

namespace EventStoreClientTests
{
    class Program
    {
        private static int count = 0;
        private static DateTime start;

        private static IActorRef router;
        private static ActorSystem system;

        static void Main(string[] args)
        {

            system = ActorSystem.Create("test");
            var routerProps = Props
                .Create<EventRouterActor>();
            router = system.ActorOf(routerProps, "router");

            var connectionSettings = ConnectionSettings.Create();
            connectionSettings.KeepRetrying();
            connectionSettings.KeepReconnecting();
            connectionSettings.SetDefaultUserCredentials(new UserCredentials("admin", "changeit"));

            var eventStoreEndpoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 1112);
            var Connection = EventStoreConnection.Create(connectionSettings.Build(), eventStoreEndpoint);
            Connection.ConnectAsync().Wait();
            var subscriptionSettings = new CatchUpSubscriptionSettings(200 * 4096, 4096, false, false);
            var esSubscription = Connection.SubscribeToAllFrom(
                new Position(StreamPosition.Start, StreamPosition.Start),
                subscriptionSettings,
                ConsumerEventAppeared,
                LiveProcessingStarted,
                SubscriptionDropped
                );
            start = DateTime.Now;
            Console.ReadKey();
        }

        private static void SubscriptionDropped(EventStoreCatchUpSubscription arg1, SubscriptionDropReason arg2, Exception arg3)
        {
            Console.WriteLine("SubscriptionDropped");
        }

        private static void LiveProcessingStarted(EventStoreCatchUpSubscription obj)
        {
            Console.WriteLine("LiveProcessingStarted");
            Console.WriteLine(DateTime.Now.Subtract(start));
        }


        private static void ConsumerEventAppeared(EventStoreCatchUpSubscription eventStoreCatchUpSubscription, ResolvedEvent resolvedEvent)
        {
            count++;
            if (count % 100000 == 0)
            {
                Console.WriteLine(count);
                Console.WriteLine(DateTime.Now.Subtract(start));
            }
            var re = resolvedEvent.Event;
            var stream = re.EventStreamId;
            //var streamDetails = EventStreamIdParser.Parse(stream);
            //Console.WriteLine($"{resolvedEvent.OriginalEventNumber} - ");
            //if (streamDetails != null)
            //    switch (streamDetails.Category)
            //    {
            //        case "Consumer":
            //            Console.BackgroundColor = ConsoleColor.Yellow;
            //            Console.WriteLine($"{resolvedEvent.OriginalPosition} - {streamDetails.Aggregate}");
            //            Console.BackgroundColor = ConsoleColor.Black;
            //            break;

            //        case "Sale":
            //            Console.BackgroundColor = ConsoleColor.Blue;
            //            Console.WriteLine($"{resolvedEvent.OriginalPosition} - {streamDetails.Aggregate}");
            //            Console.BackgroundColor = ConsoleColor.Black;
            //            break;
            //        default:
            //            Console.BackgroundColor = ConsoleColor.Red;
            //            Console.WriteLine($"{resolvedEvent.OriginalPosition} - {streamDetails.Aggregate}");
            //            Console.BackgroundColor = ConsoleColor.Black;
            //            break;
            //    }
        }
    }

    public class EventStreamIdParser
    {
        public static StreamDetails Parse(string eventStreamId)
        {
            var match = System.Text.RegularExpressions.Regex.Match(
              eventStreamId,
              @"^(?<category>[^-$]+)-(?<operator>[0-9A-Fa-f\-]{36})_(?<legacyid>[0-9]{1,20})_(?<aggregate>[0-9A-Fa-f\-]{36})$");

            if (!match.Success)
            {
                return null;
            }

            return new StreamDetails()
            {
                Category = match.Groups["category"].Value,
                Operator = match.Groups["operator"].Value,
                LegacyId = match.Groups["legacyid"].Value,
                Aggregate = match.Groups["aggregate"].Value,
            };
        }

        public class StreamDetails
        {
            public string Category { get; set; }
            public string Operator { get; set; }
            public string LegacyId { get; set; }
            public string Aggregate { get; set; }
        }
    }
}
