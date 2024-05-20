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
            if (isCurrentEnemy)
            {
                checkIfArrived();
                checkIfSeen();
                createDebug();
            }
        }

        private void createDebug()
        {
            if (SAINPlugin.DebugMode)
            {
                var enemyPlaces = AllEnemyPlaces;
                foreach (var obj in _guiObjects)
                {
                    if (!enemyPlaces.Contains(obj.Key))
                    {
                        _debugPlacesToRemove.Add(obj.Key);
                    }
                }
                foreach (var debugPlace in _debugPlacesToRemove)
                {
                    DebugGizmos.DestroyLabel(_guiObjects[debugPlace]);
                    _guiObjects.Remove(debugPlace);
                }
                _debugPlacesToRemove.Clear();

                foreach (var place in enemyPlaces)
                {
                    if (!_guiObjects.ContainsKey(place))
                    {
                        _guiObjects.Add(place, new GUIObject());
                    }
                    GUIObject obj = _guiObjects[place];
                    updateDebugString(place, obj);
                    DebugGizmos.AddGUIObject(obj);
                }
            }
        }

        private void checkIfSeen()
        {
            if (_nextCheckSeenHeardPlaces < Time.time)
            {
                _nextCheckSeenHeardPlaces = Time.time + _checkSeenFreq;

                Vector3 lookSensor = _enemy.BotOwner.LookSensor._headPoint;
                foreach (var place in HeardPlacesPersonal)
                {
                    if (!place.HasSeenPersonal
                        && place.PersonalClearLineOfSight(lookSensor, LayerMaskClass.HighPolyWithTerrainMaskAI))
                    {
                        tryTalk();
                    }
                }
                return;
            }

            if (_nextCheckSquadSeenPlaces < Time.time)
            {
                _nextCheckSquadSeenPlaces = Time.time + _checkSeenFreq;

                Vector3 lookSensor = _enemy.BotOwner.LookSensor._headPoint;
                EnemyPlace place = LastSquadSeenPlace;
                if (place != null
                    && !place.HasSquadSeen
                    && place.SquadClearLineOfSight(lookSensor, LayerMaskClass.HighPolyWithTerrainMaskAI))
                {
                    tryTalk();
                }
                return;
            }
        }

        private void tryTalk()
        {
            _enemy.SAIN?.Talk.GroupSay(EFTMath.RandomBool() ? EPhraseTrigger.Clear : EPhraseTrigger.LostVisual, null, true, 40);
        }

        private float _nextCheckArrivedHeardPlaces;
        private float _nextCheckArrivedSeenPlace;
        private float _nextCheckArrivedSquadPlaces;
        private const float _checkArriveFreq = 0.25f;

        private void checkIfArrived()
        {
            float time = Time.time;
            if (_nextCheckArrivedHeardPlaces < time)
            {
                _nextCheckArrivedHeardPlaces = time + _checkArriveFreq;
                Vector3 myPosition = _enemy.SAIN.Position;

                bool arrivedNew = false;
                foreach (EnemyPlace heardPlace in HeardPlacesPersonal)
                {
                    if (checkPersonalArrived(heardPlace, myPosition) && !arrivedNew)
                        arrivedNew = true;
                }
                if (arrivedNew)
                    tryTalk();

                return;
            }
            if (_nextCheckArrivedSeenPlace < time)
            {
                _nextCheckArrivedSeenPlace = time + _checkArriveFreq;
                Vector3 myPosition = _enemy.SAIN.Position;

                bool arrivedNew = false;
                if (checkPersonalArrived(LastSeenPlace, myPosition) && !arrivedNew)
                    arrivedNew = true;

                if (arrivedNew)
                    tryTalk();

                return;
            }
            if (_nextCheckArrivedSquadPlaces < time)
            {
                _nextCheckArrivedSquadPlaces = time + _checkArriveFreq;
                Vector3 myPosition = _enemy.SAIN.Position;

                bool arrivedNew = false;
                if (checkSquadArrived(LastSquadSeenPlace, myPosition) && !arrivedNew)
                    arrivedNew = true;
                if (checkSquadArrived(LastSquadHeardPlace, myPosition) && !arrivedNew)
                    arrivedNew = true;

                if (arrivedNew)
                    tryTalk();

                return;
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

        public EnemyPlace GetPlaceHaventSeen(bool skipTimer = false)
        {
            var places = AllEnemyPlaces;
            for (int i = 0; i < places.Count; i++)
            {
                EnemyPlace enemyPlace = places[i];
                if (!enemyPlace.HasSeenPersonal)
                {
                    return enemyPlace;
                }
            }
            return null;
        }

        public EnemyPlace GetPlaceHaventArrived(bool skipTimer = false)
        {
            var places = AllEnemyPlaces;
            for (int i = 0; i < places.Count; i++)
            {
                EnemyPlace enemyPlace = places[i];
                if (!enemyPlace.HasArrivedPersonal)
                {
                    return enemyPlace;
                }
            }
            return null;
        }

        private float _nextCheckSeenHeardPlaces;
        private float _nextCheckSquadSeenPlaces;
        private const float _checkSeenFreq = 0.5f;

        public EnemyPlace UpdateSeenPlace(Vector3 position)
        {
            if (LastSeenPlace == null)
            {
                LastSeenPlace = new EnemyPlace(position, 300f, true, _enemy.EnemyIPlayer);
                _allEnemyPlaces.Add(LastSeenPlace);
                updatePlaces(true);
                OnEnemyPlaceAdded?.Invoke(LastSeenPlace, _enemy.SAIN);
            }
            else
            {
                LastSeenPlace.Position = position;
            }
            return LastSeenPlace;
        }

        public void UpdateSquadSeenPlace(EnemyPlace place)
        {
            if (LastSquadSeenPlace != place)
            {
                if (LastSquadSeenPlace == null)
                {
                    LastSquadSeenPlace = place;
                    _allEnemyPlaces.Add(LastSquadSeenPlace);
                }
                else
                {
                    LastSquadSeenPlace.Position = place.Position;
                }
            }
        }

        public Action<EnemyPlace, SAINComponentClass> OnEnemyPlaceAdded;

        public EnemyPlace AddPersonalHeardPlace(Vector3 position, bool arrived, bool gunFire)
        {
            _searchedAllKnownLocations = false;

            var lastHeard = LastHeardPlace;
            if (lastHeard != null
                && (lastHeard.Position - position).sqrMagnitude < 5f * 5f)
            {
                lastHeard.Position = position;
                lastHeard.HasArrivedPersonal = false;
                return lastHeard;
            }

            var newPlace = new EnemyPlace(position, 300f, gunFire, _enemy.EnemyIPlayer)
            {
                HasArrivedPersonal = arrived,
            };

            if (HeardPlacesPersonal.Count >= _maxHeardPlaces)
            {
                HeardPlacesPersonal.RemoveAt(HeardPlacesPersonal.Count - 1);
            }

            HeardPlacesPersonal.Add(newPlace);
            _allEnemyPlaces.Add(newPlace);
            updatePlaces(true);
            OnEnemyPlaceAdded?.Invoke(newPlace, _enemy.SAIN);
            return newPlace;
        }

        public void UpdateSquadHeardPlace(EnemyPlace place)
        {
            if (LastSquadHeardPlace == null)
            {
                _allEnemyPlaces.Add(LastSquadSeenPlace);
            }
            LastSquadHeardPlace = place;
        }

        private const int _maxHeardPlaces = 10;

        public EnemyPlace LastSeenPlace { get; private set; }
        public EnemyPlace LastSquadSeenPlace { get; private set; }
        public EnemyPlace LastSquadHeardPlace { get; private set; }

        public List<EnemyPlace> HeardPlacesPersonal = new List<EnemyPlace>(_maxHeardPlaces);

        private void updatePlaces(bool force = false)
        {
            if (force || _nextUpdatePlacesTime < Time.time)
            {
                _nextUpdatePlacesTime = Time.time + 0.5f;

                clearPlaces(_allEnemyPlaces);
                if (_allEnemyPlaces.Count > 1)
                {
                    sortByAge(_allEnemyPlaces);
                    if (_allEnemyPlaces.Count > _maxHeardPlaces + 3)
                    {
                        _allEnemyPlaces.RemoveAt(_allEnemyPlaces.Count - 1);
                    }
                }

                clearPlaces(HeardPlacesPersonal);
                if (HeardPlacesPersonal.Count > 1)
                {
                    sortByAge(HeardPlacesPersonal);
                    if (HeardPlacesPersonal.Count >= _maxHeardPlaces)
                    {
                        HeardPlacesPersonal.RemoveAt(HeardPlacesPersonal.Count - 1);
                    }
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
        private readonly List<EnemyPlace> _allEnemyPlaces = new List<EnemyPlace>();

        private void sortByAge(List<EnemyPlace> places)
        {
            places.RemoveAll(x => x == null);
            places.Sort((x, y) => x.TimeSincePositionUpdated.CompareTo(y.TimeSincePositionUpdated));
        }
        private void clearPlaces(List<EnemyPlace> places)
        {
            places.RemoveAll(x => x?.ShallClear == true || x == null);
        }

        public EnemyPlace LastKnownPlace
        {
            get
            {
                var places = AllEnemyPlaces;
                return places.Count > 0 ? places.First() : null;
            }
        }

        public EnemyPlace LastHeardPlace
        {
            get
            {
                return HeardPlacesPersonal.Count > 0 ? HeardPlacesPersonal[0] : null;
            }
        }
    }
}