using SAIN.Preset.BotSettings.SAINSettings.Categories;
using SAIN.Preset.GlobalSettings;
using SAIN.Preset.Personalities;

namespace SAIN.Preset.BotSettings.SAINSettings
{
    public class SAINSettingsClass : SettingsGroupBase<SAINSettingsClass>
    {
        public BotDifficultySettings Difficulty = new BotDifficultySettings();
        public SAINCoreSettings Core = new SAINCoreSettings();
        public SAINAimingSettings Aiming = new SAINAimingSettings();
        public SAINBossSettings Boss = new SAINBossSettings();
        public SAINChangeSettings Change = new SAINChangeSettings();
        public SAINGrenadeSettings Grenade = new SAINGrenadeSettings();
        public SAINHearingSettings Hearing = new SAINHearingSettings();
        public SAINLaySettings Lay = new SAINLaySettings();
        public SAINLookSettings Look = new SAINLookSettings();
        public SAINMindSettings Mind = new SAINMindSettings();
        public SAINMoveSettings Move = new SAINMoveSettings();
        public SAINPatrolSettings Patrol = new SAINPatrolSettings();
        public SAINScatterSettings Scattering = new SAINScatterSettings();
        public SAINShootSettings Shoot = new SAINShootSettings();

        public override void InitList()
        {
            SettingsList.Clear();
            SettingsList.Add(Difficulty);
            SettingsList.Add(Core);
            SettingsList.Add(Aiming);
            SettingsList.Add(Boss);
            SettingsList.Add(Change);
            SettingsList.Add(Grenade);
            SettingsList.Add(Hearing);
            SettingsList.Add(Lay);
            SettingsList.Add(Look);
            SettingsList.Add(Mind);
            SettingsList.Add(Patrol);
            SettingsList.Add(Scattering);
            SettingsList.Add(Shoot);
        }
    }
}