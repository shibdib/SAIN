using EFT;
using SAIN.Components.PlayerComponentSpace.Classes;
using SAIN.Components.PlayerComponentSpace.Classes.Equipment;
using SAIN.Helpers;
using SAIN.SAINComponent;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

namespace SAIN.Components.PlayerComponentSpace
{
    public class PlayerComponent : MonoBehaviour
    {
        private void Update()
        {
            updatePosition();

            if (IsActive)
            {
                Flashlight.Update();
                Equipment.Update();
                navRayCastAllDir();
            }
        }

        private void updatePosition()
        {
            Position = Person.Transform.Position;
            LookDirection = Person.Transform.LookDirection;
        }

        private void navRayCastAllDir()
        {
            if (!SAINPlugin.DebugMode ||
                !SAINPlugin.DrawDebugGizmos ||
                !Player.IsYourPlayer)
            {
                return;
            }

            Vector3 origin = Position;
            if (NavMesh.SamplePosition(origin, out var hit, 1f, -1))
            {
                origin = hit.position;
            }

            Vector3 direction;
            int max = 5;
            for (int i = 0; i < max; i++)
            {
                direction = UnityEngine.Random.onUnitSphere;
                direction.y = 0;
                direction = direction.normalized * 30f;
                Vector3 target = origin + direction;
                if (NavMesh.Raycast(origin, target, out var hit2, -1))
                {
                    target = hit2.position;
                }
                DebugGizmos.Line(origin, target, 0.05f, 0.25f, true);
            }
        }

        public string ProfileId { get; private set; }
        public FlashLightClass Flashlight { get; private set; }
        public PersonClass Person { get; private set; }
        public SAINAIData AIData { get; private set; }
        public SAINEquipmentClass Equipment { get; private set; }

        public bool IsActive => Person.IsActive;
        public Vector3 Position { get; private set; }
        public Vector3 LookDirection { get; private set; }

        public Vector3 DirectionTo(Vector3 point) => Person.Transform.DirectionTo(point);

        public PersonTransformClass Transform => Person.Transform;
        public Player Player => Person.Player;
        public IPlayer IPlayer => Person.IPlayer;
        public string Name => Person.Name;
        public BotOwner BotOwner => Person.BotOwner;
        public BotComponent BotComponent => Person.BotComponent;
        public bool IsAI => Person.IsAI;
        public bool IsSAINBot => Person.IsSAINBot;

        public bool Init(IPlayer iPlayer, Player player)
        {
            try
            {
                ProfileId = iPlayer.ProfileId;
                Person = new PersonClass(iPlayer, player);

                Flashlight = new FlashLightClass(this);
                Equipment = new SAINEquipmentClass(this);

                AIData = new SAINAIData(Equipment.GearInfo, this);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
                return false;
            }
            Logger.LogDebug($"{Name} Player Component Created");
            StartCoroutine(delayInit());
            return true;
        }

        private IEnumerator delayInit()
        {
            yield return null;
            Equipment.Init();
        }

        public void InitBotOwner(BotOwner botOwner)
        {
            Person.InitBotOwner(botOwner);
        }

        public void InitBotComponent(BotComponent bot)
        {
            Person.InitBotComponent(bot);
        }

        private void OnDisable()
        {
            StopAllCoroutines();
        }

        private void OnDestroy()
        {
            StopAllCoroutines();
            Equipment?.Dispose();
            OnComponentDestroyed?.Invoke(ProfileId);
            Logger.LogDebug($"{Name} Player Component Destroyed");
        }

        public Action<string> OnComponentDestroyed { get; set; }
    }
}