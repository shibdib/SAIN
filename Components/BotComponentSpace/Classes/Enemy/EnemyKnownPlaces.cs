using EFT;
using SAIN.Helpers;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace SAIN.SAINComponent.Classes.EnemyClasses
{
    public class EnemyKnownPlaces : EnemyBase, ISAINEnemyClass
    {
        public Action<EnemyPlace, BotComponent> OnEnemyPlaceAdded;
        public Action<EnemyPlace> OnNewLastKnown { get; set; }

        public EnemyPlace LastSeenPlace { get; private set; }
        public EnemyPlace LastHeardPlace { get; private set; }
        public EnemyPlace LastSquadSeenPlace { get; private set; }
        public EnemyPlace LastSquadHeardPlace { get; private set; }

        public float TimeSinceLastKnownUpdated { get; private set; } = float.MaxValue;
        public EnemyPlace LastKnownPlace { get; private set; }
        public Vector3? LastKnownPosition { get; private set; }
        public float EnemyDistanceFromLastKnown { get; private set; }

        public bool SearchedAllKnownLocations
        {
            get
            {
                if (_nextCheckSearchTime < Time.time)
                {
                    _nextCheckSearchTime = Time.time + 0.5f;

                    bool allSearched = true;
                    foreach (var place in AllEnemyPlaces)
                    {
                        if (!place.HasArrivedPersonal)
                        {
                            allSearched = false;
                            break;
                        }
                    }

                    if (allSearched
                        && !_searchedAllKnownLocations)
                    {
                        TimeAllLocationsSearched = Time.time;
                    }

                    _searchedAllKnownLocations = allSearched;
                }
                return _searchedAllKnownLocations;
            }
        }
        public float TimeSinceAllLocationsSearched => Time.time - TimeAllLocationsSearched;
        public float TimeAllLocationsSearched { get; private set; }

        public List<EnemyPlace> AllEnemyPlaces
        {
            get
            {
                updatePlaces();
                return _allEnemyPlaces;
            }
        }

        public EnemyKnownPlaces(Enemy enemy) : base(enemy)
        {
        }

        public void Init()
        {
            Bot.OnBotDisabled += botDisabled;
            Enemy.OnEnemyForgotten += onEnemyForgotten;
            Enemy.OnEnemyKnown += onEnemyKnown;
        }

        private void botDisabled()
        {
            clearAllPlaces();
        }

        public void Dispose()
        {
            clearAllPlaces();
            Bot.OnBotDisabled -= botDisabled;
            Enemy.OnEnemyForgotten -= onEnemyForgotten;
            Enemy.OnEnemyKnown -= onEnemyKnown;

            foreach (var obj in _guiObjects)
            {
                DebugGizmos.DestroyLabel(obj.Value);
            }
            _guiObjects?.Clear();
        }

        public void onEnemyForgotten(Enemy enemy)
        {
            clearAllPlaces();
        }

        public void onEnemyKnown(Enemy enemy)
        {

        }

        private void clearAllPlaces()
        {
            _allEnemyPlaces.Clear();
            LastSeenPlace = null;
            LastHeardPlace = null;
            LastSquadSeenPlace = null;
            LastSquadHeardPlace = null;
            LastKnownPosition = null;
            LastKnownPlace = null;
            EnemyDistanceFromLastKnown = float.MaxValue;
        }

        public void Update()
        {
            updateLastKnown();
            updatePlaces();
            if (Enemy.EnemyKnown)
            {
                checkIfArrived();
                if (Enemy.IsCurrentEnemy)
                {
                    checkIfSeen();
                    createDebug();
                }
            }
        }

        private void createDebug()
        {
            if (SAINPlugin.DebugMode)
            {
                EnemyPlace lastKnown = LastKnownPlace;
                if (lastKnown != null)
                {
                    if (debugLastKnown == null)
                    {
                        debugLastKnown = DebugGizmos.CreateLabel(lastKnown.Position, string.Empty);
                    }
                    updateDebugString(lastKnown, debugLastKnown);
                }
            }
            else if (debugLastKnown != null)
            {
                DebugGizmos.DestroyLabel(debugLastKnown);
                debugLastKnown = null;
            }
        }

        private void checkIfSeen()
        {
            if (_nextCheckSeenTime < Time.time)
            {
                _nextCheckSeenTime = Time.time + _checkSeenFreq;

                Vector3 lookSensor = Bot.PlayerComponent.LookSensorPosition;
                string myProfileId = Bot.ProfileId;

                foreach (EnemyPlace place in AllEnemyPlaces)
                {
                    if (place != null)
                    {
                        bool ismyPlace = place.OwnerID == myProfileId;
                        if (ismyPlace && !place.HasSeenPersonal
                            && place.PersonalClearLineOfSight(lookSensor, LayerMaskClass.HighPolyWithTerrainMaskAI))
                        {
                            tryTalk();
                        }
                        else if (!ismyPlace && !place.HasSeenSquad
                            && place.SquadClearLineOfSight(lookSensor, LayerMaskClass.HighPolyWithTerrainMaskAI))
                        {
                            tryTalk();
                        }
                    }
                }
            }
        }

        private void tryTalk()
        {
            if (_nextTalkClearTime < Time.time 
                && Bot.Talk.GroupSay(EFTMath.RandomBool() ? EPhraseTrigger.Clear : EPhraseTrigger.LostVisual, null, true, 33))
            {
                _nextTalkClearTime = Time.time + 5f;
            }
        }

        private void checkIfArrived()
        {
            float time = Time.time;
            if (_checkArrivedTime < time)
            {
                _checkArrivedTime = time + _checkArriveFreq;
                Vector3 myPosition = Bot.Position;
                string myProfileID = Bot.ProfileId;

                foreach (EnemyPlace place in AllEnemyPlaces)
                {
                    if (place != null)
                    {
                        bool ismyPlace = place.OwnerID == myProfileID;
                        if (ismyPlace 
                            && checkPersonalArrived(place, myPosition))
                        {
                            tryTalk();
                        }
                        else 
                        if (!ismyPlace 
                            && checkSquadArrived(place, myPosition))
                        {
                            tryTalk();
                        }
                    }
                }
            }
        }

        private bool checkPersonalArrived(EnemyPlace place, Vector3 myPosition)
        {
            if (place != null
                && !place.HasArrivedPersonal
                && checkArrived(place, myPosition))
            {
                place.HasArrivedPersonal = true;
                return true;
            }
            return false;
        }

        private bool checkSquadArrived(EnemyPlace place, Vector3 myPosition)
        {
            if (place != null
                && !place.HasArrivedSquad
                && checkArrived(place, myPosition))
            {
                place.HasArrivedSquad = true;
                return true;
            }
            return false;
        }

        private bool checkArrived(EnemyPlace place, Vector3 myPosition)
        {
            return (myPosition - place.Position).sqrMagnitude < arrivedReachDistanceSqr;
        }

        private void updateDebugString(EnemyPlace place, GUIObject obj)
        {
            obj.WorldPos = place.Position;

            StringBuilder stringBuilder = obj.StringBuilder;
            stringBuilder.Clear();

            stringBuilder.AppendLine($"Bot: {BotOwner.name}");
            stringBuilder.AppendLine($"Known Location of {EnemyPlayer.Profile.Nickname}");

            if (LastKnownPlace == place)
            {
                stringBuilder.AppendLine($"Last Known Location.");
            }

            stringBuilder.AppendLine($"Time Since Position Updated: {Time.time - place.TimePositionUpdated}");

            stringBuilder.AppendLine($"Arrived? [{place.HasArrivedPersonal}]"
                + (place.HasArrivedPersonal ? $"Time Since Arrived: [{Time.time - place.TimeArrived}]" : string.Empty));

            stringBuilder.AppendLine($"Seen? [{place.HasSeenPersonal}]"
                + (place.HasSeenPersonal ? $"Time Since Seen: [{Time.time - place.TimeSeen}]" : string.Empty));
        }

        public EnemyPlace UpdateSeenPlace(Vector3 position)
        {
            if (LastSeenPlace == null)
            {
                LastSeenPlace = new EnemyPlace(Bot.Player.ProfileId, position, 300f, true, EnemyIPlayer);
                LastSeenPlace.HasSeenPersonal = true;
                addPlace(LastSeenPlace);
            }
            else
            {
                LastSeenPlace.Position = position;
            }
            return LastSeenPlace;
        }

        private void addPlace(EnemyPlace enemyPlace)
        {
            if (enemyPlace != null)
            {
                if (_searchedAllKnownLocations && 
                    !enemyPlace.HasArrivedPersonal)
                {
                    _searchedAllKnownLocations = false;
                }
                _allEnemyPlaces.Add(enemyPlace);
                updatePlaces(true);
                OnEnemyPlaceAdded?.Invoke(enemyPlace, Bot);
            }
        }

        public void UpdateSquadSeenPlace(EnemyPlace place)
        {
            if (place == null)
            {
                return;
            }
            if (LastSquadSeenPlace == place)
            {
                return;
            }
            if (LastSquadSeenPlace != null)
            {
                _allEnemyPlaces.Remove(LastSquadSeenPlace);
            }
            LastSquadSeenPlace = place;
            addPlace(place);
        }

        public EnemyPlace UpdatePersonalHeardPosition(Vector3 position, bool arrived, bool isGunFire)
        {
            if (Enemy.IsVisible)
            {
                return null;
            }

            _searchedAllKnownLocations = false;

            var lastHeard = LastHeardPlace;
            if (lastHeard != null)
            {
                if ((lastHeard.Position - position).sqrMagnitude < 5f * 5f)
                {
                    lastHeard.Position = position;
                    lastHeard.HasArrivedPersonal = arrived;
                    lastHeard.HasArrivedSquad = false;
                    lastHeard.HasSeenPersonal = false;
                    lastHeard.HasSeenSquad = false;
                    updatePlaces(true);
                    return lastHeard;
                }
                _allEnemyPlaces.Remove(lastHeard);
            }

            LastHeardPlace = new EnemyPlace(Bot.Player.ProfileId, position, 300f, isGunFire, EnemyIPlayer)
            {
                HasArrivedPersonal = arrived,
            };

            addPlace(LastHeardPlace);
            return LastHeardPlace;
        }

        public void UpdateSquadHeardPlace(EnemyPlace place)
        {
            if (place == null)
            {
                return;
            }
            if (LastSquadHeardPlace == place)
            {
                return;
            }
            if (LastSquadHeardPlace != null)
            {
                _allEnemyPlaces.Remove(LastSquadHeardPlace);
            }
            LastSquadHeardPlace = place;
            addPlace(place);
        }

        private void updatePlaces(bool force = false)
        {
            if (force || _nextUpdatePlacesTime < Time.time)
            {
                _nextUpdatePlacesTime = Time.time + 0.5f;
                if (_allEnemyPlaces.Count > 0)
                {
                    sortAndClearPlaces(_allEnemyPlaces);
                }
            }
        }

        private void sortAndClearPlaces(List<EnemyPlace> places)
        {
            places.RemoveAll(x => x?.ShallClear == true || x == null);
            places.Sort((x, y) => x.TimeSincePositionUpdated.CompareTo(y.TimeSincePositionUpdated));
        }

        private void updateLastKnown()
        {
            EnemyPlace lastKnown = findMostRecentPlace();
            if (lastKnown == null)
            {
                EnemyDistanceFromLastKnown = float.MaxValue;
                LastKnownPlace = null;
                TimeSinceLastKnownUpdated = float.MaxValue;
                return;
            }

            LastKnownPlace = lastKnown;
            TimeSinceLastKnownUpdated = lastKnown.TimeSincePositionUpdated;
            Vector3 placePosition = lastKnown.Position;
            LastKnownPosition = placePosition;

            bool newLastKnown = _lastLastKnownPlace == null || _lastLastKnownPlace != lastKnown;
            if (newLastKnown)
            {
                _lastLastKnownPlace = lastKnown;
                OnNewLastKnown?.Invoke(lastKnown);
            }

            float timeAdd = Enemy.IsCurrentEnemy ? 0.2f : 0.5f;

            if (newLastKnown || _nextUpdateDistTime + timeAdd < Time.time)
            {
                _nextUpdateDistTime = Time.time;
                EnemyDistanceFromLastKnown = (placePosition - EnemyPosition).magnitude;
            }
        }

        private EnemyPlace findMostRecentPlace()
        {
            EnemyPlace result = null;
            float timeUpdated = float.MaxValue;
            if (LastSeenPlace != null)
            {
                result = LastSeenPlace;
                timeUpdated = LastSeenPlace.TimeSincePositionUpdated;
            }
            if (Enemy.IsVisible)
            {
                return result;
            }
            foreach (var place in AllEnemyPlaces)
            {
                if (place != null &&
                    place.TimeSincePositionUpdated < timeUpdated)
                {
                    timeUpdated = place.TimeSincePositionUpdated;
                    result = place;
                }
            }
            return result;
        }

        private float _nextUpdateDistTime;
        private float _nextTalkClearTime;
        private float _checkArrivedTime;
        private const float _checkArriveFreq = 0.25f;
        private const float arrivedReachDistanceSqr = 0.5f;
        private bool _searchedAllKnownLocations;
        private float _nextCheckSearchTime;
        private float _nextCheckSeenTime;
        private const float _checkSeenFreq = 0.5f;
        private EnemyPlace _lastLastKnownPlace;
        private float _nextUpdatePlacesTime;
        private readonly List<EnemyPlace> _allEnemyPlaces = new List<EnemyPlace>();
        private GUIObject debugLastKnown;
        private readonly List<EnemyPlace> _debugPlacesToRemove = new List<EnemyPlace>();
        private readonly Dictionary<EnemyPlace, GUIObject> _guiObjects = new Dictionary<EnemyPlace, GUIObject>();
    }
}