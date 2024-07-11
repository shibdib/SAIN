using SAIN.SAINComponent.Classes.EnemyClasses;
using UnityEngine;

namespace SAIN.Components.BotComponentSpace.Classes.EnemyClasses
{
    public struct HearingReport
    {
        public Vector3 position;
        public SAINSoundType soundType;
        public EEnemyPlaceType placeType;
        public bool isDanger;
        public bool shallReportToSquad;
    }
}
