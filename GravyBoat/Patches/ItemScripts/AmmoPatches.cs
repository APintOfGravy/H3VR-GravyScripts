using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using FistVR;
using HarmonyLib;
using System.Reflection;
using System.ComponentModel;
using Valve.VR.InteractionSystem;
using System.CodeDom;
using System.Diagnostics.Eventing.Reader;
using System.Data;
using GravyScripts.Components;

namespace GravyScripts.Patches
{
    [HarmonyPatch(typeof(FVRFireArmRound))]
    public static class AmmoPatches
    {
        public static Dictionary<FVRFireArmRound, bool> shouldScan = [];
        public static List<FVRFireArmChamber> validChambers = [];
        public static List<FVRFireArmMagazineReloadTrigger> validTriggers = [];

        [HarmonyPatch("Awake")]
        [HarmonyPostfix]
        public static void AwakePatch(FVRFireArmRound __instance)
        {
            AddAmmoData(__instance);

            __instance.m_canAnimate = false;
        }

        public static AmmoData AddAmmoData(FVRFireArmRound __instance)
        {
            AmmoData data = __instance.gameObject.GetComponent<AmmoData>();
            if (data == null) 
            {
                data = __instance.gameObject.AddComponent<AmmoData>();

                data.OrientalTransform = new GameObject("Orient");
                data.OrientalTransform.transform.parent = __instance.transform;
                data.OrientalTransform.transform.localPosition = Vector3.zero;
                data.OrientalTransform.transform.localEulerAngles = Vector3.zero;

                data.UnfiredLength = __instance.UnfiredRenderer.GetComponent<MeshFilter>().mesh.bounds.size.z;
                if (__instance.FiredRenderer != null && (__instance.FiredRenderer.GetComponent<MeshFilter>().mesh.bounds.size.z < __instance.UnfiredRenderer.GetComponent<MeshFilter>().mesh.bounds.size.z))
                {
                    data.FiredLength = __instance.FiredRenderer.GetComponent<MeshFilter>().mesh.bounds.size.z;
                }
                else
                {
                    data.FiredLength = __instance.UnfiredRenderer.GetComponent<MeshFilter>().mesh.bounds.size.z;
                }

                data.OriginalPoseRot = __instance.PoseOverride.localEulerAngles;

                Mesh mesh = __instance.UnfiredRenderer.gameObject.GetComponent<MeshFilter>().mesh;
                Dictionary<float, float> HELL = [];
                foreach (Vector3 shit in mesh.vertices)
                {
                    bool inserted = false;
                    Dictionary<float, float> HELL2 = HELL;
                    // if we use HELL2, it won't yell at us for writing to a dictionary we're iterating through
                    foreach (KeyValuePair<float, float> kvp in HELL2)
                    {
                        // Check if its within a smaaaaaaallll tolerance.
                        if (kvp.Key > (shit.z - 0.0001) && kvp.Key < (shit.z + 0.0001))
                        {
                            if (shit.y > kvp.Value)
                            {
                                HELL.Remove(kvp.Key);
                                HELL.Add(shit.z, shit.y);
                            }
                            inserted = true;
                            break;
                        }
                    }
                    if (!inserted)
                    {
                        HELL.Add(shit.z, shit.y);
                    }
                }
                // Sort by the key's value, -Key should mean it sorts by the closest to the front first.
                HELL.OrderByDescending(o => o.Key);
                Debug.Log($"First key is {HELL.First().Key}, last is {HELL.Last().Key}");
                if (HELL.First().Key < HELL.Last().Key)
                {
                    Debug.Log("Reordering");
                    HELL.OrderBy(o => o.Key);
                }
                data.Horror = HELL;
            }

            return data;
        }

        [HarmonyPatch("BeginInteraction")]
        [HarmonyPostfix]
        public static void BeginInteractionPatch(FVRFireArmRound __instance, FVRViveHand hand)
        {
            AmmoData data = __instance.GetComponent<AmmoData>();

            data.ShouldScan = true;
            data.IsBeingChambered = false;
        }

        [HarmonyPatch("UpdateInteraction")]
        [HarmonyPostfix]
        public static void UpdateInteractionPatch(FVRFireArmRound __instance, FVRViveHand hand)
        {
            AmmoData data = __instance.GetComponent<AmmoData>();
            float targetAngle = 35f;

            foreach (Collider childColliders in __instance.m_colliders)
            {
                if (childColliders.gameObject != __instance.gameObject)
                {
                    childColliders.enabled = true;
                }
            }

            // Used for checking if the target has changed or if velocity changes have already been applied __instance Update, so that we don't accidentally do it twice, or when we shouldn't.
            bool applyVelocity = true;
            GameObject lastTarget = data.PivotTarget;

            __instance.UseGripRotInterp = true;
            if (hand.OtherHand.m_currentInteractable != null)
            {
                FVRInteractiveObject interactiveObject = hand.OtherHand.m_currentInteractable;
                Vector3 offset = Vector3.zero;

                if (interactiveObject is Revolver)
                {
                    if (__instance.ObjectWrapper.TagFirearmRoundPower == FVRObject.OTagFirearmRoundPower.Shotgun)
                    {
                        offset.x = -70;
                    }
                    else
                    {
                        offset.x = -100;
                    }
                }
                else if (interactiveObject is BreakActionWeapon || interactiveObject is TubeFedShotgun)
                {
                    offset.x = -40;
                }

                __instance.PoseOverride.localEulerAngles = data.OriginalPoseRot + offset;
            }
            else
            {
                __instance.PoseOverride.localEulerAngles = data.OriginalPoseRot;
            }

            data.OrientalTransform.transform.parent = __instance.transform;
            // __instance should be true when the round isn't already in the process of being loaded in a chamber && the other hand is holding something.
            if (data.ShouldScan && hand.OtherHand.m_currentInteractable != null)
            {
                // Clean old data.
                data.PivotTarget = null;

                // Iterate through every chamber and magazine loading point the other hand is holding.
                foreach (FVRFireArmChamber chamber in __instance.m_hand.OtherHand.m_currentInteractable.GetComponentsInChildren<FVRFireArmChamber>())
                {
                    if (Vector3.Distance(chamber.transform.position, __instance.transform.position) > data.UnfiredLength * 10)
                    {
                        continue;
                    }

                    Vector3 original = chamber.transform.localPosition;
                    Vector3 orient = chamber.transform.localPosition;
                    ChamberData chamberData = chamber.GetComponent<ChamberData>();

                    float angle;

                    if (chamberData.LoadPivotPoint == LoadTo.None)
                    {
                        continue;
                    }
                    else if (chamberData.LoadPivotPoint == LoadTo.Front)
                    {
                        orient.z += data.UnfiredLength * 0.5f;
                    }
                    else if (chamberData.LoadPivotPoint == LoadTo.Back)
                    {
                        orient.z -= data.UnfiredLength * 0.5f;
                    }
                    chamber.transform.localPosition = orient;

                    if (chamberData.LoadDirection == LoadAngle.BackFirst)
                    {
                        angle = Vector3.Angle(-__instance.transform.forward, chamber.transform.position - __instance.transform.position);
                    }
                    else if (chamberData.LoadDirection == LoadAngle.FrontFirst)
                    {
                        angle = Vector3.Angle(__instance.transform.forward, chamber.transform.position - __instance.transform.position);
                    }
                    else
                    {
                        chamber.transform.localPosition = original;
                        continue;
                    }

                    // Check if the chamber is actually usable && make sure the angle is the most accurate one to use && make sure the hand isn't too far away.
                    if (chamber.RoundType == __instance.RoundType && chamber.IsManuallyChamberable && chamber.IsAccessible && !chamber.IsFull && (angle < targetAngle) && (Vector3.Distance(chamber.transform.position, data.OrientalTransform.transform.position) < data.UnfiredLength * 3))
                    {
                        targetAngle = angle;

                        data.PivotTarget = chamber.gameObject;
                    }

                    chamber.transform.localPosition = original;
                }

                foreach (FVRFireArmMagazineReloadTrigger trigger in __instance.m_hand.OtherHand.m_currentInteractable.GetComponentsInChildren<FVRFireArmMagazineReloadTrigger>())
                {
                    ReloadTriggerPatches.NotAPatch(trigger);

                    if (Vector3.Distance(trigger.transform.position, __instance.transform.position) > data.UnfiredLength * 10)
                    {
                        continue;
                    }

                    // If the magazine is loaded in the gun && it's detachable, it's usually inaccessible, so we don't want it.
                    if (trigger.Magazine.RoundType != __instance.RoundType || trigger.Magazine.IsFull() || (trigger.Magazine.FireArm != null && !trigger.Magazine.IsDropInLoadable))
                    {
                        continue;
                    }

                    Vector3 original = trigger.transform.localPosition;
                    Vector3 orient = trigger.transform.localPosition;
                    ChamberData chamberData = trigger.GetComponent<ChamberData>();

                    float angle;

                    if (chamberData.LoadPivotPoint == LoadTo.None)
                    {
                        continue;
                    }
                    else if (chamberData.LoadPivotPoint == LoadTo.Front)
                    {
                        orient.z += data.UnfiredLength * 0.5f;
                    }
                    else if (chamberData.LoadPivotPoint == LoadTo.Back)
                    {
                        orient.z -= data.UnfiredLength * 0.5f;
                    }
                    trigger.transform.localPosition = orient;

                    if (chamberData.LoadDirection == LoadAngle.BackFirst)
                    {
                        angle = Vector3.Angle(-__instance.transform.forward, trigger.transform.position - __instance.transform.position);
                    }
                    else if (chamberData.LoadDirection == LoadAngle.FrontFirst)
                    {
                        angle = Vector3.Angle(__instance.transform.forward, trigger.transform.position - __instance.transform.position);
                    }
                    else
                    {
                        trigger.transform.localPosition = original;
                        continue;
                    } 

                    float distance = Vector3.Distance(trigger.transform.position, data.OrientalTransform.transform.position);
                    float length = data.UnfiredLength * 2;

                    // Check if the angle is the most accurate one to use && make sure the hand isn't too far away
                    if (angle < targetAngle && distance < length)
                    {
                        // I don't like checking DoesDisplayXOscillate, but it seems a reliable way to distinguish box mag from tube-fed, if nothing else.
                        if (trigger.Magazine.IsDropInLoadable && trigger.Magazine.DoesDisplayXOscillate)
                        {
                            continue;
                        }
                        else
                        {
                            targetAngle = angle;

                            data.PivotTarget = trigger.gameObject;
                        }
                    }
                    
                    trigger.transform.localPosition = original;
                }

                if (data.PivotTarget != lastTarget || data.PivotTarget == null)
                {
                    // The target is one we weren't focusing before, don't correct velocity.
                    applyVelocity = false;
                }
                data.OrientalTransform.transform.position = Vector3.MoveTowards(data.OrientalTransform.transform.position, __instance.transform.position, data.PosSpeed * Time.deltaTime);

                if (data.PivotTarget != null)
                {
                    // Correct positioning that other methods might've been doing.
                    Vector3 aaa = data.OrientalTransform.transform.localPosition;

                    FVRFireArmMagazineReloadTrigger trigger = data.PivotTarget.GetComponent<FVRFireArmMagazineReloadTrigger>();
                    ChamberData chamberData = data.PivotTarget.GetComponent<ChamberData>();
                    Vector3 orient = data.PivotTarget.transform.localPosition;
                    Vector3 original = data.PivotTarget.transform.localPosition;

                    // Save the current rotation, LookAt to get the rotation where we face towards the target, then undo our changes 
                    Quaternion curRot = data.OrientalTransform.transform.rotation;

                    // Magazine
                    if (chamberData.LoadPivotPoint == LoadTo.Front)
                    {
                        orient.z += data.UnfiredLength * 0.48f;
                    }
                    else if (chamberData.LoadPivotPoint == LoadTo.Back)
                    {
                        orient.z -= data.UnfiredLength * 0.48f;
                    }
                    data.PivotTarget.transform.localPosition = orient;
                    data.OrientalTransform.transform.LookAt(data.PivotTarget.transform);

                    if (chamberData.LoadDirection == LoadAngle.BackFirst)
                    {
                        data.OrientalTransform.transform.forward = -data.OrientalTransform.transform.forward;
                        // data.OrientalTransform.transform.LookAt((data.PivotTarget.transform.position - data.OrientalTransform.transform.position) * -1 + data.OrientalTransform.transform.position);
                        // data.OrientalTransform.transform.Rotate(0, 180, 0);
                    }
                    data.OrientalTransform.transform.rotation = curRot; // Quaternion.RotateTowards(curRot, data.OrientalTransform.transform.rotation, data.RotSpeed * Time.deltaTime);



                    if (chamberData.LoadDirection == LoadAngle.BackFirst)
                    {
                        data.OrientalTransform.transform.position += (-data.OrientalTransform.transform.forward * data.UnfiredLength * 0.5f);
                    }
                    else if (chamberData.LoadDirection == LoadAngle.FrontFirst)
                    {
                        data.OrientalTransform.transform.position += (data.OrientalTransform.transform.forward * data.UnfiredLength * 0.5f);
                    }

                    float angle = Vector3.Angle(-data.PivotTarget.transform.forward, data.OrientalTransform.transform.position - data.PivotTarget.transform.position);
                    float distance = Vector3.Distance(data.OrientalTransform.transform.position, data.PivotTarget.transform.position);
                    float length = data.UnfiredLength * 01.5f;

                    // If it's close enough, start chambering.
                    if ((angle < 45f) && (distance < length) && data.TimeSinceChambered > 0.3f)
                    {
                        // If the angle is low, automatically go to push in.
                        if (angle < 35)
                        {
                            data.TimeSinceChamberStarted = 0;
                            data.ShouldScan = false;
                            data.IsBeingWiggled = false;
                            data.IsBeingChambered = true;
                        }
                        // Give topping up magazines a bit more leeway. 
                        else if (angle < 35 && trigger != null && trigger.Magazine != null && trigger.Magazine.FireArm == null && !trigger.Magazine.IsDropInLoadable)
                        {
                            data.TimeSinceChamberStarted = 0;
                            data.ShouldScan = false;
                            data.IsBeingWiggled = false;
                            data.IsBeingChambered = true;
                        }

                        // If not, intermediate state.
                        else if (false) //Vector3.Angle(data.OrientalTransform.transform.forward, data.PivotTarget.transform.forward) < 45f)
                        {
                            data.ShouldScan = false;
                            data.IsBeingWiggled = true;
                            data.IsBeingChambered = false;

                            SM.PlayImpactSound(__instance.AudioImpactController.ImpactType, MatSoundType.Plastic, AudioImpactIntensity.Light, data.PivotTarget.transform.position, FVRPooledAudioType.Impacts, 25);
                        }
                    }
                    
                    data.PivotTarget.transform.localPosition = original;
                    data.OrientalTransform.transform.localPosition = aaa;
                }
            }
            if (data.IsBeingWiggled)
            {
                foreach (Collider childColliders in __instance.m_colliders)
                {
                    if (childColliders.gameObject != __instance.gameObject)
                    {
                        childColliders.enabled = false;
                    }
                }
                FVRFireArmChamber chamber = data.PivotTarget.GetComponent<FVRFireArmChamber>();
                FVRFireArmMagazineReloadTrigger trigger = data.PivotTarget.GetComponent<FVRFireArmMagazineReloadTrigger>();
                ChamberData chamberData = data.PivotTarget.GetComponent<ChamberData>();
                Vector3 orient = data.PivotTarget.transform.localPosition;
                Vector3 targetPos = data.PivotTarget.transform.localPosition;
                Vector3 pivotPos = data.OrientalTransform.transform.position;
                data.OrientalTransform.transform.position = __instance.transform.position;

                if (chamberData.LoadPivotPoint == LoadTo.Front)
                {
                    orient.z += data.UnfiredLength * 0.5f;
                }
                else if (chamberData.LoadPivotPoint == LoadTo.Back)
                {
                    orient.z -= data.UnfiredLength * 0.5f;
                }
                data.PivotTarget.transform.localPosition = orient;
                if (Vector3.Distance(data.PivotTarget.transform.position, __instance.transform.position) > data.UnfiredLength * 1.5 || Vector3.Angle(data.OrientalTransform.transform.forward, data.PivotTarget.transform.forward) > 60f)
                {
                    data.IsBeingWiggled = false;
                    data.ShouldScan = true;
                }
                else
                {
                    data.OrientalTransform.transform.position = __instance.transform.position;//__instance.transform.position;
                    data.OrientalTransform.transform.LookAt(data.PivotTarget.transform);

                    if (chamberData.LoadDirection == LoadAngle.BackFirst)
                    {
                        data.OrientalTransform.transform.forward = -data.OrientalTransform.transform.forward;
                        // data.OrientalTransform.transform.LookAt((data.PivotTarget.transform.position - data.OrientalTransform.transform.position) * -1 + data.OrientalTransform.transform.position);
                        // data.OrientalTransform.transform.Rotate(0, 180, 0);
                    }

                    data.OrientalTransform.transform.position = data.PivotTarget.transform.position;
                    if (chamberData.LoadDirection == LoadAngle.BackFirst)
                    {
                        data.OrientalTransform.transform.position += data.OrientalTransform.transform.forward * (data.UnfiredLength * 0.5f);
                    }
                    else if (chamberData.LoadDirection == LoadAngle.FrontFirst)
                    {
                        data.OrientalTransform.transform.position -= data.OrientalTransform.transform.forward * (data.UnfiredLength * 0.5f);
                    }
                    data.OrientalTransform.transform.position = Vector3.MoveTowards(pivotPos, data.OrientalTransform.transform.position, data.PosSpeed * Time.deltaTime);

                    float distanceFromHand = Vector3.Distance(data.PivotTarget.transform.position, __instance.transform.position) / data.UnfiredLength;
                    float angle;
                    float angleCheck = 20 - (8 * distanceFromHand);

                    // Magazine
                    if (chamberData.LoadDirection == LoadAngle.BackFirst)
                    {
                        orient.z -= data.UnfiredLength * distanceFromHand;
                        data.PivotTarget.transform.localPosition = orient;
                        angle = Vector3.Angle(data.PivotTarget.transform.forward, __instance.transform.position - data.PivotTarget.transform.position);
                    }
                    else
                    {
                        orient.z += data.UnfiredLength * distanceFromHand;
                        data.PivotTarget.transform.localPosition = orient;
                        angle = Vector3.Angle(-data.PivotTarget.transform.forward, __instance.transform.position - data.PivotTarget.transform.position);
                    }
                    
                    if (angle < angleCheck)
                    {
                        data.ShouldScan = false;
                        data.IsBeingWiggled = false;
                        data.IsBeingChambered = true;
                    }
                }

                data.PivotTarget.transform.localPosition = targetPos;
            }
            if (data.IsBeingChambered)
            {
                FVRFireArmChamber chamber = data.PivotTarget.GetComponent<FVRFireArmChamber>();
                FVRFireArmMagazineReloadTrigger trigger = data.PivotTarget.GetComponent<FVRFireArmMagazineReloadTrigger>();
                ChamberData chamberData = data.PivotTarget.GetComponent<ChamberData>();
                float length = data.UnfiredLength * 1.1f;

                if (__instance.IsSpent && __instance.FiredRenderer != null)
                {
                    length = data.FiredLength * 1.1f;
                }

                // Check if the hand is too far from the chamber.
                if (Vector3.Distance(data.PivotTarget.transform.position, data.OrientalTransform.transform.position) > length && data.UnchamberedAgo > 0.3)
                {
                    data.IsBeingChambered = false;
                    data.ShouldScan = true;
                }
                else
                {
                    data.TimeSinceChambered = 0;
                    foreach (Collider childColliders in __instance.m_colliders)
                    {
                        if (childColliders.gameObject != __instance.gameObject)
                        {
                            childColliders.enabled = false;
                        }
                    }

                    data.OrientalTransform.transform.position = __instance.transform.position;
                    data.OrientalTransform.transform.rotation = __instance.transform.rotation;

                    data.OrientalTransform.transform.parent = data.PivotTarget.transform.parent;

                    if (chamberData.LoadDirection == LoadAngle.FrontFirst)
                    {
                        // First, we need to figure out how far this point could go along the forward axis based on it's width. Trigonomery. I hate trigonometry. It's always a good sign when you're having to go on to Wikipedia to figure out how to handle triangles because you never made it far enough in school to learn anything about trigonometry.
                        // ...Shoutout to my boy https://en.wikipedia.org/wiki/Solution_of_triangles, and it's source... mathsisfun.com. Let this be the least dignified code comment I ever write, please dear god.
                        Vector3 orientPosOnAxis = Vector3.Project(data.OrientalTransform.transform.position - data.PivotTarget.transform.position, data.PivotTarget.transform.forward) + data.PivotTarget.transform.position;
                        float distanceFromAxis = Vector3.Distance(data.OrientalTransform.transform.position, orientPosOnAxis);
                        float distanceToFront = data.Horror.First().Key;

                        if ((distanceFromAxis < distanceToFront)) // && (Vector3.Angle(data.OrientalTransform.transform.forward, data.PivotTarget.transform.forward) < 45))
                        {
                            // Do triangle maths (horror) to figure out where we can make the front of our round line up with the chamber axis.
                            float sideA = distanceToFront;
                            float sideB = distanceFromAxis;
                            float angleA = Vector3.Angle(data.PivotTarget.transform.forward, data.OrientalTransform.transform.position - orientPosOnAxis);
                            float angleB = Mathf.Asin(sideB * Mathf.Sin(angleA) / sideA);
                            float angleC = 180f - angleA - angleB;
                            float sideC = Mathf.Sin(angleC) * sideA / Mathf.Sin(angleA);

                            Vector3 aimPoint = orientPosOnAxis + (data.PivotTarget.transform.forward * sideC);
                            // I don't know why but for some reason, the round doesn't seem to line up perfectly. So we need to get the distance from the round to the aimPoint, which for *some godforsaken reason* doesn't seem to be the distance between the center and front of the round.
                            if (Vector3.Distance(data.OrientalTransform.transform.position, aimPoint) != data.UnfiredLength)
                            {
                                Debug.Log($"Yeah, it's different. {Vector3.Distance(data.OrientalTransform.transform.position, aimPoint)} versus {data.Horror.First().Key} and {data.UnfiredLength}");
                            }
                            data.OrientalTransform.transform.position += data.OrientalTransform.transform.forward * data.Horror.Last().Key;

                            data.OrientalTransform.transform.LookAt(aimPoint, __instance.transform.up);
                            data.OrientalTransform.transform.position -= data.OrientalTransform.transform.forward * data.Horror.Last().Key;

                            // Now through the power of more triangles, we figure out how to clamp the round's rotation so that we don't have it clipping through the rim of the chamber.
                            // First, we need to find the maximum angle a round can feasibly be at.
                            float roundWidth = __instance.UnfiredRenderer.GetComponent<MeshFilter>().mesh.bounds.size.y;

                            /*
                            float sideD = roundWidth * 0.5f;
                            Vector3 chamberTop = data.PivotTarget.transform.position + (data.PivotTarget.transform.up * sideD);
                            float sideE = Vector3.Distance(aimPoint, chamberTop);
                            float angleE = 90;
                            float angleD = Mathf.Asin((sideD * Mathf.Sin(angleE)) / sideE);
                            float angleF = 180 - angleE - angleD;
                            float sideF = (Mathf.Sin(angleF) * sideE) / Mathf.Sin(angleE);


                            GameObject measuriser = new();
                            measuriser.transform.parent = data.PivotTarget.transform;
                            measuriser.transform.position = aimPoint;
                            measuriser.transform.LookAt(chamberTop, data.PivotTarget.transform.up);
                            measuriser.transform.localEulerAngles = new Vector3(measuriser.transform.localEulerAngles.x - angleD, measuriser.transform.localEulerAngles.y, measuriser.transform.localEulerAngles.z);
                            */
                            /*
                            measuriser.transform.localPosition += measuriser.transform.forward * sideE;
                            measuriser.transform.forward = -measuriser.transform.forward;
                            measuriser.transform.localEulerAngles = new Vector3(measuriser.transform.localEulerAngles.x - angleF, measuriser.transform.localEulerAngles.y, measuriser.transform.localEulerAngles.z);
                            measuriser.transform.position += measuriser.transform.forward * sideD;
                            measuriser.transform.LookAt(aimPoint, data.PivotTarget.transform.up);
                            */
                            /*
                            data.OrientalTransform.transform.position += (data.OrientalTransform.transform.forward * distanceToFront);

                            float roundAngle = Vector3.Angle(data.PivotTarget.transform.forward, data.OrientalTransform.transform.forward);
                            float maxAngle = Vector3.Angle(-data.PivotTarget.transform.forward, measuriser.transform.forward);
                            float changeAngleBy = Mathf.Max(roundAngle - maxAngle, 0);
                            */
                            /*
                            float changeAngleBy = 0f;
                            if (roundAngle > Vector3.Angle(measuriser.transform.forward, data.PivotTarget.transform.forward))
                            {
                                changeAngleBy = Vector3.Angle(measuriser.transform.forward, data.OrientalTransform.transform.forward);
                            }
                            */
                            data.OrientalTransform.transform.position += data.OrientalTransform.transform.forward * distanceToFront;
                            if (chamberData.LoadPivotPoint == LoadTo.Back)
                            {
                                // Using last will make it go backwards because no round in the game exists that has it's rearmost point in front of the center. I hope this never changes.
                                data.PivotTarget.transform.position += data.PivotTarget.transform.forward * data.Horror.Last().Key;
                            }
                            else if (chamberData.LoadPivotPoint == LoadTo.Front)
                            {
                                data.PivotTarget.transform.position -= data.PivotTarget.transform.forward * data.Horror.Last().Key;
                            }

                            if (Vector3.Angle(data.PivotTarget.transform.forward, aimPoint - data.PivotTarget.transform.position) < Vector3.Angle(-data.PivotTarget.transform.forward, aimPoint - data.PivotTarget.transform.position))
                            {
                                // data.PivotTarget.transform.position -= data.PivotTarget.transform.forward * distanceToFront;

                                float percDistIn = 1f / (roundWidth * 0.5f) * Vector3.Distance(aimPoint, data.PivotTarget.transform.position);
                                percDistIn = 1f - (percDistIn * -1f);
                                float changeAngleBy = Vector3.Angle(data.OrientalTransform.transform.forward, data.PivotTarget.transform.forward) * percDistIn;

                                // data.PivotTarget.transform.position += data.PivotTarget.transform.forward * distanceToFront;
                                // data.OrientalTransform.transform.forward = Vector3.RotateTowards(data.OrientalTransform.transform.forward, data.PivotTarget.transform.forward, changeAngleBy, 0);
                                data.OrientalTransform.transform.rotation = Quaternion.RotateTowards(data.OrientalTransform.transform.rotation, data.PivotTarget.transform.rotation, changeAngleBy);
                            }

                            if (chamberData.LoadPivotPoint == LoadTo.Back)
                            {
                                data.PivotTarget.transform.position -= data.PivotTarget.transform.forward * data.Horror.Last().Key;
                            }
                            else if (chamberData.LoadPivotPoint == LoadTo.Front)
                            {
                                data.PivotTarget.transform.position += data.PivotTarget.transform.forward * data.Horror.Last().Key;
                            }

                            data.OrientalTransform.transform.position -= data.OrientalTransform.transform.forward * distanceToFront;
                        }
                    }
                    else if (chamberData.LoadDirection == LoadAngle.BackFirst)
                    {
                        // Vector3 savePos = data.OrientalTransform.transform.position;
                        Vector3 orientPosOnAxis = Vector3.Project(data.OrientalTransform.transform.position - data.PivotTarget.transform.position, data.PivotTarget.transform.forward) + data.PivotTarget.transform.position;
                        float distanceFromAxis = Vector3.Distance(data.OrientalTransform.transform.position, orientPosOnAxis);
                        float distanceToRear = data.Horror.Last().Key;

                        if (distanceFromAxis < distanceToRear)
                        {
                            float sideA = distanceToRear;
                            float sideB = distanceFromAxis;
                            float angleA = 90f;
                            float angleB = Mathf.Asin((sideB * Mathf.Sin(angleA)) / sideA);
                            float angleC = 180f - angleA - angleB;
                            float sideC = (Mathf.Sin(angleC) * sideA) / Mathf.Sin(angleA);

                            Vector3 aimPoint = orientPosOnAxis - (data.PivotTarget.transform.forward * sideC);
                            distanceToRear = Vector3.Distance(data.OrientalTransform.transform.position, aimPoint);

                            data.OrientalTransform.transform.LookAt(aimPoint);
                            data.OrientalTransform.transform.forward = -data.OrientalTransform.transform.forward;

                            float roundWidth = __instance.UnfiredRenderer.GetComponent<MeshFilter>().mesh.bounds.size.y;
                            data.OrientalTransform.transform.position -= (data.OrientalTransform.transform.forward * distanceToRear);
                            if (chamberData.LoadPivotPoint == LoadTo.Back)
                            {
                                // Using last will make it go backwards because no round in the game exists that has it's rearmost point in front of the center. I hope this never changes.
                                data.PivotTarget.transform.position -= (data.PivotTarget.transform.forward * data.Horror.First().Key);
                            }
                            else if (chamberData.LoadPivotPoint == LoadTo.Front)
                            {
                                data.PivotTarget.transform.position += (data.PivotTarget.transform.forward * data.Horror.First().Key);
                            }

                            if (Vector3.Angle(-data.PivotTarget.transform.forward, aimPoint - data.PivotTarget.transform.position) < Vector3.Angle(data.PivotTarget.transform.forward, aimPoint - data.PivotTarget.transform.position))
                            {
                                float percDistIn = (1f / (roundWidth * 0.5f)) * Vector3.Distance(aimPoint, data.PivotTarget.transform.position);
                                percDistIn = 1f - (percDistIn * -1f);
                                float changeAngleBy = Vector3.Angle(data.OrientalTransform.transform.forward, data.PivotTarget.transform.forward) * percDistIn;

                                data.OrientalTransform.transform.rotation = Quaternion.RotateTowards(data.OrientalTransform.transform.rotation, data.PivotTarget.transform.rotation, changeAngleBy);
                            }

                            data.OrientalTransform.transform.position += (data.OrientalTransform.transform.forward * distanceToRear);

                            if (chamberData.LoadPivotPoint == LoadTo.Back)
                            {
                                data.PivotTarget.transform.position += (data.PivotTarget.transform.forward * data.Horror.First().Key);
                            }
                            else if (chamberData.LoadPivotPoint == LoadTo.Front)
                            {
                                data.PivotTarget.transform.position -= (data.PivotTarget.transform.forward * data.Horror.First().Key);
                            }
                        }
                    }

                    data.OrientalTransform.transform.position = Vector3.Lerp(__instance.transform.position, data.OrientalTransform.transform.position, data.TimeSinceChamberStarted / 0.25f);
                    data.OrientalTransform.transform.rotation = Quaternion.Lerp(__instance.transform.rotation, data.OrientalTransform.transform.rotation, data.TimeSinceChamberStarted / 0.25f);
                    
                    if (chamberData.LoadPivotPoint == LoadTo.Center)
                    {
                        data.PivotTarget.transform.position += data.PivotTarget.transform.forward * data.Horror.First().Key;
                    }

                    bool angleCheck = Vector3.Angle(data.PivotTarget.transform.forward, data.OrientalTransform.transform.position - data.PivotTarget.transform.position) < Vector3.Angle(-data.PivotTarget.transform.forward, data.OrientalTransform.transform.position - data.PivotTarget.transform.position);

                    if (chamberData.LoadDirection == LoadAngle.BackFirst)
                    {
                        angleCheck = Vector3.Angle(data.PivotTarget.transform.forward, data.OrientalTransform.transform.position - data.PivotTarget.transform.position) > Vector3.Angle(-data.PivotTarget.transform.forward, data.OrientalTransform.transform.position - data.PivotTarget.transform.position);
                    }

                    if (chamberData.LoadPivotPoint == LoadTo.Center)
                    {
                        data.PivotTarget.transform.position -= (data.PivotTarget.transform.forward * data.Horror.First().Key);
                    }

                    // Check if the angle between the round and the front of the chamber position is less than the rear. Easy way of checking forwards VS backwards.
                    if (angleCheck && data.UnchamberedAgo < 0.3)
                    {
                        data.OrientalTransform.transform.position = data.PivotTarget.transform.position;
                    }
                    else if (angleCheck)
                    {
                        data.OrientalTransform.transform.position = data.PivotTarget.transform.position;

                        // The round has gone far forward enough to be chambered, wahoo
                        data.IsBeingChambered = false;
                        data.ShouldScan = false;

                        if (chamber != null)
                        {
                            // __instance.Chamber(__instance.HoveredOverChamber, true);
                            chamber.SetRound(__instance, data.OrientalTransform.transform.position, data.OrientalTransform.transform.rotation);
                            chamber.PlayChamberingAudio();

                            // UnityEngine.Object.Destroy(__instance.gameObject);
                        }
                        else
                        {
                            trigger.Magazine.AddRound(__instance, true, true);
                        }

                        if (__instance.ProxyRounds.Count > 0)
                        {
                            __instance.UnfiredRenderer.enabled = false;
                            if (__instance.FiredRenderer != null)
                            {
                                __instance.FiredRenderer.enabled = false;
                            }

                            data.TimeSinceChambered = 0f;
                            data.IsRepalming = true;
                        }
                        else
                        {
                            __instance.ForceBreakInteraction();

                            UnityEngine.Object.Destroy(__instance.gameObject);
                        }
                    }
                }
            }
            if (data.IsRepalming)
            {
                foreach (Collider childColliders in __instance.m_colliders)
                {
                    if (childColliders.gameObject != __instance.gameObject)
                    {
                        childColliders.enabled = false;
                    }
                }

                bool isFarEnough = Vector3.Distance(data.PivotTarget.transform.position, data.OrientalTransform.transform.position) > data.UnfiredLength * 1.5;
                bool hasBeenTooLong = data.TimeSinceChambered > 2;

                if (isFarEnough || hasBeenTooLong)
                {
                    if (__instance.ProxyRounds.Count > 0)
                    {
                        __instance.CycleToProxy(true, false);
                        __instance.m_proxyDumpFlag = false;
                    }

                    __instance.ForceBreakInteraction();

                    UnityEngine.Object.Destroy(__instance.gameObject);
                }
                data.TimeSinceChambered += Time.deltaTime;
            }
        }

        [HarmonyPatch("UpdateProxyPositions")]
        [HarmonyPostfix]
        public static void UpdateProxyPositionsPatch(FVRFireArmRound __instance)
        {
            if (__instance.ProxyRounds.Count > 0)
            {
                if (__instance.IsHeld)
                {
                    if (__instance.ProxyRounds.Count == 1 && __instance.ProxyPose == FVRFireArmRound.ProxyPositionMode.Standard && __instance.IsHeld && AM.GetRoundPower(__instance.RoundType) == FVRObject.OTagFirearmRoundPower.Shotgun && __instance.m_hand.OtherHand.CurrentInteractable is FVRFireArm && (__instance.m_hand.OtherHand.CurrentInteractable as FVRFireArm).Magazine != null && (__instance.m_hand.OtherHand.CurrentInteractable as FVRFireArm).Magazine.IsIntegrated)
                    {
                        __instance.ProxyPose = FVRFireArmRound.ProxyPositionMode.InLine;
                        Vector3 position = __instance.transform.position;
                        float height = __instance.GetComponent<CapsuleCollider>().height;
                        Vector3 vector = -__instance.transform.forward * (height * 1.02f);
                        for (int i = 0; i < __instance.ProxyRounds.Count; i++)
                        {
                            __instance.ProxyRounds[i].GO.transform.position = position + vector * (i + 1);
                            __instance.ProxyRounds[i].GO.transform.localRotation = Quaternion.identity;
                        }
                    }
                    else
                    {
                        __instance.ProxyPose = FVRFireArmRound.ProxyPositionMode.Standard;
                        Vector3 position2 = __instance.transform.position;
                        Vector3 vector2 = -__instance.transform.forward * __instance.PalmingDimensions.z + -__instance.transform.up * __instance.PalmingDimensions.y;
                        for (int j = 0; j < __instance.ProxyRounds.Count; j++)
                        {
                            __instance.ProxyRounds[j].GO.transform.position = position2 + vector2 * (j + 2);
                            __instance.ProxyRounds[j].GO.transform.localRotation = Quaternion.identity;
                        }
                    }
                }
                else
                {
                    Vector3 position3 = __instance.transform.position;
                    Vector3 vector3 = -__instance.transform.up * __instance.PalmingDimensions.y;
                    for (int k = 0; k < __instance.ProxyRounds.Count; k++)
                    {
                        __instance.ProxyRounds[k].GO.transform.position = position3 + vector3 * (k + 2);
                        __instance.ProxyRounds[k].GO.transform.localRotation = Quaternion.identity;
                    }
                }
            }
            else
            {
                __instance.ProxyPose = FVRFireArmRound.ProxyPositionMode.Standard;
            }
        }

        public static void ApplyVelocity(FVRFireArmRound __instance)
        {
            AmmoData data = __instance.GetComponent<AmmoData>();

            data.OrientalTransform.transform.position += data.PivotTarget.transform.position - data.TargetPosLastFrame;
            data.OrientalTransform.transform.eulerAngles += data.PivotTarget.transform.eulerAngles - data.TargetRotLastFrame;

            data.TargetPosLastFrame = data.PivotTarget.transform.position;
            data.TargetRotLastFrame = data.PivotTarget.transform.eulerAngles;
        }

        [HarmonyPatch("EndInteraction")]
        [HarmonyPostfix]
        public static void EndInteractionPatch(FVRFireArmRound __instance, FVRViveHand hand)
        {
            AmmoData data = __instance.GetComponent<AmmoData>();
            __instance.PoseOverride.localEulerAngles = data.OriginalPoseRot;

            foreach (Collider childColliders in __instance.m_colliders)
            {
                if (childColliders.gameObject != __instance.gameObject)
                {
                    childColliders.enabled = true;
                }
            }
        }

        [HarmonyPatch("FVRUpdate")]
        [HarmonyPostfix]
        public static void FVRUpdatePatch(FVRFireArmRound __instance)
        {
            AmmoData data = AddAmmoData(__instance);

            if ((!data.ShouldScan || data.PivotTarget == null) && !data.IsBeingChambered && !data.IsBeingWiggled)
            {
                data.OrientalTransform.transform.position = Vector3.MoveTowards(data.OrientalTransform.transform.position, __instance.transform.position, data.PosSpeed * Time.deltaTime);
                data.OrientalTransform.transform.rotation = Quaternion.RotateTowards(data.OrientalTransform.transform.rotation, __instance.transform.rotation, data.RotSpeed * Time.deltaTime);
            }

            __instance.UnfiredRenderer.transform.position = data.OrientalTransform.transform.position;
            __instance.UnfiredRenderer.transform.rotation = data.OrientalTransform.transform.rotation;
            if (__instance.FiredRenderer != null)
            {
                __instance.FiredRenderer.transform.position = data.OrientalTransform.transform.position;
                __instance.FiredRenderer.transform.rotation = data.OrientalTransform.transform.rotation;
            }

            foreach (Collider collider in __instance.m_colliders)
            {
                if (collider.gameObject != __instance.gameObject)
                {
                    collider.transform.position = data.OrientalTransform.transform.position;
                    collider.transform.rotation = data.OrientalTransform.transform.rotation;
                }
            }
        }

        // Patch the default collision detection to stop it from interfering with our bespoke code.
        [HarmonyPatch("OnTriggerEnter")]
        [HarmonyPrefix]
        public static bool TriggerEnterPatch(FVRFireArmRound __instance, Collider collider)
        {
            AmmoData data = __instance.GetComponent<AmmoData>();
            data.IsInTrigger = true;

            if (__instance.IsSpent)
            {
                return false;
            }
            if (__instance.isManuallyChamberable && __instance.HoveredOverChamber == null && __instance.m_hoverOverReloadTrigger == null && collider.gameObject.CompareTag("FVRFireArmChamber"))
            {
                FVRFireArmChamber chamber = collider.gameObject.GetComponent<FVRFireArmChamber>();
                ChamberData chamberData = collider.gameObject.GetComponent<ChamberData>();
                if (chamber != null  && !chamber.IsFull && chamber.IsAccessible && !data.IsRepalming && data.TimeSinceChambered > 0.3f)
                {
                    if (chamberData.LoadDirection == LoadAngle.None)
                    {
                        __instance.Chamber(chamber, true);

                        if (__instance.ProxyRounds.Count > 0)
                        {
                            __instance.UnfiredRenderer.enabled = false;
                            if (__instance.FiredRenderer != null)
                            {
                                __instance.FiredRenderer.enabled = false;
                            }

                            data.TimeSinceChambered = 0f;
                            data.ShouldScan = false;
                            data.IsBeingWiggled = false;
                            data.IsBeingChambered = false;
                            data.IsRepalming = true;
                            // make sure to actually set the pivot target so we don't get lots of angry errors. whoops
                            data.PivotTarget = chamber.gameObject;
                        }
                        else
                        {
                            __instance.ForceBreakInteraction();

                            UnityEngine.Object.Destroy(__instance.gameObject);
                        }
                    }
                    else
                    {
                        data.TimeSinceChamberStarted = 0;
                        data.ShouldScan = false;
                        data.IsBeingWiggled = false;
                        data.IsBeingChambered = true;

                        data.PivotTarget = chamber.gameObject;
                    }
                    return false;
                }
            }
            if (__instance.isMagazineLoadable && __instance.HoveredOverChamber == null && collider.gameObject.CompareTag("FVRFireArmMagazineReloadTrigger"))
            {
                FVRFireArmMagazineReloadTrigger trigger = collider.gameObject.GetComponent<FVRFireArmMagazineReloadTrigger>();
                ChamberData chamberData = collider.gameObject.GetComponent<ChamberData>();

                if (trigger != null && trigger.Magazine != null && trigger.Magazine.RoundType == __instance.RoundType && !trigger.Magazine.IsFull() && !data.IsBeingChambered && !data.IsRepalming && data.TimeSinceChambered > 0.3f)
                {
                    if (chamberData.LoadDirection == LoadAngle.None)
                    {
                        trigger.Magazine.AddRound(__instance, true, true, true);

                        if (__instance.ProxyRounds.Count > 0)
                        {
                            __instance.UnfiredRenderer.enabled = false;
                            if (__instance.FiredRenderer != null)
                            {
                                __instance.FiredRenderer.enabled = false;
                            }

                            data.TimeSinceChambered = 0f;
                            data.ShouldScan = false;
                            data.IsBeingWiggled = false;
                            data.IsBeingChambered = false;
                            data.IsRepalming = true;

                            data.PivotTarget = trigger.gameObject;
                        }
                        else
                        {
                            __instance.ForceBreakInteraction();

                            UnityEngine.Object.Destroy(__instance.gameObject);
                        }
                    }
                    else
                    {
                        data.TimeSinceChamberStarted = 0;
                        data.ShouldScan = false;
                        data.IsBeingWiggled = false;
                        data.IsBeingChambered = true;

                        data.PivotTarget = trigger.gameObject;
                    }

                    return false;
                }
            }
            if (__instance.isPalmable && __instance.ProxyRounds.Count < __instance.MaxPalmedAmount && !__instance.IsSpent && collider.gameObject.CompareTag("FVRFireArmRound"))
            {
                FVRFireArmRound round = collider.gameObject.GetComponent<FVRFireArmRound>();
                if (round.RoundType == __instance.RoundType && !round.IsSpent && round.QuickbeltSlot == null)
                {
                    __instance.HoveredOverRound = round;
                }
            }

            return false;
        }

        [HarmonyPatch("OnTriggerExit")]
        [HarmonyPrefix]
        public static bool TriggerExitPatch(FVRFireArmRound __instance, Collider collider)
        {
            AmmoData data = __instance.GetComponent<AmmoData>();
            data.IsInTrigger = false;

            if (__instance.m_hand == null || __instance.m_hand.OtherHand.m_currentInteractable == null)
            {
                return true;
            }
            if (__instance.isPalmable && collider != null && __instance.HoveredOverRound != null && collider.gameObject.CompareTag("FVRFireArmRound") && collider.gameObject == __instance.HoveredOverRound.gameObject)
            {
                __instance.HoveredOverRound = null;
            }

            return false;
        }
    }

    [HarmonyPatch(typeof(Revolver))]
    public static class TempRevolverPatchWahoo
    {
        [HarmonyPatch("Awake")]
        [HarmonyPostfix]
        public static void AwakePatch(Revolver __instance)
        {
            foreach (FVRFireArmChamber chamber in __instance.Chambers)
            {
                if (chamber.IsManuallyChamberable)
                {
                    chamber.IsManuallyExtractable = true;
                }
            }
        }
    }
}
