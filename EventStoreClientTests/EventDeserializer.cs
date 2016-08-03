using System.Text;
using Newtonsoft.Json;

namespace EventStoreClientTests
{
    public class EventDeserializer
    {
        public static object Deserialize(byte[] bytes, string eventTypeName)
        {
            switch (eventTypeName)
            {
                case nameof(ConsumerDomainEvents.AddedToGroup):
                    return JsonConvert.DeserializeObject<ConsumerDomainEvents.AddedToGroup>(Encoding.UTF8.GetString(bytes));
                case nameof(ConsumerDomainEvents.RemovedFromGroup):
                    return JsonConvert.DeserializeObject<ConsumerDomainEvents.RemovedFromGroup>(Encoding.UTF8.GetString(bytes));
                case nameof(ConsumerDomainEvents.Debited):
                    return JsonConvert.DeserializeObject<ConsumerDomainEvents.Debited>(Encoding.UTF8.GetString(bytes));
                case nameof(ConsumerDomainEvents.Credited):
                    return JsonConvert.DeserializeObject<ConsumerDomainEvents.Credited>(Encoding.UTF8.GetString(bytes));
                case nameof(ConsumerDomainEvents.FingerImgSet):
                    return JsonConvert.DeserializeObject<ConsumerDomainEvents.FingerImgSet>(Encoding.UTF8.GetString(bytes));
                case nameof(ConsumerDomainEvents.FingerImgRemoved):
                    return JsonConvert.DeserializeObject<ConsumerDomainEvents.FingerImgRemoved>(Encoding.UTF8.GetString(bytes));

                case nameof(ConsumerDomainEvents.IsActiveSet):
                    return JsonConvert.DeserializeObject<ConsumerDomainEvents.IsActiveSet>(Encoding.UTF8.GetString(bytes));
                case nameof(ConsumerDomainEvents.UserNameSet):
                    return JsonConvert.DeserializeObject<ConsumerDomainEvents.UserNameSet>(Encoding.UTF8.GetString(bytes));
                case nameof(ConsumerDomainEvents.CardAdded):
                    return JsonConvert.DeserializeObject<ConsumerDomainEvents.CardAdded>(Encoding.UTF8.GetString(bytes));
                case nameof(ConsumerDomainEvents.CardRemoved):
                    return JsonConvert.DeserializeObject<ConsumerDomainEvents.CardRemoved>(Encoding.UTF8.GetString(bytes));
                case nameof(ConsumerDomainEvents.ScannerIdSet):
                    return JsonConvert.DeserializeObject<ConsumerDomainEvents.ScannerIdSet>(Encoding.UTF8.GetString(bytes));
                //case nameof(ConsumerDomainEvents.FingerImgRemoved):
                //    return JsonConvert.DeserializeObject<ConsumerDomainEvents.FingerImgRemoved>(Encoding.UTF8.GetString(bytes));
                case nameof(ConsumerDomainEvents.FirstNameSet):
                    return JsonConvert.DeserializeObject<ConsumerDomainEvents.FirstNameSet>(Encoding.UTF8.GetString(bytes));
                case nameof(ConsumerDomainEvents.LastNameSet):
                    return JsonConvert.DeserializeObject<ConsumerDomainEvents.LastNameSet>(Encoding.UTF8.GetString(bytes));
                case nameof(ConsumerDomainEvents.EMailAddressSet):
                    return JsonConvert.DeserializeObject<ConsumerDomainEvents.EMailAddressSet>(Encoding.UTF8.GetString(bytes));
                case nameof(ConsumerDomainEvents.PinSet):
                    return JsonConvert.DeserializeObject<ConsumerDomainEvents.PinSet>(Encoding.UTF8.GetString(bytes));
                case nameof(ConsumerDomainEvents.MobilePhoneSet):
                    return JsonConvert.DeserializeObject<ConsumerDomainEvents.MobilePhoneSet>(Encoding.UTF8.GetString(bytes));
                case nameof(ConsumerDomainEvents.ClientAccountSet):
                    return JsonConvert.DeserializeObject<ConsumerDomainEvents.ClientAccountSet>(Encoding.UTF8.GetString(bytes));
                default:
                    return null;
            }
        }
    }
}