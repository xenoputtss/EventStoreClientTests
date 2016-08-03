using System;
using Akka.Actor;
using ConsumerDomainEvents;
using KioskDirectedMessages.Consumer;
using Newtonsoft.Json.Linq;

namespace EventStoreClientTests
{
    /// <summary>
    /// Incomming events: 
    /// * AddedToGroup
    /// * RemovedFromGroup
    /// * Debited
    /// * Credited
    /// * FingerImgSet
    /// * FingerImgRemoved
    ///
    /// * IsActiveSet
    /// * Debited
    /// * Credited
    /// * UserNameSet
    /// * CardAdded
    /// * CardRemoved
    /// * ScannerIdSet
    /// * FingerImgSet
    /// * FingerImgRemoved
    /// * FirstNameSet
    /// * LastNameSet
    /// * EMailAddressSet
    /// * PinSet
    /// * MobilePhoneSet
    /// * ClientAccountSet
    ///
    /// Outgoing messages:
    /// * TODO: Add/Remove group not important here?
    /// * AdjustBalance
    /// * AddFingerImageToConsumer
    /// * RemoveFingerImageFromConsumer
    /// * UpdateConsumers
    /// * UpdateBalance
    /// </summary>
    class ConsumerAggregateActor : ReceiveActor
    {
        private UpdateConsumer _updateConsumerCommand;
        private AdjustBalance _adjustBalanceCommand;
        private AddFingerImageToConsumer _addFingerImageToConsumerCommand;
        private RemoveFingerImageFromConsumer _removeFingerImageToConsumerCommand;
        //private UpdateBalance _updateBalance; // TODO: Probably not necessary for distributor if use is mainly to override kiosk balance

        private bool _isPaused;

        public ConsumerAggregateActor(string streamId)
        {
            _isPaused = true;

            var idParts = streamId.Split('_');
            var employeeId = int.Parse(idParts[0]);
            var uniqueId = Guid.Parse(idParts[1]);
            _updateConsumerCommand = ConsumerUpdateExtensions.CreateDefaultUpdateConsumer(employeeId, uniqueId);
            _adjustBalanceCommand = new AdjustBalance(streamId, 0M, 0M, Guid.Empty);
            _addFingerImageToConsumerCommand = new AddFingerImageToConsumer(streamId, null);
            _removeFingerImageToConsumerCommand = new RemoveFingerImageFromConsumer(streamId, null);



            Receive<ConsumerDomainEvents.AddedToGroup>(e =>
            {
                // TODO: No reason to do anything with group yet for consumer aggregate
                //_consumer.ApplyEvent(e);
            });

            Receive<ConsumerDomainEvents.RemovedFromGroup>(e =>
            {
                // TODO: No reason to do anything with group yet for consumer aggregate
                //_consumer.ApplyEvent(e);
            });

            Receive<ConsumerDomainEvents.Credited>(e =>
            {
                Console.WriteLine(_adjustBalanceCommand.CurrentAmount);
                _adjustBalanceCommand = new AdjustBalance(
                    _adjustBalanceCommand.EmployeeId,
                    e.Amount,
                    _adjustBalanceCommand.CurrentAmount + e.Amount,
                    e.CashinUniqueId);
                IssueCommand(_updateConsumerCommand);
            });

            Receive<ConsumerDomainEvents.Debited>(e =>
            {
                _adjustBalanceCommand = new AdjustBalance(
                    _adjustBalanceCommand.EmployeeId,
                    e.Amount,
                    _adjustBalanceCommand.CurrentAmount - e.Amount,
                    e.SaleUniqueId);
                IssueCommand(_updateConsumerCommand);
            });

            Receive<ConsumerDomainEvents.FingerImgSet>(e =>
            {
                _addFingerImageToConsumerCommand = new AddFingerImageToConsumer(
                    _addFingerImageToConsumerCommand.EmployeeId,
                    e.FingerImg);
                IssueCommand(_updateConsumerCommand);
            });

            Receive<ConsumerDomainEvents.FingerImgRemoved>(e =>
            {
                _removeFingerImageToConsumerCommand = new RemoveFingerImageFromConsumer(
                    _addFingerImageToConsumerCommand.EmployeeId,
                    null /*HACK: Not used at kiosk*/);
                IssueCommand(_updateConsumerCommand);
            });

            Receive<ResumeCommand>(e =>
            {
                // Unpause the aggregate and forward any accumulated commands
                _isPaused = false;
                IssueCommand(this._updateConsumerCommand);
                IssueCommand(this._adjustBalanceCommand);
                IssueCommand(this._addFingerImageToConsumerCommand);
                IssueCommand(this._removeFingerImageToConsumerCommand);
            });

            ReceiveAny(e =>
            {
                // OPTIMIZE: Run this only if is subset of events causing UpdateConsumer to change
                _updateConsumerCommand = ConsumerUpdateExtensions.ToUpdateConsumerCommand(_updateConsumerCommand, e);
                IssueCommand(_updateConsumerCommand);
            });
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