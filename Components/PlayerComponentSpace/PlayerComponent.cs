using EFT;
using SAIN.Components.PlayerComponentSpace.Classes.Equipment;
using SAIN.SAINComponent;
using System;
using UnityEngine;

namespace SAIN.Components.PlayerComponentSpace
{
    public class PlayerComponent : MonoBehaviour
    {
        private void Update()
        {
            if (IsActive)
            {
                Equipment.Update();
            }
        }

        public bool IsActive => Person.IsActive;

        public Vector3 Position => Person.Transform.Position;
        public Vector3 LookDirection => Person.Transform.LookDirection;
        public Vector3 DirectionTo(Vector3 point) => Person.Transform.DirectionTo(point);

        public TransformClass Transform => Person.Transform;
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
        public SAINEquipmentClass Equipment { get; private set; }

        public bool Init(IPlayer iPlayer, Player player)
        {
            try
            {
                Person = new SAINPersonClass(iPlayer, player);
                FlashlightComponent = player.GetOrAddComponent<FlashLightComponent>();
                AIData = new SAINAIData();
                Equipment = new SAINEquipmentClass(this);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
                return false;
            }
            return true;
        }

        public void InitBot(BotOwner botOwner)
        {
            Person.initBot(botOwner);
        }

        private void OnDisable()
        {
            StopAllCoroutines();
        }

        private void OnDestroy()
        {
            StopAllCoroutines();
        }
    }
}