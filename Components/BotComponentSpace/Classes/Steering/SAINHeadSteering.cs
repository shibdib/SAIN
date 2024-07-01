namespace SAIN.SAINComponent.Classes.Mover
{
    public class SAINHeadSteering : BotBaseClass, ISAINClass
    {
        public SAINHeadSteering(BotComponent bot) : base(bot)
        {

        }

        public void Init()
        {
            UpdatePresetSettings(SAINPlugin.LoadedPreset);
        }

        public void Update()
        {

        }

        public void Dispose()
        {

        }
    }
}