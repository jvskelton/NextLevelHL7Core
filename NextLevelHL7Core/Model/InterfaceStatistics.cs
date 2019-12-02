using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace NextLevelHL7
{
    public class InterfaceStatistics
    {
        public ConcurrentDictionary<string, int> Successes = new ConcurrentDictionary<string, int>();
        public ConcurrentDictionary<string, int> Failures = new ConcurrentDictionary<string, int>();
        public DateTime? LastMessageDateTime { get; set; }
        public DateTime StartDateTime { get; set;  }
        public TimeSpan UpTime { get { return DateTime.Now - StartDateTime; } }

        public InterfaceStatistics()
        {
            StartDateTime = DateTime.Now;
        }

        public void AddSuccess(string messageType)
        {
            LastMessageDateTime = DateTime.Now;
            if (!Successes.ContainsKey(messageType))
                Successes[messageType] = 0;
            Successes[messageType] = Successes[messageType] + 1;
        }

        public void AddFailure(string messageType = "ALL")
        {
            LastMessageDateTime = DateTime.Now;
            if (!Failures.ContainsKey(messageType))
                Failures[messageType] = 0;
            Failures[messageType] = Failures[messageType] + 1;
        }

        public void Merge(InterfaceStatistics statistics)
        {
            foreach (KeyValuePair<string, int> kvp in statistics.Successes)
                Successes[kvp.Key] += Successes[kvp.Key] + kvp.Value;
        }

        public void Clear()
        {
            Successes.Clear();
            Failures.Clear();
        }

        public int GetMessagesReceived()
        {
            return Successes.Values.Sum();
        }

        public bool HasReceivedMessage(TimeSpan timeSpan)
        {
            if (!LastMessageDateTime.HasValue)
                return true;
            if (DateTime.Now - LastMessageDateTime > timeSpan)
                return false;
            return true;
        }
    }
}
