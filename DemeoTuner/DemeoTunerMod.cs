using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Boardgame;
using Boardgame.BoardEntities;
using Boardgame.BoardEntities.Abilities;
using Boardgame.BoardgameActions;
using Boardgame.Data;
using Boardgame.GameplayEffects;
using Boardgame.NonVR.Save;
using Boardgame.Sequence;
using Boardgame.SerializableEvents;
using Boardgame.Social;
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
        public const string Version = "1.2";
        public const string DownloadLink = "https://github.com/IDizor/DemeoTuner/releases";
    }

    public class DemeoTunerMod : MelonMod
    {
        internal static readonly MelonLogger.Instance Logger = new MelonLogger.Instance(nameof(DemeoTuner));
        private static bool useChessboardDistance = false;
        private static int extraEnemiesSpawnCounter = 0;
        
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
                {
                    if (abilityStore.TryGetValue(AbilityKey.Arrow, out Ability ability))
                    {
                        ability.abilityDamage.targetDamage = Settings.Hunter_Arrow_TargetDamage;
                        ability.abilityDamage.critDamage = Settings.Hunter_Arrow_CritDamage;
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
        
        /// <summary>
        /// Applies settings for heroes.
        /// </summary>
        [HarmonyPatch(typeof(Piece), "CreatePiece")]
        public class Piece_CreatePiece
        {
            public static void Postfix(ref Piece __result)
            {
                ApplyHeroSettings(__result, false);
            }
        }

        /// <summary>
        /// Applies settings for heroes when game loaded from save.
        /// </summary>
        [HarmonyPatch(typeof(Piece), "Deserialize")]
        public class Piece_Deserialize
        {
            public static void Postfix(ref Piece __result)
            {
                ApplyHeroSettings(__result, true);
            }
        }

        /// <summary>
        /// Allows reconnected players to make a turn.
        /// </summary>
        [HarmonyPatch(typeof(Piece), "EnableEffectState")]
        public class Piece_EnableEffectState
        {
            public static bool Prefix(EffectStateType effectState, ref bool __result)
            {
                if (Settings.AllowReconnectedPlayersToPlayCurrentTurn &&
                    effectState == EffectStateType.SummoningSickness &&
                    GetCallerMethodName() == nameof(BoardgameActionRecreateReconnectedPlayerPiece.RecreatePlayerPiece))
                {
                    __result = true;
                    return false;
                }
                return true;
            }
        }

        /// <summary>
        /// Counts spawn events for extra enemy groups.
        /// </summary>
        [HarmonyPatch(typeof(SerializableEventUpdateFogAndSpawn), "CreateBoardgameAction")]
        public class SerializableEventUpdateFogAndSpawn_CreateBoardgameAction
        {
            public static bool Prefix(SerializableEventUpdateFogAndSpawn __instance, ref BoardgameAction __result)
            {
                if (Settings.ExtraEnemiesSpawnLimit >= 0)
                {
                    extraEnemiesSpawnCounter++;
                    if (extraEnemiesSpawnCounter > Settings.ExtraEnemiesSpawnLimit)
                    {
                        var gameContext = (GameContext)AccessTools.Field(typeof(SerializableEventUpdateFogAndSpawn), "gameContext").GetValue(__instance);
                        __result = new BoardgameActionUpdateFog(gameContext, __instance.RandomSeed);
                        return false;
                    }
                }
                return true;
            }
        }

        /// <summary>
        /// Resets spawn counter for extra enemy grops.
        /// </summary>
        [HarmonyPatch(typeof(PlayWithFriendsController), "OnEnterGameState")]
        public class PlayWithFriendsController_OnEnterGameState
        {
            public static void Postfix(GameStates newState)
            {
                if (newState == GameStates.PlayingState)
                {
                    extraEnemiesSpawnCounter = 0;
                }
            }
        }

        /*
        /// <summary>
        /// Saves heroes to files.
        /// </summary>
        [HarmonyPatch(typeof(GameDataAPI), "Init")]
        public class GameDataAPI_Init
        {
            public static void Postfix(ref GameDataAPI __instance)
            {
                foreach (var p in __instance.PieceConfig[GameConfigType.Elven])
                {
                    if (p.Key.ToString().StartsWith("Hero"))
                    {
                        System.IO.File.WriteAllText($"D:\\_{p.Key}.json", JsonHelper.Serialize(p.Value, 3));
                    }
                }
            }
        }
        */

        /*
        /// <summary>
        /// Invulnerable mode.
        /// </summary>
        [HarmonyPatch(typeof(EffectSink), "SubtractHealth")]
        public class EffectSink_SubtractHealth
        {
            public static void Prefix(ref float amount, int ___ownerPieceId)
            {
                var piece = StatusEffect.pieceAndTurnController.GetPiece(___ownerPieceId);
                if (piece != null && piece.IsPlayer())
                {
                    amount = -1;
                }
            }
        }
        */

        private static void ApplyHeroSettings(Piece hero, bool isDeserialize)
        {
            var stats = hero.effectSink;
            stats.TryGetStatMaxUnscaled(Stats.Type.Health, out int maxHP);

            switch (hero.boardPieceId)
            {
                case BoardPieceId.HeroGuardian:
                    stats.TrySetStatMaxValue(Stats.Type.Health,         Math.Max(Settings.Guardian_Health, maxHP));
                    stats.TrySetStatBaseValue(Stats.Type.MoveRange,     Settings.Guardian_MoveRange);
                    stats.TrySetStatBaseValue(Stats.Type.AttackDamage,  Settings.Guardian_AttackDamage);
                    stats.TrySetStatBaseValue(Stats.Type.CritDamage,    Settings.Guardian_CritDamage);
                    if (!isDeserialize)
                    {
                        stats.TrySetStatBaseValue(Stats.Type.Health,    Settings.Guardian_Health);
                    }
                    break;
                case BoardPieceId.HeroHunter:
                    stats.TrySetStatMaxValue(Stats.Type.Health,         Math.Max(Settings.Hunter_Health, maxHP));
                    stats.TrySetStatBaseValue(Stats.Type.MoveRange,     Settings.Hunter_MoveRange);
                    stats.TrySetStatBaseValue(Stats.Type.AttackDamage,  Settings.Hunter_AttackDamage);
                    stats.TrySetStatBaseValue(Stats.Type.CritDamage,    Settings.Hunter_CritDamage);
                    if (!isDeserialize)
                    {
                        stats.TrySetStatBaseValue(Stats.Type.Health,    Settings.Hunter_Health);
                    }
                    break;
                case BoardPieceId.HeroRogue:
                    stats.TrySetStatMaxValue(Stats.Type.Health,         Math.Max(Settings.Rogue_Health, maxHP));
                    stats.TrySetStatBaseValue(Stats.Type.MoveRange,     Settings.Rogue_MoveRange);
                    stats.TrySetStatBaseValue(Stats.Type.AttackDamage,  Settings.Rogue_AttackDamage);
                    stats.TrySetStatBaseValue(Stats.Type.CritDamage,    Settings.Rogue_CritDamage);
                    if (!isDeserialize)
                    {
                        stats.TrySetStatBaseValue(Stats.Type.Health,    Settings.Rogue_Health);
                    }
                    break;
                case BoardPieceId.HeroSorcerer:
                    stats.TrySetStatMaxValue(Stats.Type.Health,         Math.Max(Settings.Sorcerer_Health, maxHP));
                    stats.TrySetStatBaseValue(Stats.Type.MoveRange,     Settings.Sorcerer_MoveRange);
                    stats.TrySetStatBaseValue(Stats.Type.AttackDamage,  Settings.Sorcerer_AttackDamage);
                    stats.TrySetStatBaseValue(Stats.Type.CritDamage,    Settings.Sorcerer_CritDamage);
                    if (!isDeserialize)
                    {
                        stats.TrySetStatBaseValue(Stats.Type.Health,    Settings.Sorcerer_Health);
                    }
                    break;
                case BoardPieceId.HeroBard:
                    stats.TrySetStatMaxValue(Stats.Type.Health,         Math.Max(Settings.Bard_Health, maxHP));
                    stats.TrySetStatBaseValue(Stats.Type.MoveRange,     Settings.Bard_MoveRange);
                    stats.TrySetStatBaseValue(Stats.Type.AttackDamage,  Settings.Bard_AttackDamage);
                    stats.TrySetStatBaseValue(Stats.Type.CritDamage,    Settings.Bard_CritDamage);
                    if (!isDeserialize)
                    {
                        stats.TrySetStatBaseValue(Stats.Type.Health,    Settings.Bard_Health);
                    }
                    break;
                case BoardPieceId.HeroWarlock:
                    stats.TrySetStatMaxValue(Stats.Type.Health,         Math.Max(Settings.Warlock_Health, maxHP));
                    stats.TrySetStatBaseValue(Stats.Type.MoveRange,     Settings.Warlock_MoveRange);
                    stats.TrySetStatBaseValue(Stats.Type.AttackDamage,  Settings.Warlock_AttackDamage);
                    stats.TrySetStatBaseValue(Stats.Type.CritDamage,    Settings.Warlock_CritDamage);
                    if (!isDeserialize)
                    {
                        stats.TrySetStatBaseValue(Stats.Type.Health,    Settings.Warlock_Health);
                    }
                    break;
                case BoardPieceId.HeroBarbarian:
                    stats.TrySetStatMaxValue(Stats.Type.Health,         Math.Max(Settings.Barbarian_Health, maxHP));
                    stats.TrySetStatBaseValue(Stats.Type.MoveRange,     Settings.Barbarian_MoveRange);
                    stats.TrySetStatBaseValue(Stats.Type.AttackDamage,  Settings.Barbarian_AttackDamage);
                    stats.TrySetStatBaseValue(Stats.Type.CritDamage,    Settings.Barbarian_CritDamage);
                    if (!isDeserialize)
                    {
                        stats.TrySetStatBaseValue(Stats.Type.Health,    Settings.Barbarian_Health);
                    }
                    break;
            }
        }

        private static string GetCallerMethodName(int index = 0)
        {
            index += 3;
            var stackTrace = new StackTrace();
            if (stackTrace.FrameCount < index)
            {
                return string.Empty;
            }
            return stackTrace.GetFrame(index).GetMethod().Name;
        }

        private static Type GetCallerType(int index = 0)
        {
            index += 3;
            var stackTrace = new StackTrace();
            if (stackTrace.FrameCount < index)
            {
                return typeof(Type);
            }
            return stackTrace.GetFrame(index).GetMethod().DeclaringType;
        }

        private static string GetCallStackPath()
        {
            var stackTrace = new StackTrace();
            var path = string.Join(" <-- ", stackTrace.GetFrames()
                .Skip(3)
                .Select(f => f.GetMethod())
                .Select(m => m.DeclaringType.Name + "." + m.Name + "()"));
            return path;
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
