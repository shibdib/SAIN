using UnityEngine;

namespace SAIN.Components.PlayerComponentSpace.Classes
{
    public class PlayerAISoundPlayer : AIDataBase
    {
        public PlayerAISoundPlayer(SAINAIData aidata) : base(aidata)
        {
        }

        private float _soundFrequency => (IsAI ? 0.5f : 0.1f);
        private float _lastSoundPower;
        private float _nextPlaySoundTime;

        public bool ShallPlayAISound(float power)
        {
            if (_nextPlaySoundTime < Time.time || 
                _lastSoundPower > power * 1.1f)
            {
                _nextPlaySoundTime = Time.time + _soundFrequency;
                _lastSoundPower = power;
                return true;
            }
            return false;
        }
    }
}