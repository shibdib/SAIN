using Comfort.Common;
using SAIN.Helpers;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UIElements;

namespace SAIN.SAINComponent.Classes
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
            foreach (var obj in GUIObjects)
            {
                DebugGizmos.DestroyLabel(obj.Value);
            }
            GUIObjects?.Clear();
        }

        public void Update()
        {
            if (_nextCheckArrived < Time.time)
            {
                _nextCheckArrived = Time.time + 0.25f; 
                checkIfArrived();
            }
            if (_nextCheckSeen < Time.time)
            {
                _nextCheckSeen = Time.time + 0.5f;
                _enemy.SAIN.StartCoroutine(checkVisionOnPlaces());
            }

            if (SAINPlugin.DebugMode)
            {
                var enemyPlaces = EnemyPlaces;
                foreach (var obj in GUIObjects)
                {
                    if (!enemyPlaces.Contains(obj.Key))
                    {
                        _debugPlacesToRemove.Add(obj.Key);
                    }
                }
                foreach (var debugPlace in _debugPlacesToRemove)
                {
                    DebugGizmos.DestroyLabel(GUIObjects[debugPlace]);
                    GUIObjects.Remove(debugPlace);
                }
                _debugPlacesToRemove.Clear();

                foreach (var place in enemyPlaces)
                {
                    if (!GUIObjects.ContainsKey(place))
                    {
                        GUIObjects.Add(place, new GUIObject());
                    }
                    GUIObject obj = GUIObjects[place];
                    UpdateDebugString(place, obj);
                    DebugGizmos.AddGUIObject(obj);
                }
            }
        }

        private void checkIfArrived()
        {
            Vector3 myPosition = _enemy.SAIN.Position;
            foreach (var place in EnemyPlaces)
            {
                if (place.HasArrived == false && (myPosition - place.Position).sqrMagnitude < 2f)
                {
                    place.HasArrived = true;
                    _enemy.SAIN.Talk.GroupSay(EFTMath.RandomBool() ? EPhraseTrigger.Clear : EPhraseTrigger.LostVisual, null, true, 40);
                }
            }
        }

        private IEnumerator checkVisionOnPlaces()
        {
            Vector3 lookSensor = _enemy.BotOwner.LookSensor._headPoint;
            List<EnemyPlace> places = new List<EnemyPlace>();
            places.AddRange(EnemyPlaces);
            foreach (var place in places)
            {
                if (!place.HasSeen)
                {
                    Vector3 lastknown = place.Position + Vector3.up;
                    Vector3 direction = lastknown - lookSensor;
                    bool canSee = place.ClearLineOfSight(lookSensor, LayerMaskClass.HighPolyWithTerrainMaskAI);
                    if (canSee)
                    {
                        _enemy.SAIN?.Talk.GroupSay(EFTMath.RandomBool() ? EPhraseTrigger.Clear : EPhraseTrigger.LostVisual, null, true, 40);
                    }
                    yield return null;
                }
            }
        }

        private readonly List<EnemyPlace> _debugPlacesToRemove = new List<EnemyPlace>();

        private void UpdateDebugString(EnemyPlace place, GUIObject obj)
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

            stringBuilder.AppendLine($"Arrived? [{place.HasArrived}]" 
                + (place.HasArrived ? $"Time Since Arrived: [{Time.time - place.TimeArrived}]" : string.Empty));

            stringBuilder.AppendLine($"Seen? [{place.HasSeen}]"
                + (place.HasSeen ? $"Time Since Seen: [{Time.time - place.TimeSeen}]" : string.Empty));
        }

        private readonly Dictionary<EnemyPlace, GUIObject> GUIObjects = new Dictionary<EnemyPlace, GUIObject>();

        public bool SearchedAllKnownLocations 
        { 
            get 
            { 
                if (_nextCheckSearchTime < Time.time)
                {
                    _nextCheckSearchTime = Time.time + 1f;

                    bool allSearched = true;
                    foreach (var place in EnemyPlaces)
                    {
                        if (!place.HasArrived)
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
            var places = EnemyPlaces;
            for (int i = 0; i < places.Count; i++)
            {
                EnemyPlace enemyPlace = places[i];
                if (!enemyPlace.HasSeen)
                {
                    return enemyPlace;
                }
            }
            return null;
        }

        public EnemyPlace GetPlaceHaventArrived(bool skipTimer = false)
        {
            var places = EnemyPlaces;
            for (int i = 0; i < places.Count; i++)
            {
                EnemyPlace enemyPlace = places[i];
                if (!enemyPlace.HasArrived)
                {
                    return enemyPlace;
                }
            }
            return null;
        }

        private float _nextCheckArrived;
        private float _nextCheckSeen;

        public void AddPosition(Vector3 position, bool seen, bool arrived)
        {
            _searchedAllKnownLocations = false;

            var lastKnown = LastKnownPlace;
            if (lastKnown != null 
                && (lastKnown.Position - position).sqrMagnitude < 3f * 3f)
            {
                lastKnown.Position = position;
                lastKnown.HasArrived = false;
                return;
            }

            var newPlace = new EnemyPlace(position) 
            {
                HasArrived = arrived, 
                HasSeen = seen 
            };

            if (seen)
            {
                LastSeenPlace = newPlace;
                return;
            }

            if (HeardPlaces.Count >= MaxKnownPlaces)
            {
                HeardPlaces.RemoveAt(0);
            }
            HeardPlaces.Add(newPlace);
        }

        private const int MaxKnownPlaces = 10;

        public EnemyPlace LastSeenPlace { get; private set; }
        public List<EnemyPlace> HeardPlaces = new List<EnemyPlace>(MaxKnownPlaces);

        public List<EnemyPlace> EnemyPlaces
        {
            get
            {
                _enemyPlaces.Clear();
                clearHeardPlaces();
                if (HeardPlaces.Count > 0)
                {
                    _enemyPlaces.AddRange(HeardPlaces);
                }
                if (LastSeenPlace != null)
                {
                    _enemyPlaces.Add(LastSeenPlace);
                }
                if (_enemyPlaces.Count > 1)
                {
                    sortByAge(_enemyPlaces);
                }
                return _enemyPlaces;
            }
        }

        private void sortByAge(List<EnemyPlace> places)
        {
            places.Sort((x, y) => x.TimeSincePositionUpdated.CompareTo(y.TimeSincePositionUpdated));
        }

        private readonly List<EnemyPlace> _enemyPlaces = new List<EnemyPlace>();

        private void clearHeardPlaces()
        {
            for (int i = HeardPlaces.Count - 1; i >= 0; i--)
            {
                EnemyPlace heardPlace = HeardPlaces[i];
                if (heardPlace.HasSeen && heardPlace.HasArrived)
                {
                    HeardPlaces.RemoveAt(i);
                }
            }
        }

        public EnemyPlace LastKnownPlace
        {
            get
            {
                var places = EnemyPlaces;
                if (places.Count > 0)
                {
                    return places[0];
                }
                return null;
            }
        }
    }
}