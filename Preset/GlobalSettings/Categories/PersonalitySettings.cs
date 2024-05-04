using SAIN.Attributes;

namespace SAIN.Preset.GlobalSettings
{
    public class PersonalitySettings
    {
        [Default(false)]
        public bool AllGigaChads = false;

        [Default(false)]
        public bool AllChads = false;

        [Default(false)]
        public bool AllRats = false;

        public bool CheckForForceAllPers(out EPersonality result)
        {
            result = EPersonality.Normal;
            if (AllGigaChads)
            {
                result = EPersonality.GigaChad;
                return true;
            }
            if (AllChads)
            {
                result = EPersonality.Chad;
                return true;
            }
            if (AllRats)
            {
                result = EPersonality.Rat;
                return true;
            }
            return false;
        }

        public void Update()
        {
            if (AllGigaChads)
            {
                AllChads = false;
                AllRats = false;
            }
            if (AllChads)
            {
                AllGigaChads = false;
                AllRats = false;
            }
            if (AllRats)
            {
                AllGigaChads = false;
                AllChads = false;
            }
        }
    }
}