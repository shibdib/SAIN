using System.Collections.Generic;
using UnityEngine;

namespace SAIN.SAINComponent.Classes
{
    public class EnemyKnownPlaces
    {
        public EnemyKnownPlaces(SAINEnemy enemy)
        {
            _enemy = enemy;
        }

        private readonly SAINEnemy _enemy;

        public void Update()
        {
            EnemyPlace lastPlace = null;
            if (_nextCheckArrived < Time.time)
            {
                lastPlace = GetPlaceHaventSeenOrArrived();
                _nextCheckArrived = Time.time + 0.25f;
                if (lastPlace != null && lastPlace.HasArrived == false)
                {
                    float sqrMag = (_enemy.SAIN.Position - lastPlace.Position).sqrMagnitude;
                    if (sqrMag < 1f)
                    {
                        lastPlace.HasArrived = true;
                    }
                }
            }
            if (_nextCheckSeen < Time.time)
            {
                _nextCheckSeen = Time.time + 1f;

                if (lastPlace == null)
                {
                    lastPlace = GetPlaceHaventSeenOrArrived();
                }

                if (lastPlace != null && lastPlace.HasSeen == false)
                {
                    Vector3 lastknown = lastPlace.Position;
                    Vector3 botPos = _enemy.SAIN.Person.Transform.Head;
                    Vector3 direction = lastknown - botPos;
                    lastPlace.HasSeen = !Physics.Raycast(botPos, direction, direction.magnitude, LayerMaskClass.HighPolyWithTerrainMaskAI);
                }
            }
        }

        private EnemyPlace GetPlaceHaventSeenOrArrived()
        {
            for (int i = KnownPlaces.Count - 1; i >= 0; i--)
            {
                EnemyPlace enemyPlace = KnownPlaces[i];
                if (enemyPlace.HasSeen == false || enemyPlace.HasArrived == false)
                {
                    return enemyPlace;
                }
            }
            return null;
        }

        private float _nextCheckArrived;
        private float _nextCheckSeen;

        public void AddPosition(Vector3 position, bool arrived = false, bool seen = false)
        {
            EnemyPlace lastKnown = LastKnownPlace;
            if (lastKnown != null)
            {
                float sqrMag = (lastKnown.Position - position).sqrMagnitude;
                if (sqrMag < 2)
                {
                    lastKnown.Position = position;
                    if (arrived)
                    {
                        lastKnown.HasArrived = true;
                    }
                    if (seen)
                    {
                        lastKnown.HasSeen = true;
                    }
                    return;
                }
            }

            lastKnown = new EnemyPlace(position);
            if (arrived)
            {
                lastKnown.HasArrived = true;
            }
            if (seen)
            {
                lastKnown.HasSeen = true;
            }

            KnownPlaces.Add(lastKnown);
        }


        public List<EnemyPlace> KnownPlaces = new List<EnemyPlace>(5);
        public EnemyPlace LastKnownPlace => KnownPlaces.Count > 0 ? KnownPlaces[KnownPlaces.Count - 1] : null;
    }
}