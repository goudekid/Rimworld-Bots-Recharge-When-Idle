using Verse.AI;
using HarmonyLib;
using Verse;
using RimWorld;
using System;

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

            
            if (Find.TickManager.TicksGame % 625 == 0 
                && ___currentlyChargingMech != null
                // don't interrupt a bot if it doesnt have at least 10% more than minimum charge OR 90%, whichever is lower (to account for someone choosing to set a minimum charge >90% for some reason).
                && ___currentlyChargingMech.needs.energy.CurLevelPercentage > Math.Min(.90f, (___currentlyChargingMech.GetMechControlGroup().mechRechargeThresholds.min + .10f)))
            {
                float blah = ___currentlyChargingMech.GetMechControlGroup().mechRechargeThresholds.min + .10f;

                //Log.Message("Mech charge: " + ___currentlyChargingMech.needs.energy.CurLevelPercentage);
                //Log.Message("Mech charge threshhold: " + ___currentlyChargingMech.GetMechControlGroup().mechRechargeThresholds.min);
                //Log.Message("Mech charge threshhold after edit: " + blah);


                //Log.Message("Trying to stop pawn from charging... " + ___currentlyChargingMech.Name);
                ___currentlyChargingMech.jobs.curDriver.ReadyForNextToil();
                //__instance.StopCharging();
            }
        }
    }
}