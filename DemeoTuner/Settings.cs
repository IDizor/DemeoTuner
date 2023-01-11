using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using bln = System.Boolean;

namespace DemeoTuner
{
    internal static class Settings
    {
        public static bln MenuQuitGameWithNoConfirmation = true;
        public static bln AutoDeleteSavedCheckpoints = false;
        public static bln AllowReconnectedPlayersToPlayCurrentTurn = true;
        public static bln HealingPotion_NoPenaltyForDowned = true;
        public static int ExtraEnemiesSpawnLimit = 20;
        
        public static int Guardian_Health = 10;
        public static int Guardian_MoveRange = 4;
        public static int Guardian_AttackDamage = 3;
        public static int Guardian_CritDamage = 6;

        public static int Hunter_Health = 10;
        public static int Hunter_MoveRange = 4;
        public static int Hunter_AttackDamage = 3;
        public static int Hunter_CritDamage = 5;
        public static int Hunter_Arrow_TargetDamage = 3;
        public static int Hunter_Arrow_CritDamage = 6;

        public static int Rogue_Health = 10;
        public static int Rogue_MoveRange = 4;
        public static int Rogue_AttackDamage = 3;
        public static int Rogue_CritDamage = 8;

        public static int Sorcerer_Health = 10;
        public static int Sorcerer_MoveRange = 4;
        public static int Sorcerer_AttackDamage = 2;
        public static int Sorcerer_CritDamage = 5;

        public static int Bard_Health = 10;
        public static int Bard_MoveRange = 4;
        public static int Bard_AttackDamage = 3;
        public static int Bard_CritDamage = 6;
        public static bln Bard_CourageShanty_MassEffect = true;
        public static bln Bard_CourageShanty_MassEffect_VisibleAreaOnly = false;
        public static int Bard_CourageShanty_MassEffect_Radius = 4;
        public static int Bard_SongOfRecovery_Radius = 2;
        public static int Bard_SongOfResilience_Radius = 2;

        public static int Warlock_Health = 10;
        public static int Warlock_MoveRange = 4;
        public static int Warlock_AttackDamage = 2;
        public static int Warlock_CritDamage = 5;

        public static int Barbarian_Health = 10;
        public static int Barbarian_MoveRange = 4;
        public static int Barbarian_AttackDamage = 4;
        public static int Barbarian_CritDamage = 8;

        static Settings()
        {
            // load settings from json file
            var path = Path.ChangeExtension(Assembly.GetExecutingAssembly().Location, "json");
            if (File.Exists(path))
            {
                var settings = JsonConvert.DeserializeObject<Dictionary<string, object>>(File.ReadAllText(path));
                typeof(Settings).GetFields(BindingFlags.Static | BindingFlags.Public).ToList().ForEach(f =>
                {
                    if (settings.TryGetValue(f.Name, out object v))
                        f.SetValue(null, Convert.ChangeType(v, f.FieldType));
                });
            }
        }
    }
}
