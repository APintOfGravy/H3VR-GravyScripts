using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HarmonyLib;
using FistVR;
using UnityEngine;
using OpenScripts2;
using GravyScripts.Components;

namespace GravyScripts
{
    [HarmonyPatch(typeof(Revolver))]
    public static class RevolverPatch
    {
        public static Dictionary<RevolverCylinder, Vector3> cylinderVelocity = [];

        [HarmonyPatch("UpdateCylinderRelease")]
        [HarmonyPrefix]
        public static bool UpdateCylinderReleasePatch(Revolver __instance)
        {
            RevolverData revolverData = __instance.gameObject.GetComponent<RevolverData>();
            Vector3 curCylVelocity = (__instance.transform.position - cylinderVelocity[__instance.Cylinder]) / Time.deltaTime;

            // "Wait a minute, this looks suspiciously like Revolver.UpdateCylinderRelease, but with 'this' replaced with '__instance'."
            // You're entirely correct. Transpiler patches can fuck off and this is the next-best thing. I'm sorry Anton.
            __instance.m_isCylinderReleasePressed = false;
            if (!__instance.IsAltHeld && (!__instance.m_isHammerLocked || __instance.DoesFiringRecock || __instance.UsesTroundSystem))
            {
                if (__instance.m_hand.IsInStreamlinedMode)
                {
                    if (__instance.m_hand.Input.BYButtonPressed)
                    {
                        __instance.m_isCylinderReleasePressed = true;
                    }
                }
                else if (__instance.m_hand.Input.TouchpadPressed && Vector2.Angle(__instance.TouchPadAxes, Vector2.left) < 45f)
                {
                    __instance.m_isCylinderReleasePressed = true;
                }
            }
            if (__instance.CylinderReleaseButton != null)
            {
                if (__instance.isCyclinderReleaseARot)
                {
                    if (!__instance.m_isCylinderReleasePressed)
                    {
                        __instance.m_curCyclinderReleaseRot = Mathf.Lerp(__instance.m_curCyclinderReleaseRot, __instance.CylinderReleaseButtonForwardPos.x, Time.deltaTime * 3f);
                    }
                    else
                    {
                        __instance.m_curCyclinderReleaseRot = Mathf.Lerp(__instance.m_curCyclinderReleaseRot, __instance.CylinderReleaseButtonRearPos.x, Time.deltaTime * 3f);
                    }
                    __instance.CylinderReleaseButton.localEulerAngles = new Vector3(__instance.m_curCyclinderReleaseRot, 0f, 0f);
                }
                else if (__instance.m_isCylinderReleasePressed)
                {
                    __instance.CylinderReleaseButton.localPosition = Vector3.Lerp(__instance.CylinderReleaseButton.localPosition, __instance.CylinderReleaseButtonForwardPos, Time.deltaTime * 3f);
                }
                else
                {
                    __instance.CylinderReleaseButton.localPosition = Vector3.Lerp(__instance.CylinderReleaseButton.localPosition, __instance.CylinderReleaseButtonRearPos, Time.deltaTime * 3f);
                }
            }
            if (__instance.UsesHoldingLatch)
            {
                float num;
                if (__instance.m_isCylinderReleasePressed || !__instance.m_isCylinderArmLocked)
                {
                    num = Mathf.MoveTowards(__instance.m_holdingLatchLerp, 1f, Time.deltaTime * 10f);
                }
                else
                {
                    num = Mathf.MoveTowards(__instance.m_holdingLatchLerp, 0f, Time.deltaTime * 10f);
                }
                if (Mathf.Abs(__instance.m_holdingLatchLerp - num) > 0.001f)
                {
                    __instance.m_holdingLatchLerp = num;
                    __instance.SetAnimatedComponent(__instance.HoldingLatch, Mathf.Lerp(__instance.HoldingLatchRange.x, __instance.HoldingLatchRange.y, __instance.m_holdingLatchLerp), __instance.HoldingLatchInterp, __instance.HoldingLatchAxis);
                }
            }
            if (__instance.m_isCylinderReleasePressed)
            {
                __instance.m_isCylinderArmLocked = false;
            }
            else
            {
                float num2 = __instance.CylinderArm.localEulerAngles.z;
                if (__instance.IsCylinderArmZ)
                {
                    num2 = __instance.CylinderArm.localEulerAngles.x;
                }
                if (__instance.IsCylinderArmY)
                {
                    num2 = __instance.CylinderArm.localEulerAngles.y;
                }
                if (Mathf.Abs(num2) <= 1f && !__instance.m_isCylinderArmLocked)
                {
                    __instance.m_isCylinderArmLocked = true;
                    __instance.CylinderArm.localEulerAngles = Vector3.zero;
                }
            }
            if (__instance.UsesTroundSystem && __instance.m_isCylinderArmLocked)
            {
                int num4 = __instance.CurChamber + 1;
                num4 %= __instance.Cylinder.numChambers;
                if (!__instance.Chambers[num4].IsFull && __instance.Magazine.HasARound())
                {
                    FVRLoadedRound fvrloadedRound = __instance.Magazine.RemoveRound(0);
                    __instance.Chambers[num4].Autochamber(fvrloadedRound.LR_Class);
                }
            }
            JointLimits limits = revolverData.RevolverHinge.limits;
            if (!__instance.m_isCylinderArmLocked)
            {
                if (__instance.Cylinder.IsHeld)
                {
                    revolverData.RevolverHinge.useSpring = true;
                }
                else 
                {
                    revolverData.RevolverHinge.useSpring = false;
                }
                revolverData.RevolverHinge.GetComponent<Rigidbody>().mass = 0.1f;
                limits.min = __instance.CylinderRotRange.x;
                limits.max = __instance.CylinderRotRange.y;
                if (__instance.UsesTroundSystem)
                {
                    /*
                    if (num7 >= __instance.TroundClipArmClamp.x && num7 <= __instance.TroundClipArmClamp.y)
                    {
                        if (!__instance.ClipTrigger.activeSelf)
                        {
                            __instance.ClipTrigger.SetActive(true);
                        }
                    }
                    else if (__instance.ClipTrigger.activeSelf)
                    {
                        __instance.ClipTrigger.SetActive(false);
                    }
                    */
                    if (__instance.Clip != null)
                    {
                        // num7 = Mathf.Clamp(num7, __instance.TroundClipArmClamp.x, __instance.TroundClipArmClamp.y);
                        limits.min = __instance.TroundClipArmClamp.x;
                        limits.max = __instance.TroundClipArmClamp.y;
                    }
                }
            }
            else
            {
                revolverData.RevolverHinge.GetComponent<Rigidbody>().mass = 0.001f;
                limits.min = __instance.CylinderRotRange.x;
                limits.max = __instance.CylinderRotRange.x;
            }
            revolverData.RevolverHinge.limits = limits;
            if (__instance.UsesTroundSystem)
            {
                if (__instance.m_isCylinderArmLocked)
                {
                    if (__instance.TroundMagTrigger.activeSelf)
                    {
                        __instance.TroundMagTrigger.SetActive(false);
                    }
                }
                else if (!__instance.TroundMagTrigger.activeSelf)
                {
                    __instance.TroundMagTrigger.SetActive(true);
                }
            }
            
            float num8 = __instance.CylinderArm.localEulerAngles.z;
            
            if (__instance.IsCylinderArmZ)
            {
                num8 = __instance.CylinderArm.localEulerAngles.x;
            }
            else if (__instance.IsCylinderArmY)
            {
                num8 = __instance.CylinderArm.localEulerAngles.y;
            }
            
            if (num8 > 180f)
            {
                num8 -= 360f;
            }
            if (num8 < -180f)
            {
                num8 += 360f;
            }
            if (Mathf.Abs(num8) > 15f)
            {
                for (int i = 0; i < __instance.Chambers.Length; i++)
                {
                    __instance.Chambers[i].IsAccessible = true;
                }
            }
            else
            {
                for (int j = 0; j < __instance.Chambers.Length; j++)
                {
                    __instance.Chambers[j].IsAccessible = false;
                }
            }
            
            if (__instance.IsCylinderArmZ && Mathf.Abs(num8) < 1f)
            {
                __instance.m_hasEjectedSinceOpening = false;
            }
            
            if (__instance.IsCylinderArmZ && Mathf.Abs(num8) > Mathf.Min(45f, __instance.CylinderRotRange.y * 0.8f) && !__instance.m_hasEjectedSinceOpening && !__instance.RequiresManualEject)
            {
                __instance.m_hasEjectedSinceOpening = true;
                if (revolverData.RevolverHinge.velocity > revolverData.EjectSpeed)
                {
                    __instance.EjectChambers();
                }
                if (revolverData.RevolverHinge.velocity != 0)
                {
                    Debug.Log("Hinge velocity was " + revolverData.RevolverHinge.velocity);
                }
            }
            
            if (__instance.IsCylinderArmY && Mathf.Abs(num8) < 1f)
            {
                __instance.m_hasEjectedSinceOpening = false;
            }
            
            if (__instance.IsCylinderArmY && Mathf.Abs(num8) > 26f && !__instance.m_hasEjectedSinceOpening && !__instance.RequiresManualEject)
            {
                __instance.m_hasEjectedSinceOpening = true;
                __instance.EjectChambers();
            }
            
            if (!__instance.IsCylinderArmZ && !__instance.IsCylinderArmY && Mathf.Abs(__instance.CylinderArm.localEulerAngles.z) > 75f && Vector3.Angle(__instance.transform.forward, Vector3.up) <= 120f && !__instance.RequiresManualEject)
            {
                float num9 = __instance.transform.InverseTransformDirection(__instance.m_hand.Input.VelLinearWorld).z;
                if (__instance.AngInvert)
                {
                    num9 = -num9;
                }
                if (num9 < -2f)
                {
                    __instance.EjectChambers();
                }
            }
            
            if (__instance.m_isCylinderArmLocked && !__instance.m_wasCylinderArmLocked)
            {
                if (!__instance.UsesTroundSystem)
                {
                    __instance.m_curChamber = __instance.Cylinder.GetClosestChamberIndex();
                    if (__instance.m_isHammerLocked)
                    {
                        if (!__instance.IsCylinderRotClockwise)
                        {
                            __instance.m_curChamber++;
                            __instance.m_curChamber %= __instance.Cylinder.numChambers;
                        }
                        else
                        {
                            __instance.m_curChamber--;
                            if (__instance.m_curChamber < 0)
                            {
                                __instance.m_curChamber = __instance.Cylinder.numChambers - 1;
                            }
                        }
                    }
                    __instance.Cylinder.transform.localRotation = __instance.Cylinder.GetLocalRotationFromCylinder(__instance.m_curChamber);
                    __instance.m_curChamberLerp = 0f;
                    __instance.m_tarChamberLerp = 0f;
                }
                __instance.PlayAudioEvent(FirearmAudioEventType.BreachClose, 1f);
            }
            
            if (!__instance.m_isCylinderArmLocked && __instance.m_wasCylinderArmLocked)
            {
                __instance.PlayAudioEvent(FirearmAudioEventType.BreachOpen, 1f);
            }
            
            if (__instance.m_isHammerLocked)
            {
                __instance.m_tarChamberLerp = 1f;
            }
            else if (!__instance.m_hasTriggerCycled && __instance.IsDoubleActionTrigger)
            {
                __instance.m_tarChamberLerp = __instance.m_curTriggerFloat * 1.4f;
            }
            
            __instance.m_curChamberLerp = Mathf.Lerp(__instance.m_curChamberLerp, __instance.m_tarChamberLerp, Time.deltaTime * 16f);
            
            if (__instance.UsesTroundSystem)
            {
                if (__instance.m_curChamberLerp > 0.3f && __instance.Chambers[__instance.CurChamber].IsFull)
                {
                    __instance.Chambers[__instance.CurChamber].EjectRound(__instance.TroundEjectPoint.position, __instance.TroundEjectPoint.forward, new Vector3(0f, 120f, 0f), false);
                }
                if (__instance.m_curChamberLerp > 0.8f && __instance.UsesTroundSystem && __instance.isCylinderArmLocked)
                {
                    int num10 = __instance.CurChamber + 2;
                    num10 %= __instance.Cylinder.numChambers;
                    if (!__instance.Chambers[num10].IsFull && __instance.Magazine.HasARound())
                    {
                        FVRLoadedRound fvrloadedRound2 = __instance.Magazine.RemoveRound(0);
                        __instance.Chambers[num10].Autochamber(fvrloadedRound2.LR_Class);
                    }
                }
            }
            
            int num11;
            if (__instance.IsCylinderRotClockwise)
            {
                num11 = (__instance.CurChamber + 1) % __instance.Cylinder.numChambers;
            }
            else
            {
                num11 = (__instance.CurChamber - 1) % __instance.Cylinder.numChambers;
            }
            
            if (__instance.isCylinderArmLocked || __instance.UsesTroundSystem)
            {
                __instance.Cylinder.transform.localRotation = Quaternion.Slerp(__instance.Cylinder.GetLocalRotationFromCylinder(__instance.CurChamber), __instance.Cylinder.GetLocalRotationFromCylinder(num11), __instance.m_curChamberLerp);
            }
            
            __instance.m_wasCylinderArmLocked = __instance.m_isCylinderArmLocked;
            

            return false;
        }

        [HarmonyPatch("Awake")]
        [HarmonyPostfix]
        public static void AwakePatch(Revolver __instance)
        {
            RevolverData revolverData = __instance.gameObject.GetComponent<RevolverData>();
            cylinderVelocity.Add(__instance.Cylinder, __instance.transform.position);

            if (__instance.gameObject.GetComponent<RevolverData>() == null)
            {
                revolverData = __instance.gameObject.AddComponent<RevolverData>();
                revolverData.RevolverHinge = __instance.CylinderArm.gameObject.AddComponent<HingeJoint>();
                revolverData.RevolverHinge.connectedBody = __instance.gameObject.GetComponent<Rigidbody>();
                revolverData.RevolverHinge.axis = new Vector3(0, 0, 1);
                if (__instance.IsCylinderArmZ)
                {
                    revolverData.RevolverHinge.axis = new Vector3(1, 0, 0);
                }
                if (__instance.IsCylinderArmY)
                {
                    revolverData.RevolverHinge.axis = new Vector3(0, 1, 0);
                }
                revolverData.RevolverHinge.useSpring = true;
                JointSpring pivotSpring = revolverData.RevolverHinge.spring;
                pivotSpring.spring = 0.5f;
                pivotSpring.damper = 0.05f;
                revolverData.RevolverHinge.spring = pivotSpring;
                revolverData.RevolverHinge.useLimits = true;
                JointLimits limits = revolverData.RevolverHinge.limits;
                limits.min = __instance.CylinderRotRange.x;
                limits.max = __instance.CylinderRotRange.x;
                revolverData.RevolverHinge.limits = limits;
                revolverData.RevolverHinge.enablePreprocessing = false;

                revolverData.RevolverHinge.GetComponent<Rigidbody>().mass = 0.1f;

                revolverData.RotLimit = __instance.CylinderRotRange.y;
            }

            foreach (FVRFireArmChamber chamber in __instance.Chambers)
            {
                if (chamber.IsManuallyChamberable == true)
                {
                    chamber.IsManuallyExtractable = true;
                }
            }

            RevolverCylinderPatch.CylLocalPosStart.Add(__instance.Cylinder, revolverData.RevolverHinge.transform.localPosition);
        }
    }

    [HarmonyPatch(typeof(RevolverCylinder))]
    public static class RevolverCylinderPatch
    {
        public static Dictionary<RevolverCylinder, Vector3> CylLocalPosStart = [];
        public static Dictionary<RevolverCylinder, Vector3> HandAngOffset = [];
        public static Dictionary<RevolverCylinder, float> OrigAngOffset = [];
        public static Dictionary<RevolverCylinder, Transform> Measuriser = [];

        public static float InitialDamp = 0.05f;
        public static float InitialSpring = 0.05f;

        [HarmonyPatch("UpdateInteraction")]
        [HarmonyPrefix]
        public static bool UpdateInteractionPatch(RevolverCylinder __instance, FVRViveHand hand)
        {
            RevolverData revolverData = __instance.Revolver.gameObject.GetComponent<RevolverData>();

            if (!Measuriser.ContainsKey(__instance))
            {
                Measuriser.Add(__instance, new GameObject("Measurisationator").transform);
                Measuriser[__instance].SetParent(__instance.Revolver.transform);
                Measuriser[__instance].localPosition = __instance.Revolver.Cylinder.transform.localPosition;
            }
            float OrigAng = 0f;

            if (OrigAngOffset.ContainsKey(__instance))
            {
                OrigAng = OrigAngOffset[__instance];
            }

            Vector3 vector = hand.Input.Pos - revolverData.RevolverHinge.transform.position;
            Vector3 vector2;

            float num;

            if (__instance.Revolver.IsCylinderArmZ)
            {
                vector2 = Vector3.ProjectOnPlane(vector, __instance.Revolver.transform.right);
                /*
                if (Vector3.Angle(vector2, -__instance.Revolver.transform.up) > 90f)
                {
                    vector2 = __instance.Revolver.transform.forward;
                }
                else if (Vector3.Angle(vector2, __instance.Revolver.transform.forward) > 90f)
                {
                    vector2 = -__instance.Revolver.transform.up;
                }
                */

                // num = Vector3.Angle(vector2, __instance.Revolver.transform.forward) + OrigAngle;
                // shoutout to https://stackoverflow.com/questions/19675676/calculating-actual-angle-between-two-vectors-in-unity3d, what the fuck is a sign
                // angle in [0,180]
                float angle = Vector3.Angle(vector2, Measuriser[__instance].forward);
                float sign = Mathf.Sign(Vector3.Dot(-__instance.Revolver.transform.right, Vector3.Cross(vector2, Measuriser[__instance].forward)));

                // angle in [-179,180]
                float signed_angle = angle * sign;

                if (!OrigAngOffset.ContainsKey(__instance))
                {
                    OrigAng = signed_angle;
                    OrigAngOffset.Add(__instance, OrigAng);
                }

                num = signed_angle - OrigAng;
            }
            else if (__instance.Revolver.IsCylinderArmY)
            {
                vector2 = Vector3.ProjectOnPlane(vector, __instance.Revolver.transform.up);

                if (Vector3.Angle(vector2, __instance.Revolver.transform.right) > 90f)
                {
                    vector2 = __instance.Revolver.transform.forward;
                }
                else if (Vector3.Angle(vector2, __instance.Revolver.transform.forward) > 90f)
                {
                    vector2 = __instance.Revolver.transform.right;
                }
                num = Vector3.Angle(vector2, __instance.Revolver.transform.forward);
            }
            else
            {
                vector2 = Vector3.ProjectOnPlane(vector, __instance.Revolver.transform.forward);
                if (Vector3.Angle(vector2, -__instance.Revolver.transform.right) > 90f)
                {
                    vector2 = __instance.Revolver.transform.up;
                }
                else if (Vector3.Angle(vector2, __instance.Revolver.transform.up) > 90f)
                {
                    vector2 = -__instance.Revolver.transform.right;
                }
                num = Vector3.Angle(vector2, __instance.Revolver.transform.up);
            }

            JointSpring spring = revolverData.RevolverHinge.spring;
            spring.spring = 10f;
            spring.damper = 0f;
            spring.targetPosition = Mathf.Clamp(num, __instance.Revolver.CylinderRotRange.x, __instance.Revolver.CylinderRotRange.y);
            revolverData.RevolverHinge.spring = spring;
            revolverData.RevolverHinge.transform.localPosition = CylLocalPosStart[__instance];

            return true;
        }

        [HarmonyPatch("EndInteraction")]
        [HarmonyPrefix]
        public static bool EndInteractionPatch(RevolverCylinder __instance, FVRViveHand hand)
        {
            RevolverData revolverData = __instance.Revolver.GetComponent<RevolverData>();

            if (HandAngOffset.ContainsKey(__instance))
            {
                HandAngOffset.Remove(__instance);
                OrigAngOffset.Remove(__instance);
            }

            JointSpring spring = revolverData.RevolverHinge.spring;
            spring.spring = InitialSpring;
            spring.damper = InitialDamp;
            spring.targetPosition = 45f;
            revolverData.RevolverHinge.spring = spring;

            return true;
        }
    }
}