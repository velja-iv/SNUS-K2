using System;

namespace Cupid.Models
{
    using System.ServiceModel;

    [ServiceContract(CallbackContract = typeof(ICupidClientCallback))]
    public interface ICupidService
    {
        [OperationContract]
        string InitSinglePerson(PersonDto person);

        [OperationContract]
        bool BlockUser(string usernameToBlock);

        [OperationContract]
        bool AcknowledgeMessage();
    }

    [ServiceContract]
    public interface ICupidClientCallback
    {
        [OperationContract(IsOneWay = true)]
        void ReceiveLoveLetter(LoveLetterDto letter);

        [OperationContract(IsOneWay = true)]
        void ReceiveInfo(string message);
    }

    public class PersonDto
    {
        public string Username { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public int Age { get; set; }
        public string PhoneNumber { get; set; } = string.Empty;
    }

    public class LoveLetterDto
    {
        public string FromUsername { get; set; } = string.Empty;
        public string ToUsername { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public int Age { get; set; }
        public string Message { get; set; } = string.Empty;
        public DateTime SentAt { get; set; }
    }

    public class UserState
    {
        public PersonDto Person { get; set; } = new PersonDto();
        public System.Collections.Generic.HashSet<string> BlockedUsers { get; set; } = new System.Collections.Generic.HashSet<string>();
        public bool HasPendingMessage { get; set; }
        public LoveLetterDto? PendingMessage { get; set; }
        public bool HasSentThisCycle { get; set; }
    }
}
