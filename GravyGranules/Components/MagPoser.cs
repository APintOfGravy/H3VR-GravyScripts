using System.Collections.Generic;
using FistVR;
using UnityEngine;
using OpenScripts2;
using HarmonyLib;
using System.Linq;

namespace GravyScripts
{
    // I'm so sorry City
    public class MagazinePoseCyclerPlus : MagazinePoseCycler
    {
        static MagazinePoseCyclerPlus()
        {
            Harmony.CreateAndPatchAll(typeof(MagazinePoseCyclerPlus));
        }

        [HarmonyPatch(typeof(MagazinePoseCycler), "Awake")]
        [HarmonyPostfix]
        public static void AwakePatch(MagazinePoseCycler __instance)
        {
            if (__instance is MagazinePoseCyclerPlus)
            {
                __instance.AlternatePoseOverrides[__instance.AlternatePoseOverrides.Count - 1] = Instantiate(__instance.Magazine.PoseOverride, __instance.gameObject.transform);
            }
            // __instance.AlternatePoseOverrides.Add(Instantiate(__instance.Magazine.PoseOverride, __instance.gameObject.transform));
        }

        [HarmonyPatch(typeof(MagazinePoseCycler), "UpdatePose")]
        [HarmonyPrefix]
        public static bool UpdatePoseFix(MagazinePoseCycler __instance)
        {
            if (__instance is MagazinePoseCyclerPlus)
            {
                FVRViveHand hand = __instance.Magazine.m_hand;
                if ((hand != null) && !hand.IsThisTheRightHand)
                {
                    int pose = (int)AccessTools.Field(typeof(MagazinePoseCycler), "_poseIndex").GetValue(__instance);

                    Vector3 posePositionLocal = __instance.AlternatePoseOverrides[pose].transform.localPosition;
                    Vector3 epicEulerMoment = __instance.AlternatePoseOverrides[pose].transform.localEulerAngles;
                    Quaternion dingusThing = new();
                    dingusThing.eulerAngles = new Vector3(epicEulerMoment.x, epicEulerMoment.y * -1, epicEulerMoment.z * -1);
                    __instance.Magazine.PoseOverride.localPosition = new Vector3(posePositionLocal.x * -1, posePositionLocal.y, posePositionLocal.z);
                    __instance.Magazine.PoseOverride.localRotation = dingusThing;

                    /*
                    __instance.Magazine.PoseOverride.localPosition = __instance.AlternatePoseOverrides[(int)AccessTools.Field(typeof(MagazinePoseCycler), "_poseIndex").GetValue(__instance)].localPosition + (Vector3)AccessTools.Field(typeof(MagazinePoseCycler), "_positionalOffset").GetValue(__instance);
                    __instance.Magazine.PoseOverride.localRotation = (Quaternion)AccessTools.Field(typeof(MagazinePoseCycler), "_rotationalOffset").GetValue(__instance) * thing;
                    */
                    return false;
                }
            }
            return true;
        }

        [HarmonyPatch(typeof(MagazinePoseCycler), "FVRPhysicalObject_UpdatePosesBasedOnCMode")]
        [HarmonyPrefix]
        public static void FVRPhysicalObject_UpdatePosesBasedOnCMode(FVRPhysicalObject self, FVRViveHand hand)
        {
            Dictionary<FVRPhysicalObject, MagazinePoseCycler> _existingMagazinePoseCyclers = (Dictionary<FVRPhysicalObject, MagazinePoseCycler>)AccessTools.Field(typeof(MagazinePoseCycler), "_existingMagazinePoseCyclers").GetValue(null);
            if (_existingMagazinePoseCyclers.TryGetValue(self, out MagazinePoseCycler thing) && thing is MagazinePoseCyclerPlus)
            {
                MagazinePoseCyclerPlus mgcp = thing as MagazinePoseCyclerPlus;
                mgcp.UpdatePosesBasedOnCMode(self, hand, _existingMagazinePoseCyclers);
            }
        }

        private void UpdatePosesBasedOnCMode(FVRPhysicalObject self, FVRViveHand hand, Dictionary<FVRPhysicalObject, MagazinePoseCycler> _existingMagazinePoseCyclers)
        {
            float lowestAngle = 1000;
            int lowestDistance = 1000;
            if ((SelectPoseBasedOnAngle == true) && _existingMagazinePoseCyclers.ContainsKey(self))
            {
                for (int i = 0; i < AlternatePoseOverrides.Count; i++)
                {
                    Vector3 posePosition = AlternatePoseOverrides[i].transform.position;
                    Quaternion poseAngle = AlternatePoseOverrides[i].transform.rotation;

                    if (!hand.IsThisTheRightHand && false)
                    {
                        // Handles flipping things around for the left hand, 
                        Vector3 posePositionLocal = AlternatePoseOverrides[i].transform.localPosition;
                        Vector3 epicEulerMoment = AlternatePoseOverrides[i].transform.localEulerAngles;
                        posePosition = posePosition - posePositionLocal + new Vector3(posePositionLocal.x * -1, posePositionLocal.y, posePositionLocal.z);
                        poseAngle.eulerAngles = new Vector3(epicEulerMoment.x, epicEulerMoment.y * -1, epicEulerMoment.z * -1);
                    }

                    float distance = Vector3.Distance(hand.PalmTransform.position, posePosition);
                    float angle = Quaternion.Angle(hand.PoseOverride.rotation, poseAngle);

                    Debug.logger.Log(LogType.Log, $"Angle on grab was {angle * -1 + 180} from {hand.PalmTransform.rotation} and {AlternatePoseOverrides[i].transform.rotation}");
                    Debug.logger.Log(LogType.Log, $"Distance on grab was {distance} from {hand.PalmTransform.position} and {posePosition}");

                    if ((angle == lowestAngle) && (distance < lowestDistance))
                    {
                        lowestAngle = angle;
                        Debug.logger.Log(LogType.Log, $"Setting angle to pose {i}");
                        AccessTools.Field(typeof(MagazinePoseCycler), "_poseIndex").SetValue(this, i);
                    }
                    else if ((distance < 0.4) && angle < lowestAngle)
                    {
                        lowestAngle = angle;
                        Debug.logger.Log(LogType.Log, $"Setting angle to pose {i}");
                        AccessTools.Field(typeof(MagazinePoseCycler), "_poseIndex").SetValue(this, i);
                    }
                }
            }
        }

        public bool SelectPoseBasedOnAngle;
    }
}
