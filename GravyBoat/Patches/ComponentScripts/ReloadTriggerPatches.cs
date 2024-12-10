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
    public static class ReloadTriggerPatches
    {
        public static List<FireArmMagazineType> RearLoadedMagazines = [
            FireArmMagazineType.mBizon_9x18mm,
            FireArmMagazineType.mMP203
        ];

        public static void NotAPatch(FVRFireArmMagazineReloadTrigger __instance)
        {
            ChamberData data = __instance.gameObject.GetComponent<ChamberData>();
            bool newData = false;
            if (data == null)
            {
                data = __instance.gameObject.AddComponent<ChamberData>();
                newData = true;
            }

            if (newData)
            {
                data.LoadDirection = LoadAngle.BackFirst;
                data.LoadPivotPoint = LoadTo.Front;
                if (RearLoadedMagazines.Contains(__instance.Magazine.MagazineType))
                {
                    data.LoadDirection = LoadAngle.FrontFirst;
                    data.LoadPivotPoint = LoadTo.Back;
                }
                else if (__instance.Magazine.IsDropInLoadable && ! __instance.Magazine.IsIntegrated)
                {
                    data.LoadDirection = LoadAngle.None;
                    data.LoadPivotPoint = LoadTo.None;
                }
                else if (__instance.Magazine.FireArm != null && __instance.Magazine.IsIntegrated)
                {
                    if (__instance.Magazine.FireArm is BoltActionRifle || (__instance.Magazine.FireArm is LeverActionFirearm && (__instance.Magazine.FireArm as LeverActionFirearm).LoadingGateAngleRange.x == (__instance.Magazine.FireArm as LeverActionFirearm).LoadingGateAngleRange.y))
                    {
                        data.LoadDirection = LoadAngle.None;
                        data.LoadPivotPoint = LoadTo.None;
                    }
                    else if (__instance.Magazine.FireArm is TubeFedShotgun || __instance.Magazine.FireArm is LeverActionFirearm)
                    {
                        data.LoadDirection = LoadAngle.FrontFirst;
                        data.LoadPivotPoint = LoadTo.Center;
                    }
                }
                else if (ChamberData.FrontLoadedAmmo.Contains(__instance.Magazine.RoundType))
                {
                    data.LoadDirection = LoadAngle.BackFirst;
                    data.LoadPivotPoint = LoadTo.Front;
                }
            }
        }
    }
}
