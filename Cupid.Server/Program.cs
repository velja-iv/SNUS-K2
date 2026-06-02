using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using CoreWCF.Configuration;
using CoreWCF.Description;
using CoreWCF.Channels;
using Cupid.Models;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Cupid.Server
{
    [CoreWCF.ServiceBehavior(InstanceContextMode = CoreWCF.InstanceContextMode.Single, ConcurrencyMode = CoreWCF.ConcurrencyMode.Multiple)]
    public class CupidService : ICupidService
    {
        private readonly ConcurrentDictionary<string, UserState> _users = new();
        private readonly ConcurrentDictionary<string, ICupidClientCallback> _callbacks = new();
        private readonly ConcurrentDictionary<string, object> _userLocks = new();

        private Timer? _timer;

        public void Start()
        {
            _timer = new Timer(RunCycle, null, Timeout.Infinite, 60_000);
        }

        public void StartImmediatelyForTesting() { _timer = new Timer(RunCycle, null, 0, 60_000); }

        public string InitSinglePerson(PersonDto person)
        {
            if (string.IsNullOrWhiteSpace(person.Username))
                throw new ArgumentException("Username required");

            // ensure numeric age and phone validation done by client - server trusts DTO here
            var assigned = person.Username;

            if (_users.ContainsKey(assigned))
            {
                var suffix = Guid.NewGuid().ToString("N").Substring(0, 8);
                assigned = assigned + "-" + suffix;
                person.Username = assigned;
            }

            var state = new UserState { Person = person };
            _users[assigned] = state;
            _userLocks[assigned] = new object();

            var callback = CoreWCF.OperationContext.Current.GetCallbackChannel<ICupidClientCallback>();
            _callbacks[assigned] = callback;

            Console.WriteLine($"[REGISTER] {assigned}");
            return assigned;
        }

        public bool BlockUser(string usernameToBlock)
        {
            var caller = GetCallerUsername();
            if (caller == null) return false;
            if (!_users.ContainsKey(usernameToBlock)) return false;

            lock (_userLocks[caller])
            {
                _users[caller].BlockedUsers.Add(usernameToBlock);
            }

            Console.WriteLine($"[BLOCK] {caller} blocked {usernameToBlock}");
            return true;
        }

        public bool AcknowledgeMessage()
        {
            var caller = GetCallerUsername();
            if (caller == null) return false;

            if (!_users.TryGetValue(caller, out var user))
                return false;

            lock (_userLocks[caller])
            {
                if (!user.HasPendingMessage)
                    return false;

                user.HasPendingMessage = false;
                user.PendingMessage = null;
            }

            Console.WriteLine($"[ACK] {caller} acknowledged message");
            return true;
        }

        private string? GetCallerUsername()
        {
            try
            {
                var callback = CoreWCF.OperationContext.Current.GetCallbackChannel<ICupidClientCallback>();
                var kv = _callbacks.FirstOrDefault(x => x.Value == callback);
                if (kv.Key == null) return null;
                return kv.Key;
            }
            catch
            {
                return null;
            }
        }

        private void RunCycle(object? state)
        {
            try
            {
                Console.WriteLine("[CYCLE] Running matchmaking cycle");

                // reset HasSentThisCycle
                foreach (var kv in _users)
                {
                    kv.Value.HasSentThisCycle = false;
                }

                var snapshots = _users.Values.ToList();

                foreach (var sender in snapshots)
                {
                    if (sender.HasSentThisCycle)
                        continue;

                    if (sender.HasPendingMessage)
                        continue;

                    var recipient = FindBestMatch(sender);

                    if (recipient == null)
                        continue;

                    SendMessage(sender, recipient);
                    sender.HasSentThisCycle = true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Cycle failed: {ex.Message}");
            }
        }

        private List<UserState> GetCandidates(UserState sender)
        {
            return _users.Values
                .Where(u => u.Person.Username != sender.Person.Username
                            && !u.BlockedUsers.Contains(sender.Person.Username)
                            && !u.HasPendingMessage)
                .ToList();
        }

        private int GetRandomScore()
        {
            return RandomNumberGenerator.GetInt32(0, 101);
        }

        private int CalculateScore(PersonDto a, PersonDto b)
        {
            int score = 0;
            if (a.City == b.City) score += 30;
            if (Math.Abs(a.Age - b.Age) <= 2) score += 20;
            score += GetRandomScore();
            return score;
        }

        private UserState? FindBestMatch(UserState sender)
        {
            var candidates = GetCandidates(sender);
            UserState? best = null;
            int bestScore = -1;

            foreach (var candidate in candidates)
            {
                int score = CalculateScore(sender.Person, candidate.Person);
                if (score > bestScore)
                {
                    bestScore = score;
                    best = candidate;
                }
            }

            return best;
        }

        private void SendMessage(UserState sender, UserState recipient)
        {
            var letter = new LoveLetterDto
            {
                FromUsername = sender.Person.Username,
                ToUsername = recipient.Person.Username,
                City = sender.Person.City,
                Age = sender.Person.Age,
                SentAt = DateTime.UtcNow,
                Message = GenerateRandomMessage()
            };

            try
            {
                if (!_callbacks.TryGetValue(recipient.Person.Username, out var callback))
                {
                    Console.WriteLine($"[SEND FAILED] No callback for {recipient.Person.Username}");
                    recipient.HasPendingMessage = false;
                    recipient.PendingMessage = null;
                    return;
                }

                lock (_userLocks[recipient.Person.Username])
                {
                    recipient.PendingMessage = letter;
                    recipient.HasPendingMessage = true;
                }

                callback.ReceiveLoveLetter(letter);
                Console.WriteLine($"[SEND] {sender.Person.Username} -> {recipient.Person.Username} (message sent)");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SEND FAILED] {ex.Message}");
                lock (_userLocks[recipient.Person.Username])
                {
                    recipient.HasPendingMessage = false;
                    recipient.PendingMessage = null;
                }
            }
        }

        private string GenerateRandomMessage()
        {
            string[] messages =
            {
                "I look forward to our meeting!",
                "I would like to get to know you.",
                "I am not interested in meeting."
            };

            int index = RandomNumberGenerator.GetInt32(messages.Length);
            return messages[index];
        }
    }

}
