using EFT;
using SAIN.Components.PlayerComponentSpace.Classes;
using SAIN.Components.PlayerComponentSpace.Classes.Equipment;
using SAIN.Components.PlayerComponentSpace.PersonClasses;
using SAIN.Helpers;
using SAIN.SAINComponent;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace SAIN.Components.PlayerComponentSpace
{
    public class PlayerComponent : MonoBehaviour
    {
        public BodyPartsClass BodyParts { get; private set; }
        public PlayerIlluminationClass Illumination { get; private set; }

        private void Update()
        {
            Person.Update();

            if (!Person.ActivationClass.PlayerActive) {
                return;
            }

            if (!IsAI || Person.ActivationClass.BotActive) {
                Illumination.Update();
                drawTransformGizmos();
                Flashlight.Update();
                Equipment.Update();
            }
            if (Player.IsYourPlayer) {
                //testNavMeshNodes();
            }
        }

        private void testNavMeshNodes()
        {
            List<Vector3> visibleNodes = new List<Vector3>();
            Vector3 origin = Transform.EyePosition;
            Vector3[] vertices = NavMesh.CalculateTriangulation().vertices;
            foreach (Vector3 vert in vertices) {
                Vector3 direction = (vert - origin);
                float sqrMag = direction.sqrMagnitude;
                if (sqrMag > 100f * 100f) {
                    continue;
                }
                float distance = Mathf.Sqrt(sqrMag);
                if (!Physics.Raycast(origin, direction, distance, LayerMaskClass.HighPolyWithTerrainMask)) {
                    visibleNodes.Add(vert);
                    continue;
                }
                direction.y += 0.5f;
                if (!Physics.Raycast(origin, direction, distance, LayerMaskClass.HighPolyWithTerrainMask)) {
                    visibleNodes.Add(vert);
                    continue;
                }
                direction.y += 0.5f;
                if (!Physics.Raycast(origin, direction, distance, LayerMaskClass.HighPolyWithTerrainMask)) {
                    visibleNodes.Add(vert);
                    continue;
                }
                direction.y += 0.5f;
                if (!Physics.Raycast(origin, direction, distance, LayerMaskClass.HighPolyWithTerrainMask)) {
                    visibleNodes.Add(vert);
                    continue;
                }
            }
            foreach (var visibleVert in visibleNodes) {
                DebugGizmos.Ray(visibleVert, Vector3.up, Color.green, 1.5f, 0.025f, true, 0.25f);
            }
        }

        private IEnumerator voiceTest()
        {
            while (true) {
                yield return playPhrases(EPhraseTrigger.LostVisual);
                yield return playPhrases(EPhraseTrigger.OnLostVisual);
                yield return null;
            }
        }

        public bool PlayVoiceLine(EPhraseTrigger phrase, ETagStatus mask, bool aggressive)
        {
            var speaker = Player.Speaker;
            if (speaker.Speaking || speaker.Busy) {
                return false;
            }

            //if (aggressive &&
            //    speaker.PhrasesBanks.TryGetValue(phrase, out var phrasesBank)) {
            //    _aggroIndexes.Clear();
            //    int count = phrasesBank.Clips.Length;
            //    for (int i = 0; i < count; i++) {
            //        if (phrasesBank.Clips[i].Clip.name.EndsWith("_bl")) {
            //            _aggroIndexes.Add(i);
            //        }
            //    }
            //
            //    if (_aggroIndexes.Count > 0) {
            //        int index = _aggroIndexes.PickRandom();
            //        speaker.PlayDirect(phrase, index);
            //        //Logger.LogInfo($"{phrase} :: {phrasesBank.Clips[index].Clip.name} :: {index}");
            //        return true;
            //    }
            //}

            return speaker.Play(phrase, mask, true, null) != null;
        }

        private readonly List<int> _aggroIndexes = new List<int>();

        private IEnumerator playPhrases(EPhraseTrigger trigger)
        {
            var speaker = Player.Speaker;
            if (speaker.PhrasesBanks.TryGetValue(trigger, out var phrasesBank)) {
                int count = phrasesBank.Clips.Length;
                for (int i = 0; i < count; i++) {
                    bool said = false;
                    while (!said) {
                        if (!speaker.Speaking && !speaker.Busy) {
                            speaker.PlayDirect(trigger, i);
                            Logger.LogInfo($"{trigger} :: {phrasesBank.Clips[i].Clip.name} :: {i}");
                            said = true;
                        }
                        yield return null;
                    }
                }
            }
        }

        private void LateUpdate()
        {
            Person.LateUpdate();
        }

        private void drawTransformGizmos()
        {
            if (SAINPlugin.DebugMode &&
                SAINPlugin.DrawDebugGizmos &&
                SAINPlugin.DebugSettings.Gizmos.DrawTransformGizmos) {
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

        private void startCoroutines()
        {
            if (_gearCoroutine == null) {
                _gearCoroutine = StartCoroutine(Equipment.GearInfo.GearUpdateLoop());
            }
        }

        private void stopCoroutines()
        {
            if (_gearCoroutine != null) {
                StopCoroutine(_gearCoroutine);
                _gearCoroutine = null;
            }
            StopAllCoroutines();
        }

        private Coroutine _gearCoroutine;

        public float DistanceToClosestHuman {
            get
            {
                return 0f;
            }
        }

        private void navRayCastAllDir()
        {
            if (!SAINPlugin.DebugMode ||
                !SAINPlugin.DrawDebugGizmos ||
                !Player.IsYourPlayer) {
                return;
            }

            Vector3 origin = Position;
            if (NavMesh.SamplePosition(origin, out var hit, 1f, -1)) {
                origin = hit.position;
            }

            Vector3 direction;
            int max = 5;
            for (int i = 0; i < max; i++) {
                direction = UnityEngine.Random.onUnitSphere;
                direction.y = 0;
                direction = direction.normalized * 30f;
                Vector3 target = origin + direction;
                if (NavMesh.Raycast(origin, target, out var hit2, -1)) {
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

        public bool IsActive => Person.Active;
        public Vector3 Position => Person.Transform.Position;
        public Vector3 LookDirection => Person.Transform.LookDirection;
        public Vector3 LookSensorPosition => Transform.EyePosition;

        public PersonTransformClass Transform => Person.Transform;
        public Player Player => Person.Player;
        public IPlayer IPlayer => Person.IPlayer;
        public string Name => Person.Name;
        public BotOwner BotOwner => Person.AIInfo.BotOwner;
        public BotComponent BotComponent => Person.AIInfo.BotComponent;
        public bool IsAI => Person.AIInfo.IsAI;
        public bool IsSAINBot => Person.AIInfo.IsSAINBot;

        public bool Init(IPlayer iPlayer, Player player)
        {
            ProfileId = iPlayer.ProfileId;

            try {
                var playerData = new PlayerData(this, player, iPlayer);
                Person = new PersonClass(playerData);

                BodyParts = new BodyPartsClass(this);
                Flashlight = new FlashLightClass(this);
                Equipment = new SAINEquipmentClass(this);
                AIData = new SAINAIData(Equipment.GearInfo, this);
                Illumination = new PlayerIlluminationClass(this);

                Illumination.Init();

                Person.ActivationClass.OnPlayerActiveChanged += handleCoroutines;
                handleCoroutines(true);
            }
            catch (Exception ex) {
                Logger.LogError(ex);
                return false;
            }
            //Logger.LogDebug($"{Person.Nickname} Player Component Created");
            StartCoroutine(delayInit());
            return true;
        }

        private void enterTrigger(IPhysicsTrigger trigger)
        {
        }

        private void exitTrigger(IPhysicsTrigger trigger)
        {
        }

        private void handleCoroutines(bool active)
        {
            if (active)
                startCoroutines();
            else
                stopCoroutines();
        }

        private IEnumerator delayInit()
        {
            yield return null;
            Equipment.Init();
        }

        public void InitBotOwner(BotOwner botOwner)
        {
            Person.ActivationClass.OnPlayerActiveChanged -= handleCoroutines;
            Person.ActivationClass.OnBotActiveChanged += handleCoroutines;
            Person.InitBot(botOwner);
            AIData.AISoundPlayer.InitAI();
        }

        public void InitBotComponent(BotComponent bot)
        {
            Person.InitBot(bot);
        }

        private void OnDisable()
        {
            Person.ActivationClass.Disable();
            stopCoroutines();
        }

        public void Dispose()
        {
            Logger.LogDebug($"Destroying Playing Component for [Name: {Person?.Name} : Nickname: {Person?.Nickname}, ProfileID: {Person?.ProfileId}, at time: {Time.time}]");
            OnComponentDestroyed?.Invoke(ProfileId);
            stopCoroutines();
            Person.ActivationClass.OnBotActiveChanged -= handleCoroutines;
            Person.ActivationClass.OnPlayerActiveChanged -= handleCoroutines;
            Illumination?.Dispose();
            Equipment?.Dispose();
            Destroy(this);
        }

        public event Action<string> OnComponentDestroyed;
    }
}