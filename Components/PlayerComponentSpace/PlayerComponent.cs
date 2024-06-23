using EFT;
using EFT.Interactive;
using SAIN.Components.PlayerComponentSpace.Classes;
using SAIN.Components.PlayerComponentSpace.Classes.Equipment;
using SAIN.Helpers;
using SAIN.SAINComponent;
using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.AI;

namespace SAIN.Components.PlayerComponentSpace
{
    public class PlayerComponent : MonoBehaviour
    {
        private void Update()
        {
            if (!Person.PlayerActive)
            {
                endLoops();
                return;
            }


            bool active = !Person.IsAI || Person.BotActive;
            startLoops(active);

            if (active)
            {
                drawTransformGizmos();
                Flashlight.Update();
                Equipment.Update();
            }
        }

        private void drawTransformGizmos()
        {
            if (SAINPlugin.DebugMode &&
                SAINPlugin.DrawDebugGizmos &&
                SAINPlugin.DebugSettings.DrawTransformGizmos)
            {
                DebugGizmos.Sphere(Transform.EyePosition, 0.05f, Color.white, true, 0.1f);
                DebugGizmos.Ray(Transform.EyePosition, Transform.HeadLookDirection, Color.white, Transform.HeadLookDirection.magnitude, 0.025f, true, 0.1f);

                DebugGizmos.Sphere(Transform.HeadPosition, 0.075f, Color.yellow, true, 0.1f);
                DebugGizmos.Ray(Transform.HeadPosition, Transform.LookDirection, Color.yellow, Transform.LookDirection.magnitude, 0.025f, true, 0.1f);

                DebugGizmos.Sphere(Transform.WeaponFirePort, 0.075f, Color.green, true, 0.1f);
                DebugGizmos.Ray(Transform.WeaponFirePort, Transform.WeaponPointDirection, Color.green, Transform.WeaponPointDirection.magnitude, 0.05f, true, 0.1f);

                DebugGizmos.Sphere(Transform.BodyPosition, 0.1f, Color.blue, true, 0.1f);
                DebugGizmos.Ray(Transform.BodyPosition, Transform.LookDirection, Color.blue, Transform.LookDirection.magnitude, 0.05f, true, 0.1f);
            }
        }

        private void startLoops(bool aiActive)
        {
            if (_transformCoroutine == null)
            {
                _transformCoroutine = StartCoroutine(updateTransformsLoop());
            }
            if (aiActive && _gearCoroutine == null)
            {
                _gearCoroutine = StartCoroutine(Equipment.GearInfo.GearUpdateLoop());
            }
        }

        private void endLoops()
        {
            if (_transformCoroutine != null)
            {
                StopCoroutine(_transformCoroutine);
                _transformCoroutine = null;
            }
            if (_gearCoroutine != null)
            {
                StopCoroutine(_gearCoroutine);
                _gearCoroutine = null;
            }
        }

        private Coroutine _transformCoroutine;
        private Coroutine _gearCoroutine;

        private IEnumerator updateTransformsLoop()
        {
            WaitForSeconds wait = new WaitForSeconds(PersonTransformClass.TRANSFORM_UPDATE_FREQ);
            while (true)
            {
                Transform.UpdatePositions();
                yield return wait;
            }
        }

        public Vector3? WeaponShotHitPoint
        {
            get
            {
                if (Time.frameCount != _cachedFrame)
                {
                    _cachedFrame = Time.frameCount;
                    Vector3 firePort = Transform.WeaponFirePort;
                    Vector3 weaponPointDir = Transform.WeaponPointDirection;

                }
                return _weaponShotHitPoint;
            }
        }

        public float DistanceToClosestHuman
        {
            get
            {
                return 0f;
            }
        }

        private int _cachedFrame;
        private Vector3? _weaponShotHitPoint;

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
        public Vector3 Position => Person.Transform.Position;
        public Vector3 LookDirection => Person.Transform.LookDirection;
        public Vector3 LookSensorPosition => Transform.EyePosition;

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
                Person = new PersonClass(iPlayer, player, this);

                Flashlight = new FlashLightClass(this);
                Equipment = new SAINEquipmentClass(this);

                AIData = new SAINAIData(Equipment.GearInfo, this);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
                return false;
            }
            Logger.LogDebug($"{Person.Nickname} Player Component Created");
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
            endLoops();
            StopAllCoroutines();
        }

        public void Dispose()
        {
            OnComponentDestroyed?.Invoke(ProfileId);
            endLoops();
            StopAllCoroutines();
            Equipment?.Dispose();
            Destroy(this);
        }

        public Action<string> OnComponentDestroyed { get; set; }
    }
}