using SAIN.SAINComponent;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SAIN.BotController.Classes
{
    public class BotCoverController : SAINControl
    {
        public void FindCoverForBots()
        {
            if (_findCoverCoroutine == null)
            {
                _findCoverCoroutine = BotController.StartCoroutine(findCover());
            }
        }

        private Coroutine _findCoverCoroutine;

        private IEnumerator findCover()
        {
            while (true)
            {
                yield return null;
            }
        }

        private readonly List<BotComponent> _localBotList = new List<BotComponent>();
    }
}
