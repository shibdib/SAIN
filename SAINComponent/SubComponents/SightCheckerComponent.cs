using EFT;
using SAIN.SAINComponent.Classes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Profiling;
using static UnityEngine.EventSystems.EventTrigger;

namespace SAIN.SAINComponent.SubComponents
{
    public class SightCheckerComponent : MonoBehaviour
    {
        private BotOwner BotOwner;
        private SAINComponentClass SAIN;

        private void Awake()
        {
            BotOwner = GetComponent<BotOwner>();
            SAIN = GetComponent<SAINComponentClass>();
        }

        private void Update()
        {

        }

        public void Dispose()
        {
            _nextCheckTimes.Clear();
            Destroy(this);
        }

        public bool SimpleSightCheck(Vector3 target, Vector3 source)
        {
            Vector3 direction = target - source;
            return !Physics.Raycast(source, direction, direction.magnitude, LayerMaskClass.HighPolyWithTerrainMask);
        }

        public bool CheckLineOfSight(Player player)
        {
            PlayerSightResult info = GetInfo(player);
            if (info == null)
            {
                return false;
            }

            if (info.NextCheckTime < Time.time)
            {
                info.NextCheckTime = Time.time + 0.1f;
                info.PartHits = 0;
                info.TotalParts = 0;
                info.InSight = false;

                foreach (var part in player.MainParts.Values)
                {
                    info.TotalParts++;

                    Vector3 headPos = BotOwner.LookSensor._headPoint;
                    Vector3 direction = part.Position - headPos;
                    if (!Physics.Raycast(headPos, direction, direction.magnitude, LayerMaskClass.HighPolyWithTerrainMask))
                    {
                        info.PartHits++;
                    }
                }
                info.InSight = info.PartHits > 0;
            }
            return info.InSight;
        }

        private PlayerSightResult GetInfo(Player player)
        {
            if (player == null) return null;

            if (!_nextCheckTimes.ContainsKey(player.ProfileId))
            {
                _nextCheckTimes.Add(player.ProfileId, new PlayerSightResult());
                player.OnPlayerDeadOrUnspawn += RemoveInfo;
            }
            return _nextCheckTimes[player.ProfileId];
        }

        private void RemoveInfo(Player player)
        {
            if (player != null)
            {
                _nextCheckTimes.Remove(player.ProfileId);
                player.OnPlayerDeadOrUnspawn -= RemoveInfo;
            }
        }

        private readonly Dictionary<string, PlayerSightResult> _nextCheckTimes = new Dictionary<string, PlayerSightResult>();
    }

    public sealed class PlayerSightResult
    {
        public float NextCheckTime;
        public int PartHits;
        public int TotalParts;
        public bool InSight;
    }
}
