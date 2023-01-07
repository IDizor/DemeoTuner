using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;

namespace DemeoTuner
{
    internal static class Settings
    {
        public static bool MenuQuitGameWithNoConfirmation = true;
        public static bool AutoDeleteSavedCheckpoints = false;
        public static bool HealingPotion_NoPenaltyForDowned = true;
        public static bool Bard_CourageShanty_MassEffect = true;
        public static bool Bard_CourageShanty_MassEffect_VisibleAreaOnly = false;
        public static int Bard_CourageShanty_MassEffect_Radius = 4;
        public static int Bard_SongOfRecovery_Radius = 2;
        public static int Bard_SongOfResilience_Radius = 2;

        static Settings()
        {
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
