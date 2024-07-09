using System;
using UnityEngine;

namespace SAIN.SAINComponent.Classes.Decision.Reasons
{
    public struct BotDecision<T> where T : Enum
    {
        public BotDecision(T decision, string reason)
        {
            Decision = decision;
            Type = typeof(T);
            Reason = reason;
            TimeDecisionMade = Time.time;
        }

        public T Decision { get; }
        public Type Type { get; }
        public string Reason { get; }
        public float TimeDecisionMade { get; }
        public float TimeSinceDecisionMade => TimeDecisionMade - Time.time;
    }
}
