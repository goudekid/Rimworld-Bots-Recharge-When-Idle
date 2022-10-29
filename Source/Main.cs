using Verse.AI;
using HarmonyLib;
using Verse;
using RimWorld;

namespace FriendlyBotsDontWander
{
    [StaticConstructorOnStartup]
    public static class Main
    {
        static Main()
        {
            Harmony instance = new Harmony("rimworld.mod.goog.FriendlyBotsDontWander");
            instance.PatchAll();
        }
    }


    [HarmonyPatch(typeof(JobGiver_Wander), "TryGiveJob", null)]
    public static class JobMaker_Patch
    {
        public static void Postfix(Pawn pawn, ref Job __result)
        {
            // if pawn is a bot and that little asshole tries to wander outside so he gets eaten by a mad squirrel, send him to the nearest correctional facility (charger)
            if (pawn.IsColonyMech && pawn.GetMechWorkMode() == MechWorkModeDefOf.Work && pawn.kindDef.defName != "Mech_Militor")
            {
                Building_MechCharger charger = JobGiver_GetEnergy_Charger.GetClosestCharger(pawn, pawn, false);
                if (charger != null && pawn.needs.energy.CurLevelPercentage < pawn.GetMechControlGroup().mechRechargeThresholds.max)
                {
                    __result = JobMaker.MakeJob(JobDefOf.MechCharge, charger);
                }
                else
                {
                    RCellFinder.TryFindNearbyMechSelfShutdownSpot(pawn.Position, pawn, pawn.Map, out var result, allowForbidden: true);
                    __result = JobMaker.MakeJob(JobDefOf.SelfShutdown, result);
                    __result.expiryInterval = 625;
                }
            }
        }
    }

    [HarmonyPatch(typeof(Building_MechCharger), nameof(Building_MechCharger.Tick), null)]
    public static class Building_MechCharger_Patch
    {
        public static void Postfix(Building_MechCharger __instance, ref Pawn ___currentlyChargingMech)
        {

            // every 625 ticks (15 in game minutes), kick the bot off the charger so it will do work if available
            // if none is available, it will just start charging again
            if (Find.TickManager.TicksGame % 625 == 0 && ___currentlyChargingMech != null)
            {
                Log.Message("Trying to stop pawn from charging... " + ___currentlyChargingMech.Name);
                ___currentlyChargingMech.jobs.curDriver.ReadyForNextToil();
                //__instance.StopCharging();
            }
        }
    }
}