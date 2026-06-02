using System;
//using System.ServiceModel;
//using System.ServiceModel.Channels;
//using System.ServiceModel.Duplex;
//using System.ServiceModel.Description;
using System.Threading.Tasks;
using CoreWCF;
using CoreWCF.Channels;
using Cupid.Models;

namespace Cupid.Client
{
    public class CupidClientCallback : ICupidClientCallback
    {
        private readonly ClientSession _session;

        public CupidClientCallback(ClientSession session)
        {
            _session = session;
        }

        public void ReceiveLoveLetter(LoveLetterDto letter)
        {
            _session.PendingLetter = letter;
            _session.HasMessage = true;

            Console.WriteLine("\n💌 NEW LOVE LETTER RECEIVED");
            Console.WriteLine($"From: {letter.FromUsername}");
            Console.WriteLine($"City: {letter.City}");
            Console.WriteLine($"Age: {letter.Age}");

            if (letter.Message == "I am not interested in meeting.")
            {
                Console.WriteLine("Message: I am not interested in meeting.");
                Console.WriteLine("Phone number hidden.");
            }
            else
            {
                Console.WriteLine($"Message: {letter.Message}");
                Console.WriteLine($"Phone: {GetSenderPhone(letter.FromUsername)}");
            }

            Console.WriteLine("\nType /ack to confirm receipt");
        }

        public void ReceiveInfo(string message)
        {
            Console.WriteLine($"[INFO] {message}");
        }

        private string GetSenderPhone(string username)
        {
            // client doesn't have full directory; server could include phone in letter if needed
            return "(hidden by server)";
        }
    }

    public class ClientSession
    {
        public bool HasMessage { get; set; }
        public LoveLetterDto? PendingLetter { get; set; }
        public string AssignedUsername { get; set; } = string.Empty;
        public ICupidService? Proxy { get; set; }
    }

    class Program
    {
        static void Main(string[] args)
        {
            var session = new ClientSession();
            var callback = new CupidClientCallback(session);
            var context = new System.ServiceModel.InstanceContext(callback);

            var binding = new System.ServiceModel.NetTcpBinding(System.ServiceModel.SecurityMode.None);
            var endpoint = new System.ServiceModel.EndpointAddress("net.tcp://localhost:9000/CupidService");
            var factory = new System.ServiceModel.DuplexChannelFactory<ICupidService>(context, binding, endpoint);
            var proxy = factory.CreateChannel();
            session.Proxy = proxy;

            Console.WriteLine("Enter username:");
            var username = Console.ReadLine() ?? string.Empty;
            Console.WriteLine("Enter city:");
            var city = Console.ReadLine() ?? string.Empty;
            Console.WriteLine("Enter age:");
            var ageStr = Console.ReadLine() ?? string.Empty;
            Console.WriteLine("Enter phone:");
            var phone = Console.ReadLine() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(city) || !int.TryParse(ageStr, out var age) || age <= 0 || string.IsNullOrWhiteSpace(phone))
            {
                Console.WriteLine("Invalid input. Exiting.");
                return;
            }

            var person = new PersonDto { Username = username, City = city, Age = age, PhoneNumber = phone };
            var assigned = proxy.InitSinglePerson(person);
            session.AssignedUsername = assigned;
            Console.WriteLine($"Registered as {assigned}");

            Task.Run(() => CommandLoop(session));

            // keep main thread alive
            while (true)
            {
                Task.Delay(1000).Wait();
            }
        }

        static void CommandLoop(ClientSession session)
        {
            var proxy = session.Proxy!;
            while (true)
            {
                var input = Console.ReadLine() ?? string.Empty;
                if (session.HasMessage)
                {
                    if (input.Trim() == "/ack")
                    {
                        proxy.AcknowledgeMessage();
                        session.HasMessage = false;
                        session.PendingLetter = null;
                        Console.WriteLine("✔ Message acknowledged.");
                    }
                    else
                    {
                        Console.WriteLine("You must /ack before issuing other commands.");
                    }
                }
                else
                {
                    if (input.StartsWith("/block "))
                    {
                        var user = input.Replace("/block ", "");
                        if (proxy.BlockUser(user))
                            Console.WriteLine($"🚫 Blocked {user}");
                        else
                            Console.WriteLine("Block failed.");
                    }
                    else
                    {
                        Console.WriteLine("Unknown command.");
                    }
                }
            }
        }
    }
}
