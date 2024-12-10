using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using FistVR;
using GravyScripts.Components;
using UnityEngine;

namespace GravyScripts.Components
{
    public class AttachableOpenBoltReceiverBolt : FVRInteractiveObject
    {
        // Token: 0x060023FA RID: 9210 RVA: 0x001253F4 File Offset: 0x001237F4
        public override void Awake()
        {
            base.Awake();
            m_boltZ_current = base.transform.localPosition.z;
            m_boltZ_forward = Point_Bolt_Forward.localPosition.z;
            m_boltZ_lock = Point_Bolt_LockPoint.localPosition.z;
            m_boltZ_rear = Point_Bolt_Rear.localPosition.z;
            if (Point_Bolt_SafetyCatch != null && UsesRotatingSafety)
            {
                m_boltZ_safetyCatch = Point_Bolt_SafetyCatch.localPosition.z;
                m_boltZ_safetyrotLimit = Point_Bolt_SafetyRotLimit.localPosition.z;
                m_hasSafetyCatch = true;
                m_currentBoltRot = BoltRot_Standard;
            }
        }

        // Token: 0x060023FB RID: 9211 RVA: 0x001254CE File Offset: 0x001238CE
        public override void UpdateInteraction(FVRViveHand hand)
        {
            base.UpdateInteraction(hand);
        }

        // Token: 0x060023FC RID: 9212 RVA: 0x001254D7 File Offset: 0x001238D7
        public override void EndInteraction(FVRViveHand hand)
        {
            base.EndInteraction(hand);
        }

        // Token: 0x060023FD RID: 9213 RVA: 0x001254E0 File Offset: 0x001238E0
        public void ChargingHandleHeld(float l)
        {
            m_isChargingHandleHeld = true;
            m_chargingHandleLerp = l;
        }

        // Token: 0x060023FE RID: 9214 RVA: 0x001254F0 File Offset: 0x001238F0
        public void ChargingHandleReleased()
        {
            m_isChargingHandleHeld = false;
            m_chargingHandleLerp = 0f;
        }

        // Token: 0x060023FF RID: 9215 RVA: 0x00125504 File Offset: 0x00123904
        public float GetBoltLerpBetweenLockAndFore()
        {
            return Mathf.InverseLerp(m_boltZ_lock, m_boltZ_forward, m_boltZ_current);
        }

        // Token: 0x06002400 RID: 9216 RVA: 0x0012551D File Offset: 0x0012391D
        public void SetBoltToRear()
        {
            m_boltZ_current = m_boltZ_rear;
        }

        // Token: 0x06002401 RID: 9217 RVA: 0x0012552C File Offset: 0x0012392C
        public void UpdateBolt()
        {
            bool flag = false;
            if (base.IsHeld || m_isChargingHandleHeld)
            {
                flag = true;
            }
            float boltZ_current = m_boltZ_current;
            if (base.IsHeld)
            {
                Vector3 vector = base.GetClosestValidPoint(Point_Bolt_Forward.position, Point_Bolt_Rear.position, m_hand.Input.Pos);
                if (UseBoltTransformRootOverride)
                {
                    vector = BoltTransformOverride.InverseTransformPoint(vector);
                }
                else
                {
                    vector = Receiver.transform.InverseTransformPoint(vector);
                }
                m_boltZ_heldTarget = vector.z;
            }
            else if (m_isChargingHandleHeld)
            {
                m_boltZ_heldTarget = Mathf.Lerp(m_boltZ_forward, m_boltZ_rear, m_chargingHandleLerp);
            }
            Vector2 vector2 = new Vector2(m_boltZ_rear, m_boltZ_forward);
            if (m_boltZ_current <= m_boltZ_lock && Receiver.IsBoltCatchEngaged())
            {
                vector2 = new Vector2(m_boltZ_rear, m_boltZ_lock);
            }
            bool flag2 = false;
            if (m_hasSafetyCatch)
            {
                float num = m_currentBoltRot;
                float num2 = Mathf.InverseLerp(Mathf.Min(BoltRot_Standard, BoltRot_Safe), Mathf.Max(BoltRot_Standard, BoltRot_Safe), num);
                if (base.IsHeld)
                {
                    if (m_boltZ_current < m_boltZ_safetyrotLimit)
                    {
                        Vector3 vector3 = m_hand.Input.Pos - base.transform.position;
                        vector3 = Vector3.ProjectOnPlane(vector3, base.transform.forward).normalized;
                        Vector3 up = Receiver.transform.up;
                        num = Mathf.Atan2(Vector3.Dot(base.transform.forward, Vector3.Cross(up, vector3)), Vector3.Dot(up, vector3)) * 57.29578f;
                        num = Mathf.Clamp(num, Mathf.Min(BoltRot_Standard, BoltRot_Safe), Mathf.Max(BoltRot_Standard, BoltRot_Safe));
                    }
                }
                else if (!m_isChargingHandleHeld)
                {
                    if (num2 <= 0.5f)
                    {
                        num = Mathf.Min(BoltRot_Standard, BoltRot_Safe);
                    }
                    else
                    {
                        num = Mathf.Max(BoltRot_Standard, BoltRot_Safe);
                    }
                }
                if (Mathf.Abs(num - BoltRot_Safe) < BoltRot_SlipDistance)
                {
                    vector2 = new Vector2(m_boltZ_rear, m_boltZ_safetyCatch);
                    flag2 = true;
                }
                else if (Mathf.Abs(num - BoltRot_Standard) >= BoltRot_SlipDistance)
                {
                    vector2 = new Vector2(m_boltZ_rear, m_boltZ_safetyrotLimit);
                }
                if (Mathf.Abs(num - m_currentBoltRot) > 0.1f)
                {
                    base.transform.localEulerAngles = new Vector3(0f, 0f, num);
                }
                m_currentBoltRot = num;
            }
            if (flag)
            {
                m_curBoltSpeed = 0f;
            }
            else if (m_curBoltSpeed >= 0f || CurPos >= BoltPos.Locked)
            {
                m_curBoltSpeed = Mathf.MoveTowards(m_curBoltSpeed, BoltSpeed_Forward, Time.deltaTime * BoltSpringStiffness);
            }
            float num3 = m_boltZ_current;
            float num4 = m_boltZ_current;
            if (flag)
            {
                num4 = m_boltZ_heldTarget;
            }
            if (flag)
            {
                num3 = Mathf.MoveTowards(m_boltZ_current, num4, BoltSpeed_Held * Time.deltaTime);
            }
            else
            {
                num3 = m_boltZ_current + m_curBoltSpeed * Time.deltaTime;
            }
            num3 = Mathf.Clamp(num3, vector2.x, vector2.y);
            if (Mathf.Abs(num3 - m_boltZ_current) > Mathf.Epsilon)
            {
                m_boltZ_current = num3;
                base.transform.localPosition = new Vector3(base.transform.localPosition.x, base.transform.localPosition.y, m_boltZ_current);
                if (SlidingPieces.Length > 0)
                {
                    float z = Point_Bolt_Rear.localPosition.z;
                    for (int i = 0; i < SlidingPieces.Length; i++)
                    {
                        Vector3 localPosition = SlidingPieces[i].Piece.localPosition;
                        float num5 = Mathf.Lerp(m_boltZ_current, z, SlidingPieces[i].DistancePercent);
                        SlidingPieces[i].Piece.localPosition = new Vector3(localPosition.x, localPosition.y, num5);
                    }
                }
                if (Spring != null)
                {
                    float num6 = Mathf.InverseLerp(m_boltZ_rear, m_boltZ_forward, m_boltZ_current);
                    Spring.localScale = new Vector3(1f, 1f, Mathf.Lerp(SpringScales.x, SpringScales.y, num6));
                }
            }
            else
            {
                m_curBoltSpeed = 0f;
            }
            BoltPos boltPos = CurPos;
            if (Mathf.Abs(m_boltZ_current - m_boltZ_forward) < 0.001f)
            {
                boltPos = BoltPos.Forward;
            }
            else if (Mathf.Abs(m_boltZ_current - m_boltZ_lock) < 0.001f)
            {
                boltPos = BoltPos.Locked;
            }
            else if (Mathf.Abs(m_boltZ_current - m_boltZ_rear) < 0.001f)
            {
                boltPos = BoltPos.Rear;
            }
            else if (m_boltZ_current > m_boltZ_lock)
            {
                boltPos = BoltPos.ForwardToMid;
            }
            else
            {
                boltPos = BoltPos.LockedToRear;
            }
            CurPos = boltPos;
            if (m_hasSafetyCatch && !IsHeld && flag2 && Mathf.Abs(m_boltZ_current - m_boltZ_safetyCatch) < 0.001f && Mathf.Abs(boltZ_current - m_boltZ_safetyCatch) >= 0.001f)
            {
                Receiver.PlayAudioEvent(FirearmAudioEventType.CatchOnSear, 1f);
            }
            if (CurPos == BoltPos.Rear && LastPos != BoltPos.Rear)
            {
                BoltEvent_BoltSmackRear();
            }
            if (CurPos == BoltPos.Locked && LastPos != BoltPos.Locked)
            {
                BoltEvent_BoltCaught();
            }
            if (CurPos >= BoltPos.Locked && LastPos < BoltPos.Locked)
            {
                BoltEvent_EjectRound();
            }
            if (CurPos < BoltPos.Locked && LastPos > BoltPos.ForwardToMid)
            {
                BoltEvent_BeginChambering();
            }
            if (CurPos == BoltPos.Forward && LastPos != BoltPos.Forward)
            {
                BoltEvent_ArriveAtFore();
            }
            if (!IsBoltLockbackRequiredForChamberAccessibility && CurPos != BoltPos.Forward)
            {
                Receiver.Chamber.IsAccessible = true;
            }
            else if (CurPos == BoltPos.LockedToRear || CurPos == BoltPos.Rear)
            {
                Receiver.Chamber.IsAccessible = true;
            }
            else
            {
                Receiver.Chamber.IsAccessible = false;
            }
            if (HasLockLatch)
            {
                float num7;
                if (CurPos == BoltPos.Forward && !base.IsHeld)
                {
                    num7 = 1f;
                }
                else
                {
                    num7 = 0f;
                }
                if (Mathf.Abs(num7 - m_lockLatchLerp) > 0.001f)
                {
                    m_lockLatchLerp = num7;
                    Receiver.SetAnimatedComponent(LockLatch, m_lockLatchLerp, LockLatch_Interp, LockLatch_SafetyAxis);
                }
            }
            LastPos = CurPos;
        }

        // Token: 0x06002402 RID: 9218 RVA: 0x00125CF0 File Offset: 0x001240F0
        private void BoltEvent_ArriveAtFore()
        {
            if (Receiver.ChamberRound())
            {
            }
            if (m_doesFiringPinStrikeOnArrivalAtFore && Receiver.Fire(true))
            {
                ImpartFiringImpulse();
            }
            if (base.IsHeld || m_isChargingHandleHeld)
            {
                Receiver.PlayAudioEvent(FirearmAudioEventType.BoltSlideForwardHeld, 1f);
            }
            else
            {
                Receiver.PlayAudioEvent(FirearmAudioEventType.BoltSlideForward, 1f);
            }
        }

        // Token: 0x06002403 RID: 9219 RVA: 0x00125D70 File Offset: 0x00124170
        public void ImpartFiringImpulse()
        {
            m_curBoltSpeed = BoltSpeed_Rearward;
        }

        // Token: 0x06002404 RID: 9220 RVA: 0x00125D7E File Offset: 0x0012417E
        private void BoltEvent_BoltCaught()
        {
            if (Receiver.IsBoltCatchEngaged())
            {
                Receiver.PlayAudioEvent(FirearmAudioEventType.CatchOnSear, 1f);
            }
        }

        // Token: 0x06002405 RID: 9221 RVA: 0x00125DA1 File Offset: 0x001241A1
        public void BoltEvent_EjectRound()
        {
            Receiver.EjectExtractedRound();
            Receiver.PlayAudioEvent(FirearmAudioEventType.MagazineEjectRound, 1f);
        }

        // Token: 0x06002406 RID: 9222 RVA: 0x00125DC0 File Offset: 0x001241C0
        private void BoltEvent_BeginChambering()
        {
            Receiver.BeginChamberingRound();
        }

        // Token: 0x06002407 RID: 9223 RVA: 0x00125DD0 File Offset: 0x001241D0
        private void BoltEvent_BoltSmackRear()
        {
            if (IsHeld || (m_isChargingHandleHeld && Receiver.AudioClipSet.BoltSlideBackHeld.Clips.Count > 0))
            {
                Receiver.PlayAudioEvent(FirearmAudioEventType.BoltSlideBackHeld, 1f);
            }
            else
            {
                Receiver.PlayAudioEvent(FirearmAudioEventType.BoltSlideBack, 1f);
            }
        }

        // Token: 0x04003DA1 RID: 15777
        [Header("Bolt Config")]
        public AttachableOpenBoltReceiver Receiver;

        // Token: 0x04003DA2 RID: 15778
        public bool IsBoltLockbackRequiredForChamberAccessibility = true;

        // Token: 0x04003DA3 RID: 15779
        public float BoltSpeed_Forward;

        // Token: 0x04003DA4 RID: 15780
        public float BoltSpeed_Rearward;

        // Token: 0x04003DA5 RID: 15781
        public float BoltSpeed_Held;

        // Token: 0x04003DA6 RID: 15782
        public float BoltSpringStiffness = 5f;

        // Token: 0x04003DA7 RID: 15783
        public BoltPos CurPos;

        // Token: 0x04003DA8 RID: 15784
        public BoltPos LastPos;

        // Token: 0x04003DA9 RID: 15785
        public Transform Point_Bolt_Forward;

        // Token: 0x04003DAA RID: 15786
        public Transform Point_Bolt_LockPoint;

        // Token: 0x04003DAB RID: 15787
        public Transform Point_Bolt_Rear;

        // Token: 0x04003DAC RID: 15788
        public Transform Point_Bolt_SafetyCatch;

        // Token: 0x04003DAD RID: 15789
        public Transform Point_Bolt_SafetyRotLimit;

        // Token: 0x04003DAE RID: 15790
        public bool UseBoltTransformRootOverride;

        // Token: 0x04003DAF RID: 15791
        public Transform BoltTransformOverride;

        // Token: 0x04003DB0 RID: 15792
        public bool HasLastRoundBoltHoldOpen;

        // Token: 0x04003DB1 RID: 15793
        public bool UsesRotatingSafety = true;

        // Token: 0x04003DB2 RID: 15794
        private bool m_doesFiringPinStrikeOnArrivalAtFore = true;

        // Token: 0x04003DB3 RID: 15795
        private float m_curBoltSpeed;

        // Token: 0x04003DB4 RID: 15796
        private float m_boltZ_current;

        // Token: 0x04003DB5 RID: 15797
        private float m_boltZ_heldTarget;

        // Token: 0x04003DB6 RID: 15798
        private float m_boltZ_forward;

        // Token: 0x04003DB7 RID: 15799
        private float m_boltZ_lock;

        // Token: 0x04003DB8 RID: 15800
        private float m_boltZ_rear;

        // Token: 0x04003DB9 RID: 15801
        private float m_boltZ_safetyCatch;

        // Token: 0x04003DBA RID: 15802
        private float m_boltZ_safetyrotLimit;

        // Token: 0x04003DBB RID: 15803
        [Header("Safety Catch Config")]
        public float BoltRot_Standard;

        // Token: 0x04003DBC RID: 15804
        public float BoltRot_Safe;

        // Token: 0x04003DBD RID: 15805
        public float BoltRot_SlipDistance;

        // Token: 0x04003DBE RID: 15806
        private bool m_hasSafetyCatch;

        // Token: 0x04003DBF RID: 15807
        private float m_currentBoltRot;

        // Token: 0x04003DC0 RID: 15808
        [Header("Spring Config")]
        public Transform Spring;

        // Token: 0x04003DC1 RID: 15809
        public Vector2 SpringScales;

        // Token: 0x04003DC2 RID: 15810
        public OpenBoltReceiverBolt.BoltSlidingPiece[] SlidingPieces;

        // Token: 0x04003DC3 RID: 15811
        private bool m_isChargingHandleHeld;

        // Token: 0x04003DC4 RID: 15812
        private float m_chargingHandleLerp;

        // Token: 0x04003DC5 RID: 15813
        [Header("Lock Latch Config")]
        public bool HasLockLatch;

        // Token: 0x04003DC6 RID: 15814
        public Transform LockLatch;

        // Token: 0x04003DC7 RID: 15815
        public FVRPhysicalObject.InterpStyle LockLatch_Interp;

        // Token: 0x04003DC8 RID: 15816
        public FVRPhysicalObject.Axis LockLatch_SafetyAxis;

        // Token: 0x04003DC9 RID: 15817
        public Vector2 LockLatch_Range;

        // Token: 0x04003DCA RID: 15818
        private float m_lockLatchLerp;

        // Token: 0x02000626 RID: 1574
        public enum BoltPos
        {
            // Token: 0x04003DCC RID: 15820
            Forward,
            // Token: 0x04003DCD RID: 15821
            ForwardToMid,
            // Token: 0x04003DCE RID: 15822
            Locked,
            // Token: 0x04003DCF RID: 15823
            LockedToRear,
            // Token: 0x04003DD0 RID: 15824
            Rear
        }

        // Token: 0x02000627 RID: 1575
        [Serializable]
        public class BoltSlidingPiece
        {
            // Token: 0x04003DD1 RID: 15825
            public Transform Piece;

            // Token: 0x04003DD2 RID: 15826
            public float DistancePercent;
        }
    }
}

namespace Plugin.Components
{
    public class AttachableOpenBoltReceiverBolt : GravyScripts.Components.AttachableOpenBoltReceiverBolt
    {
        [ContextMenu("Convert to New")]
        void TranslatePrefab()
        {
            GravyScripts.Components.AttachableOpenBoltReceiverBolt newComponent = gameObject.AddComponent<GravyScripts.Components.AttachableOpenBoltReceiverBolt>();
            foreach (FieldInfo field in typeof(GravyScripts.Components.AttachableOpenBoltReceiverBolt).GetFields())
            {
                field.SetValue(newComponent, field.GetValue(this));
            }
        }
    }
}