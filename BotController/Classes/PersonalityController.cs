using SAIN.Components.BotController;
using SAIN.SAINComponent;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SAIN.BotController.Classes
{
    public class PersonalityController : SAINControl
    {
        public static PersonalityController Instance;
        public PersonalityController()
        {
            Instance = this;
        }

        public void Update()
        {
        }

        public EPersonality GetAPersonality(Bot bot)
        {
            return EPersonality.Normal;
        }

        private void checkPersonalitiesOnAllBots()
        {
            _personalityCounts.Clear();
            foreach (var kvp in Bots)
            {
                Bot bot = kvp.Value;
                if (bot != null)
                {
                    updateCount(bot.Info.Personality, 1);
                }
            }
        }

        private void updateCount(EPersonality personality, int count)
        {
            if (_personalityCounts.ContainsKey(personality))
            {
                _personalityCounts[personality] += count;
                return;
            }
            _personalityCounts.Add(personality, count);
        }

        private readonly Dictionary<EPersonality, int> _personalityCounts = new Dictionary<EPersonality, int>();

    }
}
