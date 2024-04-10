using SAIN.Attributes;
using SAIN.Helpers;
using SAIN.Preset.GlobalSettings.Categories;
using static SAIN.Helpers.JsonUtility;

namespace SAIN.Preset.GlobalSettings
{
    public class GlobalSettingsClass
    {
        public static readonly GlobalSettingsClass Defaults = new GlobalSettingsClass();

        public static GlobalSettingsClass ImportGlobalSettings(SAINPresetDefinition Preset)
        {
            string fileName = FileAndFolderNames[JsonEnum.GlobalSettings];
            string presetsFolder = FileAndFolderNames[JsonEnum.Presets];

            if (!Load.LoadObject(out GlobalSettingsClass result, fileName, presetsFolder, Preset.Name))
            {
                result = new GlobalSettingsClass
                {
                    EFTCoreSettings = EFTCoreSettings.GetCore(),
                };
                SaveObjectToJson(result, fileName, presetsFolder, Preset.Name);
            }

            EFTCoreSettings.UpdateCoreSettings(result.EFTCoreSettings);

            SaveObjectToJson(result, fileName, presetsFolder, Preset.Name);

            return result;
        }

        [Name("General")]
        public GeneralSettings General = new GeneralSettings();

        [Name("Looting Bots")]
        public LootingBotsSettings LootingBots = new LootingBotsSettings();

        [Name("No Bush ESP")]
        public NoBushESPSettings NoBushESP = new NoBushESPSettings();

        [Name("Aiming")]
        public AimSettings Aiming = new AimSettings();

        [Name("Shoot")]
        public ShootSettings Shoot = new ShootSettings();

        [Name("Cover")]
        public CoverSettings Cover = new CoverSettings();

        [Name("Extract")]
        public ExtractSettings Extract = new ExtractSettings();

        [Name("Flashlight")]
        public FlashlightSettings Flashlight = new FlashlightSettings();

        [Name("Personality")]
        public PersonalitySettings Personality = new PersonalitySettings();

        [Name("Mind")]
        public MindSettings Mind = new MindSettings();

        [Name("Hearing")]
        public HearingSettings Hearing = new HearingSettings();

        [Name("Look")]
        public LookSettings Look = new LookSettings();

        [Hidden]
        public EFTCoreSettings EFTCoreSettings = new EFTCoreSettings();
    }
}