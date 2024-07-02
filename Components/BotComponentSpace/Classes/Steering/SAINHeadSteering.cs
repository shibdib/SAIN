namespace SAIN.SAINComponent.Classes.Mover
{
    public class SAINHeadSteering : BotBaseClass, ISAINClass
    {
        public SAINHeadSteering(BotComponent bot) : base(bot)
        {

        }

        public void Init()
        {
            base.SubscribeToPresetChanges(null);
        }

        public void Update()
        {

        }

        public void Dispose()
        {
        }
    }
}