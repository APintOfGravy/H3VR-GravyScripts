using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HarmonyLib;
using FistVR;
using UnityEngine;
using GravyScripts.Components;

namespace GravyScripts
{
    [HarmonyPatch(typeof(BoltActionRifle))]
    public static class BoltActionPatches
    {
        static Dictionary<BoltActionRifle, bool> isChamberingFromMag = [];
        [HarmonyPatch("UpdateBolt")]
        [HarmonyPrefix]
        public static bool UpdateInteractionPrePatch(BoltActionRifle __instance, BoltActionRifle_Handle.BoltActionHandleState State, float lerp, bool isCatchHeld)
        {
            ChamberData chamberData = __instance.Chamber.GetComponent<ChamberData>();
            __instance.CurBoltHandleState = State;

            isChamberingFromMag[__instance] = false;

            if (__instance.CurBoltHandleState == BoltActionRifle_Handle.BoltActionHandleState.Forward && __instance.LastBoltHandleState != BoltActionRifle_Handle.BoltActionHandleState.Forward)
            {
                __instance.Chamber.transform.parent = __instance.BoltHandle.BoltActionHandleRoot.transform.parent;
                __instance.Chamber.transform.localPosition = chamberData.DefaultPosition;
            }
            else if (__instance.CurBoltHandleState == BoltActionRifle_Handle.BoltActionHandleState.Mid && __instance.LastBoltHandleState == BoltActionRifle_Handle.BoltActionHandleState.Rear && __instance.Magazine != null)
            {
                if (!__instance.m_proxy.IsFull && __instance.Magazine.HasARound() && !__instance.Chamber.IsFull)
                {
                    isChamberingFromMag[__instance] = true;
                    __instance.Chamber.transform.parent = __instance.BoltHandle.BoltActionHandleRoot.transform;
                    __instance.Chamber.transform.localPosition = chamberData.DefaultBoltPosition;
                }
            }

            return true;
        }

        [HarmonyPatch("UpdateBolt")]
        [HarmonyPostfix]
        public static void UpdateInteractionPostPatch(BoltActionRifle __instance, BoltActionRifle_Handle.BoltActionHandleState State, float lerp, bool isCatchHeld)
        {
            __instance.CurBoltHandleState = State;

            if (isChamberingFromMag[__instance])
            {
                __instance.Chamber.ProxyRound.transform.position = __instance.Extraction_ChamberPos.position;
                __instance.Chamber.ProxyRound.transform.rotation = __instance.Extraction_ChamberPos.rotation;
            }
        }
    }
}
