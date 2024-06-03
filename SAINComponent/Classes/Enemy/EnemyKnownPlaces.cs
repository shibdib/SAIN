using Comfort.Common;
using EFT;
using JetBrains.Annotations;
using SAIN.Helpers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UIElements;

namespace SAIN.SAINComponent.Classes.Enemy
{
    public class EnemyKnownPlaces
    {
        public EnemyKnownPlaces(SAINEnemy enemy)
        {
            _enemy = enemy;
        }

        private readonly SAINEnemy _enemy;

        public void Dispose()
        {
            foreach (var obj in _guiObjects)
            {
                DebugGizmos.DestroyLabel(obj.Value);
            }
            _guiObjects?.Clear();
        }

        public void Update(bool isCurrentEnemy)
        {
            updatePlaces();
            checkIfArrived();
            if (isCurrentEnemy)
            {
                checkIfSeen();
                createDebug();
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

        private GUIObject debugLastKnown;

        private void checkIfSeen()
        {
            if (_nextCheckSeenTime < Time.time)
            {
                _nextCheckSeenTime = Time.time + _checkSeenFreq;

                Vector3 lookSensor = _enemy.BotOwner.LookSensor._headPoint;
                string id = _enemy.Player.ProfileId;

                foreach (EnemyPlace place in AllEnemyPlaces)
                {
                    if (place != null)
                    {
                        bool ismyPlace = place.OwnerID == id;
                        if (ismyPlace
                            && !place.HasSeenPersonal
                            && place.PersonalClearLineOfSight(lookSensor, LayerMaskClass.HighPolyWithTerrainMaskAI))
                        {
                            tryTalk();
                        }
                        else
                        if (!ismyPlace
                            && !place.HasSeenSquad
                            && place.SquadClearLineOfSight(lookSensor, LayerMaskClass.HighPolyWithTerrainMaskAI))
                        {
                            tryTalk();
                        }
                    }
                }
            }
        }

        private float _nextTalkClearTime;

        private void tryTalk()
        {
            if (_nextTalkClearTime < Time.time 
                && _enemy.Bot.Talk.GroupSay(EFTMath.RandomBool() ? EPhraseTrigger.Clear : EPhraseTrigger.LostVisual, null, true, 40))
            {
                _nextTalkClearTime = Time.time + 5f;
            }
        }

        private float _checkArrivedTime;
        private const float _checkArriveFreq = 0.25f;

        private void checkIfArrived()
        {
            float time = Time.time;
            if (_checkArrivedTime < time)
            {
                _checkArrivedTime = time + _checkArriveFreq;
                Vector3 myPosition = _enemy.Bot.Position;
                string id = _enemy.Player.ProfileId;

                foreach (EnemyPlace place in AllEnemyPlaces)
                {
                    if (place != null)
                    {
                        bool ismyPlace = place.OwnerID == id;
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

        private const float arrivedReachDistanceSqr = 0.5f;

        private readonly List<EnemyPlace> _debugPlacesToRemove = new List<EnemyPlace>();

        private void updateDebugString(EnemyPlace place, GUIObject obj)
        {
            obj.WorldPos = place.Position;

            StringBuilder stringBuilder = obj.StringBuilder;
            stringBuilder.Clear();

            stringBuilder.AppendLine($"Bot: {_enemy.BotOwner.name}");
            stringBuilder.AppendLine($"Known Location of {_enemy?.EnemyPlayer.Profile.Nickname}");

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

        private readonly Dictionary<EnemyPlace, GUIObject> _guiObjects = new Dictionary<EnemyPlace, GUIObject>();

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

        private bool _searchedAllKnownLocations;

        private float _nextCheckSearchTime;

        private float _nextCheckSeenTime;

        private const float _checkSeenFreq = 0.5f;

        public EnemyPlace UpdateSeenPlace(Vector3 position)
        {
            if (LastSeenPlace == null)
            {
                LastSeenPlace = new EnemyPlace(_enemy.Bot.Player.ProfileId, position, 300f, true, _enemy.EnemyIPlayer);
                LastSeenPlace.HasSeenPersonal = _enemy.IsVisible;
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
                _allEnemyPlaces.Add(enemyPlace);
                updatePlaces(true);
                OnEnemyPlaceAdded?.Invoke(enemyPlace, _enemy.Bot);
            }
        }

        public void UpdateSquadSeenPlace(EnemyPlace place)
        {
            if (LastSquadSeenPlace == null || LastSquadSeenPlace != place)
            {
                LastSquadSeenPlace = place;
                addPlace(LastSquadSeenPlace);
                return;
            }
        }

        public Action<EnemyPlace, BotComponent> OnEnemyPlaceAdded;

        public EnemyPlace UpdatePersonalHeardPosition(Vector3 position, bool arrived, bool isGunFire)
        {
            if (_enemy.IsVisible)
            {
                return null;
            }

            _searchedAllKnownLocations = false;

            var lastHeard = LastHeardPlace;
            if (lastHeard != null
                && (lastHeard.Position - position).sqrMagnitude < 5f * 5f)
            {
                lastHeard.Position = position;
                lastHeard.HasArrivedPersonal = false;
                lastHeard.HasArrivedSquad = false;
                lastHeard.HasSeenPersonal = false;
                lastHeard.HasSeenSquad = false;
                updatePlaces(true);
                return lastHeard;
            }

            LastHeardPlace = new EnemyPlace(_enemy.Bot.Player.ProfileId, position, 300f, isGunFire, _enemy.EnemyIPlayer)
            {
                HasArrivedPersonal = arrived,
            };

            addPlace(LastHeardPlace);
            return LastHeardPlace;
        }

        public void UpdateSquadHeardPlace(EnemyPlace place)
        {
            if (LastSquadHeardPlace == null || LastSquadHeardPlace != place)
            {
                LastSquadHeardPlace = place;
                addPlace(place);
            }
        }

        public EnemyPlace LastSeenPlace { get; private set; }
        public EnemyPlace LastHeardPlace { get; private set; }
        public EnemyPlace LastSquadSeenPlace { get; private set; }
        public EnemyPlace LastSquadHeardPlace { get; private set; }

        private void updatePlaces(bool force = false)
        {
            if (force || _nextUpdatePlacesTime < Time.time)
            {
                _nextUpdatePlacesTime = Time.time + 0.5f;
                sortAndClearPlaces(_allEnemyPlaces);

                if (_allEnemyPlaces.Count > 0)
                {
                    TimeSinceLastKnownUpdated = _allEnemyPlaces.First().TimeSincePositionUpdated;
                }
                else if (TimeSinceLastKnownUpdated != float.MaxValue)
                {
                    TimeSinceLastKnownUpdated = float.MaxValue;
                }
            }
        }

        private float _nextUpdatePlacesTime;

        public List<EnemyPlace> AllEnemyPlaces
        {
            get
            {
                updatePlaces();
                return _allEnemyPlaces;
            }
        }

        public float TimeSinceLastKnownUpdated { get; private set; } = float.MaxValue;

        private readonly List<EnemyPlace> _allEnemyPlaces = new List<EnemyPlace>();

        private void sortAndClearPlaces(List<EnemyPlace> places)
        {
            places.RemoveAll(x => x?.ShallClear == true || x == null);
            places.Sort((x, y) => x.TimeSincePositionUpdated.CompareTo(y.TimeSincePositionUpdated));
        }

        public EnemyPlace LastKnownPlace
        {
            get
            {
                EnemyPlace result = null;
                float timeUpdated = float.MaxValue;
                if (LastSeenPlace != null)
                {
                    result = LastSeenPlace;
                    timeUpdated = LastSeenPlace.TimeSincePositionUpdated;
                }
                if (_enemy.IsVisible)
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
                if (result != null 
                    && (_lastLastKnownPlace == null 
                        || _lastLastKnownPlace != result))
                {
                    _lastLastKnownPlace = result;
                    OnNewLastKnown?.Invoke(result);
                }
                return result;
            }
        }

        public Action<EnemyPlace> OnNewLastKnown { get; set; }

        private EnemyPlace _lastLastKnownPlace;
    }
}