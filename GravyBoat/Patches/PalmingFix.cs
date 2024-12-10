using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using FistVR;
using HarmonyLib;
using OpenScripts2;
using UnityEngine;

namespace GravyScripts
{
    public class PalmingFix
    {
        [HarmonyPatch(typeof(FVRFireArmRound), "DuplicateFromSpawnLock")]
        [HarmonyPrefix]
        public static bool DuplicateFromSpawnLock(FVRFireArmRound __instance, ref GameObject __result, FVRViveHand hand)
        {
            GameObject gameObject = UnityEngine.Object.Instantiate(__instance.ObjectWrapper.GetGameObject(), __instance.Transform.position, __instance.Transform.rotation);
            FVRPhysicalObject fvrPhys = gameObject.GetComponent<FVRPhysicalObject>();
            if (hand != null)
            {
                hand.ForceSetInteractable(fvrPhys);
            }
            fvrPhys.SetQuickBeltSlot(null);
            if (hand != null)
            {
                fvrPhys.BeginInteraction(hand);
            }
            if (__instance.MP.IsMeleeWeapon && fvrPhys.MP.IsThrownDisposable)
            {
                fvrPhys.MP.IsCountingDownToDispose = true;
                if (fvrPhys.MP.m_isThrownAutoAim)
                {
                    fvrPhys.MP.SetReadyToAim(true);
                    fvrPhys.MP.SetPose(__instance.MP.PoseIndex);
                }
            }

            FVRFireArmRound fvrRound = gameObject.GetComponent<FVRFireArmRound>();
            if (GM.Options.ControlOptions.SmartAmmoPalming == ControlOptions.SmartAmmoPalmingMode.Enabled && fvrRound != null && hand.OtherHand.CurrentInteractable != null)
            {
                int num = 0;
                if (hand.OtherHand.CurrentInteractable is FVRFireArm)
                {
                    FVRFireArm fvrfireArm = hand.OtherHand.CurrentInteractable as FVRFireArm;
                    if (fvrfireArm.RoundType == __instance.RoundType)
                    {
                        FVRFireArmMagazine magazine = fvrfireArm.Magazine;
                        if (magazine != null)
                        {
                            num = magazine.m_capacity - magazine.m_numRounds;
                        }
                        for (int i = 0; i < fvrfireArm.GetChambers().Count; i++)
                        {
                            FVRFireArmChamber fvrfireArmChamber = fvrfireArm.GetChambers()[i];
                            if (fvrfireArmChamber.IsManuallyChamberable && (!fvrfireArmChamber.IsFull || fvrfireArmChamber.IsSpent) && fvrfireArmChamber.IsAccessible)
                            {
                                num++;
                            }
                        }
                    }
                }
                else if (hand.OtherHand.CurrentInteractable is FVRFireArmMagazine)
                {
                    FVRFireArmMagazine fvrfireArmMagazine = hand.OtherHand.CurrentInteractable as FVRFireArmMagazine;
                    if (fvrfireArmMagazine.RoundType == __instance.RoundType)
                    {
                        num = fvrfireArmMagazine.m_capacity - fvrfireArmMagazine.m_numRounds;
                    }
                }
                else if (hand.OtherHand.CurrentInteractable is Speedloader)
                {
                    Speedloader speedloader = hand.OtherHand.CurrentInteractable as Speedloader;
                    if (speedloader.Chambers[0].Type == __instance.RoundType)
                    {
                        for (int j = 0; j < speedloader.Chambers.Count; j++)
                        {
                            if (!speedloader.Chambers[j].IsLoaded)
                            {
                                num++;
                            }
                        }
                    }
                }
                else if (hand.OtherHand.CurrentInteractable is FVRFireArmClip)
                {
                    FVRFireArmClip fvrfireArmClip = hand.OtherHand.CurrentInteractable as FVRFireArmClip;
                    if (fvrfireArmClip.RoundType == __instance.RoundType)
                    {
                        num = fvrfireArmClip.m_capacity - fvrfireArmClip.m_numRounds;
                    }
                }

                if (num < 1)
                {
                    num = __instance.ProxyRounds.Count;
                }
                int num2 = Mathf.Min(__instance.ProxyRounds.Count, num - 1);
                for (int k = 0; k < num2; k++)
                {
                    fvrRound.AddProxy(__instance.ProxyRounds[k].Class, __instance.ProxyRounds[k].ObjectWrapper);
                }
                fvrRound.UpdateProxyDisplay();

                __result = gameObject;
            }
            else
            {
                for (int l = 0; l < __instance.ProxyRounds.Count; l++)
                {
                    fvrRound.AddProxy(__instance.ProxyRounds[l].Class, __instance.ProxyRounds[l].ObjectWrapper);
                }
                fvrRound.UpdateProxyDisplay();
            }
            return false;
        }
    }
}
