using DrakiaXYZ.BigBrain.Brains;
using EFT;

namespace SAIN.SAINComponent.Classes
{
    public class SAINActivationClass : SAINBase
    {

        public bool SAINLayersActive => ActiveLayer != ESAINLayer.None;

        public ESAINLayer ActiveLayer { get; private set; }

        public void SetActiveLayer(ESAINLayer layer)
        {
            _layer = layer;
        }

        public SAINActivationClass(BotComponent botComponent) : base(botComponent)
        {
        }

        public void Update()
        {
            setActive();
            handlePatrolData();
        }

        private void setActive()
        {
            ActiveLayer = _layer;
        }

        private void handlePatrolData()
        {
            bool paused = BotOwner.PatrollingData?.Status == PatrolStatus.pause;
            bool customLayerActive = BrainManager.IsCustomLayerActive(BotOwner);
            bool sainActive = SAINLayersActive;

            setPatrolData(sainActive, paused, customLayerActive);

            // Verify patrol data is being resumed correctly.
            if (paused &&
                !sainActive &&
                !customLayerActive)
            {
                //string layer = BrainManager.GetActiveLayerName(BotOwner);
                //Logger.LogWarning($"{BotOwner.name} Active Layer: {layer} Patrol data is paused!");
                //BotOwner.PatrollingData?.Unpause();
                //if (!_speedReset)
                //{
                //    _speedReset = true;
                //    resetSpeed();
                //}
            }
        }

        private void setPatrolData(bool value, bool paused, bool customLayerActive)
        {
            // SAIN layers are active, make sure patrol data is paused.
            if (value && !paused)
            {
                _speedReset = false;
                BotOwner.PatrollingData?.Pause();
            }
            // SAIN layers are not active, unpause patrol data
            else if (!value)
            {
                if (!_speedReset)
                {
                    _speedReset = true;
                    resetSpeed();
                }
                if (!customLayerActive && paused)
                {
                    BotOwner.PatrollingData?.Unpause();
                }
            }
        }

        private void resetSpeed()
        {
            BotOwner.SetTargetMoveSpeed(1f);
            BotOwner.Mover.SetPose(1f);
            Bot.Mover.SetTargetMoveSpeed(1f);
            Bot.Mover.SetTargetPose(1f);
        }

        private ESAINLayer _layer;
        private bool _speedReset;
    }
}