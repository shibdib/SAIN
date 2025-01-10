﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace SAIN.Preset.GlobalSettings.Categories
{
    public class BotBrains
    {
        public static string Parse(Brain brain)
        {
            return brain.ToString();
        }

        public static Brain Parse(string brain)
        {
            if (Enum.TryParse(brain, out Brain result))
            {
                return result;
            }
            return Brain.Assault;
        }

        public static readonly Brain[] AllBrains = Enum.GetValues(typeof(Brain)).Cast<Brain>().ToArray();

        public static readonly List<Brain> AllBrainsList = AllBrains.ToList();

        public static readonly Brain[] Bosses =
        {
            Brain.BossBully,
            Brain.BossGluhar,
            Brain.Knight,
            Brain.BossKojaniy,
            Brain.BossSanitar,
            Brain.Tagilla,
            //Brain.BossZryachiy,
            Brain.Killa,
            Brain.SectantPriest,
            Brain.BossBoar,
            Brain.BossKolontay,
            Brain.BossPartisan,
        };

        public static readonly Brain[] Followers =
        {
            Brain.FollowerBully,
            Brain.FollowerGluharAssault,
            Brain.FollowerGluharProtect,
            Brain.FollowerGluharScout,
            Brain.FollowerKojaniy,
            Brain.FollowerSanitar,
            Brain.TagillaFollower,
            //Brain.Fl_Zraychiy,
            Brain.SectantWarrior,
            Brain.BigPipe,
            Brain.BirdEye,
            Brain.FollowerBoar,
            Brain.FollowerBoarClose1,
            Brain.FollowerBoarClose2,
            Brain.BossBoarSniper,
            Brain.FollowerKolontayAssault,
            Brain.FollowerKolontaySecurity,
        };

        public static readonly Brain[] Goons =
        {
            Brain.Knight,
            Brain.BigPipe,
            Brain.BirdEye,
        };

        public static readonly Brain[] Special =
        {
            Brain.BossTest,
            Brain.Obdolbs,
            Brain.Gifter,
            Brain.CursAssault,
        };
    }
}