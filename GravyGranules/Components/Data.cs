using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FistVR;
using UnityEngine;
using OpenScripts2;

namespace GravyScripts.Components
{
    public class RevolverData : MonoBehaviour
    {
        public HingeJoint RevolverHinge = null;
        public float RotLimit;
        public float EjectSpeed = 1000f;
    }

    public class ChamberData : MonoBehaviour
    {
        public static List<FireArmRoundType> FrontLoadedAmmo = [
            FireArmRoundType.aRPG7Rocket,
            FireArmRoundType.a40mmCaseless,
            FireArmRoundType.aSturmPistole,
            FireArmRoundType.aDarkMatterLemon,
            FireArmRoundType.aGrappleBolt,
            FireArmRoundType.aRemoteMissile,
            FireArmRoundType.a60mmMortar,
            FireArmRoundType.mf_rocket,
            FireArmRoundType.aCannon367Inch,
            FireArmRoundType.aHCBBolt,
            FireArmRoundType.a50mmPotato
        ];

        public bool ShouldUseChamberAnimations = true;
        public LoadAngle LoadDirection = LoadAngle.FrontFirst;
        public LoadTo LoadPivotPoint = LoadTo.Back;

        [HideInInspector]
        public Vector3 DefaultPosition = Vector3.zero;
        [HideInInspector]
        public Vector3 DefaultBoltPosition = Vector3.zero;
        [HideInInspector]
        public bool IsGrabbingRound = false;
    }

    public class AmmoData : MonoBehaviour
    {
        public float PosSpeed = 1f;
        public float RotSpeed = 240f;

        [HideInInspector]
        public bool ShouldScan = true;
        [HideInInspector]
        public bool IsBeingWiggled = false;
        [HideInInspector]
        public bool IsBeingChambered = false;
        [HideInInspector]
        public bool IsRepalming = false;

        [HideInInspector]
        public float TimeSinceChamberStarted = 0f;
        [HideInInspector]
        public float TimeSinceChambered = 0f;
        [HideInInspector]
        public float UnchamberedAgo = 0f;

        [HideInInspector]
        public GameObject OrientalTransform = null;
        [HideInInspector]
        public GameObject PivotTarget = null;

        [HideInInspector]
        public Vector3 TargetPosLastFrame = Vector3.zero;
        [HideInInspector]
        public Vector3 TargetRotLastFrame = Vector3.zero;

        [HideInInspector]
        public Vector3 OriginalPoseRot = Vector3.zero;
        [HideInInspector]
        public bool IsInTrigger = true;

        [HideInInspector]
        public float UnfiredLength = 0f;
        [HideInInspector]
        public float FiredLength = 0f;
        [HideInInspector]
        public Dictionary<float, float> Horror = [];


        // This is a supplement to FVRFireArmRound lacking it's own bespoke Update() method.
        public void Update()
        {
            TimeSinceChambered += Time.deltaTime;
            UnchamberedAgo += Time.deltaTime;
            TimeSinceChamberStarted += Time.deltaTime;
        }
    }

    public enum LoadAngle
    {
        None,
        FrontFirst,
        BackFirst
    }

    public enum LoadTo
    {
        None,
        Front,
        Center,
        Back
    }
}
