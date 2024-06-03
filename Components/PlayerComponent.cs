using EFT;
using SAIN.SAINComponent;
using SAIN.SAINComponent.BaseClasses;
using System;
using UnityEngine;

namespace SAIN.Components
{
    public class PlayerComponent : MonoBehaviour
    {
        public bool Init(IPlayer iPlayer, Player player)
        {
            bool success = false;
            try
            {
                Person = new SAINPersonClass(iPlayer, player);
                FlashlightComponent = player.GetOrAddComponent<FlashLightComponent>();
                AIData = new SAINAIData();
                success = true;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
            }
            return success;
        }

        public void InitBot(BotOwner botOwner)
        {
            Person.InitBot(botOwner);
        }

        private void Update()
        {
        }

        private void OnDisable()
        {
            StopAllCoroutines();
        }

        private void OnDestroy()
        {
            StopAllCoroutines();
        }

        public bool IsActive => Person.IsActive;
        public Vector3 Position => Person.Transform.Position;
        public Vector3 LookDirection => Person.Transform.LookDirection;
        public Vector3 DirectionTo(Vector3 point) => Person.Transform.DirectionTo(point);
        public SAINPersonTransformClass Transform => Person.Transform;
        public Player Player => Person.Player;
        public IPlayer IPlayer => Person.IPlayer;
        public string ProfileId => Person.ProfileId;
        public string Name => Person.Name;
        public BotOwner BotOwner => Person.BotOwner;
        public BotComponent BotComponent => Person.BotComponent;
        public bool IsAI => Person.IsAI;
        public bool IsSAINBot => Person.IsSAINBot;
        public FlashLightComponent FlashlightComponent { get; private set; }
        public SAINPersonClass Person { get; private set; }
        public SAINAIData AIData { get; private set; }
    }
}