using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Boardgame;
using Boardgame.BoardEntities;
using Boardgame.BoardEntities.Abilities;
using Boardgame.Data;
using Boardgame.GameplayEffects;
using Boardgame.NonVR.Save;
using Boardgame.Sequence;
using Boardgame.Ui.LobbyMenu;
using DataKeys;
using HarmonyLib;
using MelonLoader;
using Unity.Mathematics;
using static Boardgame.Ui.LobbyMenu.LobbyMenuController;

namespace DemeoTuner
{
    public static class BuildInfo
    {
        public const string Name = "DemeoTuner";
        public const string Description = "Do not auto-delete saves, mass Courage Shanty, and more.";
        public const string Author = "IDizor";
        public const string Company = "IDizor";
        public const string Version = "1.0.0";
        public const string DownloadLink = "https://github.com/IDizor/DemeoTuner/releases";
    }

    public class DemeoTunerMod : MelonMod
    {
        internal static readonly MelonLogger.Instance Logger = new MelonLogger.Instance(nameof(DemeoTuner));
        private static bool useChessboardDistance = false;
        
        /// <summary>
        /// Initialize mod.
        /// </summary>
        public override void OnInitializeMelon()
        {
            var harmony = new HarmonyLib.Harmony(GetType().ToString());
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }

        /// <summary>
        /// Do not auto-delete saved games.
        /// </summary>
        [HarmonyPatch(typeof(SaveGameManager), "DeleteActiveSave")]
        public class SaveGameManager_DeleteActiveSave
        {
            public static bool Prefix()
            {
                return Settings.AutoDeleteSavedCheckpoints;
            }
        }

        /// <summary>
        /// Allow to save to any slot.
        /// </summary>
        [HarmonyPatch(typeof(NonVrSaveViewController), "IsSlotWriteable")]
        public class NonVrSaveViewController_IsSlotWriteable
        {
            public static bool Prefix(ref bool __result)
            {
                if (!Settings.AutoDeleteSavedCheckpoints)
                {
                    __result = true;
                    return false;
                }
                return true;
            }
        }

        /// <summary>
        /// Quit game with no confirmation.
        /// </summary>
        [HarmonyPatch(typeof(LobbyMenuController), "ShowMainContent")]
        public class LobbyMenuController_ShowMainContent
        {
            public static bool Prefix(MenuContent newContent, LobbyMenuController __instance)
            {
                if (newContent == MenuContent.QuitGame && Settings.MenuQuitGameWithNoConfirmation)
                {
                    var QuitGame = AccessTools.Method(typeof(LobbyMenuController), "QuitGame");
                    QuitGame.Invoke(__instance, null);
                    return false;
                }
                return true;
            }
        }
        
        /// <summary>
        /// Ability patches.
        /// </summary>
        [HarmonyPatch(typeof(AbilityFactory), "Init")]
        public class AbilityFactory_Init
        {
            public static void Postfix()
            {
                var abilityStore = AccessTools.StaticFieldRefAccess<Dictionary<AbilityKey, Ability>>(typeof(AbilityFactory), "abilityStore");

                if (Settings.HealingPotion_NoPenaltyForDowned)
                {
                    if (abilityStore.TryGetValue(AbilityKey.HealingPotion, out Ability ability))
                    {
                        //File.WriteAllText($"D:\\_{ability.abilityKey}.json", JsonHelper.Serialize(ability, 3));
                        ability.abilityHeal = SetStructField(ability.abilityHeal, "overrideIfDowned", false);
                    }
                }
                if (Settings.Bard_CourageShanty_MassEffect)
                {
                    if (abilityStore.TryGetValue(AbilityKey.CourageShanty, out Ability ability))
                    {
                        ability.maxRange = 0;
                        ability.mayTargetSelf = true;
                        ability.areaOfEffectRange = Settings.Bard_CourageShanty_MassEffect_Radius;
                    }
                }
                {
                    if (abilityStore.TryGetValue(AbilityKey.SongOfRecovery, out Ability ability))
                    {
                        ability.areaOfEffectRange = Settings.Bard_SongOfRecovery_Radius;
                    }
                }
                {
                    if (abilityStore.TryGetValue(AbilityKey.SongOfResilience, out Ability ability))
                    {
                        ability.areaOfEffectRange = Settings.Bard_SongOfResilience_Radius;
                    }
                }
            }
        }

        /// <summary>
        /// Updates CourageShanty effects on targets in ability area.
        /// </summary>
        [HarmonyPatch(typeof(StrengthenCourage), "BuildSequence")]
        public class StrengthenCourage_BuildSequence
        {
            public static bool Prefix(StrengthenCourage __instance, ref ISequenceNode __result, AbilityContext abilityContext, GameContext gameContext, ref EffectStateType[] ___orderedEffects, ref int ___orderedEffectsLength)
            {
                if (!Settings.Bard_CourageShanty_MassEffect)
                {
                    return true;
                }

                ___orderedEffectsLength = ___orderedEffects.Length;
                if (___orderedEffectsLength == 0)
                {
                    __result = new LambdaSequenceNode(delegate {});
                    return false;
                }
                var areaOfEffect = (MoveSet)AccessTools.Field(typeof(AbilityContext), "areaOfEffect").GetValue(abilityContext);
                var UpdateEffectsOnTarget = AccessTools.Method(typeof(StrengthenCourage), "UpdateEffectsOnTarget");
                __result = new LambdaSequenceNode(delegate
                {
                    List<Piece> pieceList = new List<Piece>();
                    areaOfEffect.ForEachAction(delegate (IntPoint2D tile, ActionType actionType, int distance)
                    {
                        Piece piece = gameContext.pieceAndTurnController.FindPieceWithPosition(tile);
                        if (piece != null)
                        {
                            pieceList.Add(piece);
                        }
                    });
                    foreach (Piece piece in pieceList)
                    {
                        UpdateEffectsOnTarget.Invoke(__instance, new object[] { new Target(piece), abilityContext });
                    }
                });
                return false;
            }
        }

        /// <summary>
        /// Forces to use chessboard distance during current Ability.CreateAreaOfEffect() call.
        /// </summary>
        [HarmonyPatch(typeof(Ability), "CreateAreaOfEffect")]
        public class Ability_CreateAreaOfEffect
        {
            public static void Prefix(Ability __instance)
            {
                useChessboardDistance = Settings.Bard_CourageShanty_MassEffect
                    && !Settings.Bard_CourageShanty_MassEffect_VisibleAreaOnly
                    && __instance.abilityKey == AbilityKey.CourageShanty;
            }
            public static void Postfix()
            {
                useChessboardDistance = false;
            }
        }

        /// <summary>
        /// Uses simple chessboard distance math in VisibilityCalculator.
        /// </summary>
        [HarmonyPatch(typeof(VisibilityCalculator), "GetViewDistance", new[] { typeof(IntPoint2D) })]
        public class VisibilityCalculator_GetViewDistance
        {
            public static bool Prefix(IntPoint2D tile, int3[] ___viewers, ref int __result)
            {
                if (useChessboardDistance && ___viewers.Length > 0)
                {
                    __result = IntPoint2D.ChessboardDistance(tile, new IntPoint2D(___viewers[0].x, ___viewers[0].y));
                    return false;
                }
                return true;
            }
        }
        
        private static string GetCallerMethodName(int index = 3)
        {
            var stackTrace = new StackTrace();
            return stackTrace.GetFrame(index).GetMethod().Name;
        }

        private static KeyValuePair<string, object>[] GetFieldsValues<T>(T obj)
        {
            var fields = AccessTools.GetDeclaredFields(typeof(T))
                .Select(f => new KeyValuePair<string, object>(f.Name, f.GetValue(obj)))
                .ToArray();
            return fields;
        }

        private static TStruct SetStructField<TStruct>(TStruct @struct, string fieldName, object value)
        {
            var fieldInfo = AccessTools.Field(typeof(TStruct), fieldName);
            object boxed = @struct;
            fieldInfo.SetValue(boxed, value);
            return (TStruct)boxed;
        }
    }
}
