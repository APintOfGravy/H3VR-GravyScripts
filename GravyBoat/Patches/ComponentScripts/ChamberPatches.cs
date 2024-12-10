using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FistVR;
using HarmonyLib;
using UnityEngine;
using GravyScripts.Components;

namespace GravyScripts.Patches
{
    [HarmonyPatch(typeof(FVRFireArmChamber))]
    public static class ChamberPatches
    {
        [HarmonyPatch("Awake")]
        [HarmonyPrefix]
        public static void AwakePatch(FVRFireArmChamber __instance)
        {
            ChamberData data = __instance.gameObject.GetComponent<ChamberData>();
            bool newData = false;
            if (data == null)
            {
                data = __instance.gameObject.AddComponent<ChamberData>();
                newData = true;
            }
            data.DefaultPosition = __instance.transform.localPosition;
            data.DefaultBoltPosition = __instance.transform.localPosition;

            if (newData)
            {
                data.LoadDirection = LoadAngle.FrontFirst;
                data.LoadPivotPoint = LoadTo.Back;
                if (__instance.Firearm != null && __instance.Firearm is Revolver && (__instance.Firearm as Revolver).UsesTroundSystem)
                {
                    data.LoadDirection = LoadAngle.None;
                    data.LoadPivotPoint = LoadTo.None;
                }
                if (ChamberData.FrontLoadedAmmo.Contains(__instance.RoundType))
                {
                    data.LoadDirection = LoadAngle.BackFirst;
                    data.LoadPivotPoint = LoadTo.Center;
                }
            }

            if (__instance.Firearm != null)
            {
                if (__instance.Firearm is BoltActionRifle)
                {
                    __instance.transform.parent = (__instance.Firearm as BoltActionRifle).BoltHandle.BoltActionHandleRoot.transform.parent;
                    data.DefaultPosition = __instance.transform.localPosition;

                }
            }
        }

        [HarmonyPatch("BeginInteraction")]
        [HarmonyPrefix]
        public static bool BeginInteractionPatch(FVRFireArmChamber __instance, FVRViveHand hand)
        {
            if (__instance.IsManuallyExtractable && __instance.IsAccessible && (__instance.IsFull && (__instance.m_round != null)))
            {
                FVRFireArmRound fvrfireArmRound = __instance.EjectRound(__instance.transform.position, Vector3.zero, Vector3.zero, false);

                if (fvrfireArmRound != null)
                {
                    fvrfireArmRound.BeginInteraction(hand);
                    hand.ForceSetInteractable(fvrfireArmRound);
                    __instance.SetRound(null, false);
                    AmmoData roundData = AmmoPatches.AddAmmoData(fvrfireArmRound);
                    roundData.ShouldScan = false;
                    roundData.IsBeingChambered = true;
                    roundData.UnchamberedAgo = 0f;
                    roundData.OrientalTransform.transform.rotation = __instance.transform.rotation;
                    roundData.PivotTarget = __instance.gameObject;
                }
            }

            return false;
        }
    }
}
