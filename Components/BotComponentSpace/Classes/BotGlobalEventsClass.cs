using System;

namespace SAIN.SAINComponent.Classes
{
    public class BotGlobalEventsClass : BotBase, IBotClass
    {
        public event Action<BotComponent> OnEnterPeace;

        public event Action<BotComponent> OnExitPeace;

        public event Action<BotComponent, NavGraphVoxelSimple, NavGraphVoxelSimple> OnVoxelChanged;

        public BotGlobalEventsClass(BotComponent sain) : base(sain)
        {
        }

        public void Init()
        {
            Bot.EnemyController.Events.OnPeaceChanged.OnToggle += PeaceChanged;
            Bot.DoorOpener.DoorFinder.OnNewVoxel += onVoxelChange;
        }

        public void Update()
        {
        }

        public void Dispose()
        {
            Bot.EnemyController.Events.OnPeaceChanged.OnToggle -= PeaceChanged;
            Bot.DoorOpener.DoorFinder.OnNewVoxel -= onVoxelChange;
        }

        private void onVoxelChange(NavGraphVoxelSimple newVoxel, NavGraphVoxelSimple oldVoxel)
        {
            OnVoxelChanged?.Invoke(Bot, newVoxel, oldVoxel);
        }

        public void PeaceChanged(bool value)
        {
            if (value)
                OnEnterPeace?.Invoke(Bot);
            else
                OnExitPeace?.Invoke(Bot);
        }
    }
}