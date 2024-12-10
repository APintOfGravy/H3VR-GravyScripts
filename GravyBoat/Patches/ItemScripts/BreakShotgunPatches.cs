using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HarmonyLib;
using FistVR;
using UnityEngine;

namespace GravyScripts
{
    [HarmonyPatch(typeof(FVRShotgunForegrip))]
    public static class BreakActionForegripPatches
    {
        public static Dictionary<FVRShotgunForegrip, Vector3> CylLocalPosStart = [];
        public static Dictionary<FVRShotgunForegrip, float> OrigAngOffset = [];
        public static Dictionary<FVRShotgunForegrip, Transform> Measuriser = [];

        public static float InitialDamp = 0.05f;
        public static float InitialSpring = 0.05f;

        [HarmonyPatch("Awake")]
        [HarmonyPrefix]
        public static bool UpdateInteractionPatch(FVRShotgunForegrip __instance)
        {
            __instance.Hinge.useSpring = false;

            return true;
        }

        [HarmonyPatch("UpdateInteraction")]
        [HarmonyPrefix]
        public static bool UpdateInteractionPatch(FVRShotgunForegrip __instance, FVRViveHand hand)
        {
            if (!Measuriser.ContainsKey(__instance))
            {
                Measuriser.Add(__instance, new GameObject("Measurisationator").transform);
                Measuriser[__instance].SetParent(__instance.Wep.transform);
                Measuriser[__instance].localPosition = __instance.transform.localPosition;
            }

            float OrigAng = 0f;

            if (OrigAngOffset.ContainsKey(__instance))
            {
                OrigAng = OrigAngOffset[__instance];
            }

            Vector3 vector = hand.Input.Pos - __instance.Hinge.transform.position;
            Vector3 vector2;

            vector2 = Vector3.ProjectOnPlane(vector, __instance.Wep.transform.right);
            /*
            if (Vector3.Angle(vector2, -__instance.Wep.transform.up) > 90f)
            {
                vector2 = __instance.Wep.transform.forward;
            }
            else if (Vector3.Angle(vector2, __instance.Wep.transform.forward) > 90f)
            {
                vector2 = -__instance.Wep.transform.up;
            }
            */

            // num = Vector3.Angle(vector2, __instance.Wep.transform.forward) + OrigAngle;
            // shoutout to https://stackoverflow.com/questions/19675676/calculating-actual-angle-between-two-vectors-in-unity3d, what the fuck is a sign
            float angle = Vector3.Angle(vector2, Measuriser[__instance].forward);
            float sign = Mathf.Sign(Vector3.Dot(-__instance.Wep.transform.right, Vector3.Cross(vector2, Measuriser[__instance].forward)));

            float num = angle * sign - OrigAng;

            if (!OrigAngOffset.ContainsKey(__instance))
            {
                OrigAngOffset.Add(__instance, num);
            }
            __instance.Hinge.useSpring = true;
            JointSpring spring = __instance.Hinge.spring;
            spring.spring = 10f;
            spring.damper = 0f;
            spring.targetPosition = Mathf.Clamp(num, 0, __instance.Wep.HingeLimit);
            __instance.Hinge.spring = spring;
            __instance.Hinge.transform.localPosition = CylLocalPosStart[__instance];

            return true;
        }

        [HarmonyPatch("EndInteraction")]
        [HarmonyPrefix]
        public static bool EndInteractionPatch(FVRShotgunForegrip __instance, FVRViveHand hand)
        {
            if (OrigAngOffset.ContainsKey(__instance))
            {
                OrigAngOffset.Remove(__instance);
            }
            __instance.Hinge.useSpring = false;
            JointSpring spring = __instance.Hinge.spring;
            spring.spring = InitialSpring;
            spring.damper = InitialDamp;
            spring.targetPosition = 45f;
            __instance.Hinge.spring = spring;

            return true;
        }
    }
}
