using System;
using System.ComponentModel;
using Akka.Actor;
using Akka.Dispatch.SysMsg;
using Akka.Persistence;
using ConsumerDomainEvents;
using KioskDirectedMessages.Consumer;
using Newtonsoft.Json.Linq;

namespace EventStoreClientTests
{
    class ConsumerAggregateData
    {
        public ConsumerAggregateData(UpdateConsumer consumer, AdjustBalance balance, AddFingerImageToConsumer finger, RemoveFingerImageFromConsumer removeFinger)
        {
            UpdateConsumerCommand = consumer;
            AdjustBalanceCommand = balance;
            AddFingerImageToConsumerCommand = finger;
            RemoveFingerImageToConsumerCommand = removeFinger;
        }
        public UpdateConsumer UpdateConsumerCommand { get; }
        public AdjustBalance AdjustBalanceCommand { get; }
        public AddFingerImageToConsumer AddFingerImageToConsumerCommand { get; }
        public RemoveFingerImageFromConsumer RemoveFingerImageToConsumerCommand { get; }
        //private UpdateBalance _updateBalance; // TODO: Probably not necessary for distributor if use is mainly to override kiosk balance
    }

    class ConsumerAggregateActor : ReceivePersistentActor
    {
        public override string PersistenceId { get; }
        private bool _isPaused;
        private int _msgsSinceLastSnapshot = 0;

        private ConsumerAggregateData _consumerAggregateData;


        public ConsumerAggregateActor(string streamId)
        {
            this.PersistenceId = streamId;
            _isPaused = true;

            var idParts = streamId.Split('_');
            var employeeId = int.Parse(idParts[0]);
            var uniqueId = Guid.Parse(idParts[1]);

            _consumerAggregateData = new ConsumerAggregateData(
                ConsumerUpdateExtensions.CreateDefaultUpdateConsumer(employeeId, uniqueId),
                new AdjustBalance(streamId, 0M, 0M, Guid.Empty),
                new AddFingerImageToConsumer(streamId, null),
                new RemoveFingerImageFromConsumer(streamId, null));

            Recover<UpdateConsumer>(e => _consumerAggregateData = new ConsumerAggregateData(e, _consumerAggregateData.AdjustBalanceCommand, _consumerAggregateData.AddFingerImageToConsumerCommand, _consumerAggregateData.RemoveFingerImageToConsumerCommand));
            Recover<AdjustBalance>(e => _consumerAggregateData = new ConsumerAggregateData(_consumerAggregateData.UpdateConsumerCommand, e, _consumerAggregateData.AddFingerImageToConsumerCommand, _consumerAggregateData.RemoveFingerImageToConsumerCommand));
            Recover<AddFingerImageToConsumer>(e => _consumerAggregateData = new ConsumerAggregateData(_consumerAggregateData.UpdateConsumerCommand, _consumerAggregateData.AdjustBalanceCommand, e, _consumerAggregateData.RemoveFingerImageToConsumerCommand));
            Recover<RemoveFingerImageFromConsumer>(e => _consumerAggregateData = new ConsumerAggregateData(_consumerAggregateData.UpdateConsumerCommand, _consumerAggregateData.AdjustBalanceCommand, _consumerAggregateData.AddFingerImageToConsumerCommand, e));
            Recover<SnapshotOffer>(offer =>
            {
                var d = offer.Snapshot as ConsumerAggregateData;
                if (d != null)
                    _consumerAggregateData = d;
            });
            Command<SaveSnapshotSuccess>(success =>
            {
                //DeleteMessages(success.Metadata.SequenceNr);
            });
            Command<SaveSnapshotFailure>(failure =>
            {
                // handle snapshot save failure...
                Console.WriteLine(failure.ToString());
            });

            Command<ConsumerDomainEvents.AddedToGroup>(e =>
            {
                // TODO: No reason to do anything with group yet for consumer aggregate
                //_consumer.ApplyEvent(e);
            });

            Command<ConsumerDomainEvents.RemovedFromGroup>(e =>
            {
                // TODO: No reason to do anything with group yet for consumer aggregate
                //_consumer.ApplyEvent(e);
            });

            Command<ConsumerDomainEvents.Credited>(c =>
            {
                //Console.WriteLine(_consumerAggregateData.AdjustBalanceCommand.CurrentAmount);

                var e = new AdjustBalance(
                     _consumerAggregateData.AdjustBalanceCommand.EmployeeId,
                    c.Amount,
                    _consumerAggregateData.AdjustBalanceCommand.CurrentAmount + c.Amount,
                    c.CashinUniqueId);

                Persist(e, newEvent =>
                {
                    _consumerAggregateData = new ConsumerAggregateData(_consumerAggregateData.UpdateConsumerCommand, newEvent, _consumerAggregateData.AddFingerImageToConsumerCommand, _consumerAggregateData.RemoveFingerImageToConsumerCommand);
                    SnapshotCheck();
                    //IssueCommand(_updateConsumerCommand);
                });
            });

            Command<ConsumerDomainEvents.Debited>(c =>
            {
                var e = new AdjustBalance(
                    _consumerAggregateData.AdjustBalanceCommand.EmployeeId,
                    c.Amount,
                    _consumerAggregateData.AdjustBalanceCommand.CurrentAmount - c.Amount,
                    c.SaleUniqueId);

                Persist(e, newEvent =>
                {
                    _consumerAggregateData = new ConsumerAggregateData(_consumerAggregateData.UpdateConsumerCommand, newEvent, _consumerAggregateData.AddFingerImageToConsumerCommand, _consumerAggregateData.RemoveFingerImageToConsumerCommand);
                    SnapshotCheck();
                    //IssueCommand(_updateConsumerCommand);
                });
            });

            Command<ConsumerDomainEvents.FingerImgSet>(c =>
            {
                var e = new AddFingerImageToConsumer(
                    _consumerAggregateData.AddFingerImageToConsumerCommand.EmployeeId,
                    c.FingerImg);
                Persist(e, newEvent =>
                {
                    _consumerAggregateData = new ConsumerAggregateData(_consumerAggregateData.UpdateConsumerCommand, _consumerAggregateData.AdjustBalanceCommand, newEvent, _consumerAggregateData.RemoveFingerImageToConsumerCommand);
                    SnapshotCheck();
                    //IssueCommand(_updateConsumerCommand);
                });
            });

            Command<ConsumerDomainEvents.FingerImgRemoved>(c =>
            {
                var e = new RemoveFingerImageFromConsumer(
                    _consumerAggregateData.RemoveFingerImageToConsumerCommand.EmployeeId,
                    null /*HACK: Not used at kiosk*/);
                Persist(e, newEvent =>
                {
                    _consumerAggregateData = new ConsumerAggregateData(_consumerAggregateData.UpdateConsumerCommand, _consumerAggregateData.AdjustBalanceCommand, _consumerAggregateData.AddFingerImageToConsumerCommand, newEvent);
                    SnapshotCheck();
                    //IssueCommand(_updateConsumerCommand);
                });
            });

            Command<ResumeCommand>(c =>
            {
                // Unpause the aggregate and forward any accumulated commands

                _isPaused = false;//TODO:  Should this use become/unbecome?

                //IssueCommand(this._updateConsumerCommand);
                //IssueCommand(this._adjustBalanceCommand);
                //IssueCommand(this._addFingerImageToConsumerCommand);
                //IssueCommand(this._removeFingerImageToConsumerCommand);
            });

            CommandAny(c =>
            {
                // OPTIMIZE: Run this only if is subset of events causing UpdateConsumer to change
                var e = _consumerAggregateData.UpdateConsumerCommand.ToUpdateConsumerCommand(c);
                Persist(e, newEvent =>
                {
                    _consumerAggregateData = new ConsumerAggregateData(newEvent, _consumerAggregateData.AdjustBalanceCommand, _consumerAggregateData.AddFingerImageToConsumerCommand, _consumerAggregateData.RemoveFingerImageToConsumerCommand);
                    SnapshotCheck();
                    //IssueCommand(_updateConsumerCommand);
                });
            });
        }

        private void SnapshotCheck()
        {
            if (++_msgsSinceLastSnapshot % 100 == 0)
            {
                SaveSnapshot(_consumerAggregateData);
            }
        }

        private void IssueCommand(object ouboundCommand)
        {
            if (!_isPaused)
            {
                Console.WriteLine(JObject.FromObject(ouboundCommand).ToString(Newtonsoft.Json.Formatting.None));
            }
        }

    }

    public static class ConsumerUpdateExtensions
    {
        public static UpdateConsumer ToUpdateConsumerCommand(this UpdateConsumer originalAggregate, object domainEvent)
        {
            return new UpdateConsumer(
                legacyEmployeeId: originalAggregate.LegacyEmployeeId,
                uniqueId: originalAggregate.UniqueId,
                userName: domainEvent is UserNameSet ? ((UserNameSet)domainEvent).UserName : originalAggregate.UserName,
                //scannerId: GetApplicableValue(domainEvent, (ScannerIdSet e) => e.ScannerId, originalAggregate.ScannerId),
                scannerId: domainEvent is ScannerIdSet ? ((ScannerIdSet)domainEvent).ScannerId : originalAggregate.ScannerId,
                firstName: domainEvent is FirstNameSet ? ((FirstNameSet)domainEvent).FirstName : originalAggregate.FirstName,
                lastName: domainEvent is LastNameSet ? ((LastNameSet)domainEvent).LastName : originalAggregate.LastName,
                eMailAddress: domainEvent is EMailAddressSet ? ((EMailAddressSet)domainEvent).EMailAddress : originalAggregate.EMailAddress,
                pin: domainEvent is PinSet ? ((PinSet)domainEvent).Pin : originalAggregate.Pin,
                isActive: domainEvent is IsActiveSet ? ((IsActiveSet)domainEvent).IsActive : originalAggregate.IsActive,
                mobilePhone: domainEvent is MobilePhoneSet ? ((MobilePhoneSet)domainEvent).MobilePhone : originalAggregate.MobilePhone,
                clientAccount: domainEvent is ClientAccountSet ? ((ClientAccountSet)domainEvent).ClientAccount : originalAggregate.ClientAccount,
                cardId:
                    domainEvent is CardAdded ? ((CardAdded)domainEvent).CardId :
                    domainEvent is CardRemoved && string.Equals(((CardRemoved)domainEvent).CardId, originalAggregate.CardId) ? string.Empty :
                    originalAggregate.ClientAccount
                );
        }

        public static TValue GetApplicableValue<TEvent, TValue>(object domainEvent, Func<TEvent, TValue> getNewValue, Func<TValue> getOriginalValue)
        {
            return domainEvent is TEvent ? getNewValue((TEvent)domainEvent) : getOriginalValue();
        }

        public static TValue GetApplicableValue<TEvent, TValue>(object domainEvent, Func<TEvent, TValue> getNewValue, TValue originalValue)
        {
            return domainEvent is TEvent ? getNewValue((TEvent)domainEvent) : originalValue;
        }

        public static UpdateConsumer CreateDefaultUpdateConsumer(int legacyEmployeeId, Guid uniqueId)
        {
            return new UpdateConsumer(
                legacyEmployeeId: legacyEmployeeId,
                userName: null,
                scannerId: null,
                firstName: null,
                lastName: null,
                eMailAddress: null,
                pin: null,
                isActive: false,
                mobilePhone: null,
                clientAccount: null,
                uniqueId: uniqueId,
                cardId: null);

        }
    }
}