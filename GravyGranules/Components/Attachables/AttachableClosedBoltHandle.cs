using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FistVR;
using UnityEngine;

namespace GravyScripts.Components
{
    public class AttachableClosedBoltHandle : FVRInteractiveObject
    {
        public override void Awake()
        {
            base.Awake();
            m_posZ_current = transform.localPosition.z;
            m_posZ_forward = Point_Forward.localPosition.z;
            m_posZ_lock = Point_LockPoint.localPosition.z;
            m_posZ_rear = Point_Rear.localPosition.z;
            if (Point_SafetyRotLimit != null && UsesRotation)
            {
                m_posZ_safetyrotLimit = Point_SafetyRotLimit.localPosition.z;
                m_hasRotCatch = true;
                m_currentRot = Rot_Standard;
            }
        }

        public float GetBoltLerpBetweenLockAndFore()
        {
            return Mathf.InverseLerp(m_posZ_lock, m_posZ_forward, m_posZ_current);
        }

        public float GetBoltLerpBetweenRearAndFore()
        {
            return Mathf.InverseLerp(m_posZ_rear, m_posZ_forward, m_posZ_current);
        }

        public bool ShouldControlBolt()
        {
            if (!UsesRotation)
            {
                return IsHeld;
            }
            return IsHeld || m_isAtLockAngle;
        }

        public override void BeginInteraction(FVRViveHand hand)
        {
            if (UsesSoundOnGrab)
            {
                Weapon.PlayAudioEvent(FirearmAudioEventType.HandleGrab, 1f);
            }
            base.BeginInteraction(hand);
        }

        public override void UpdateInteraction(FVRViveHand hand)
        {
            base.UpdateInteraction(hand);
            if (HasRotatingPart)
            {
                Vector3 normalized = (transform.position - m_hand.PalmTransform.position).normalized;
                if (Vector3.Dot(normalized, transform.right) > 0f)
                {
                    RotatingPart.localEulerAngles = RotatingPartLeftEulers;
                }
                else
                {
                    RotatingPart.localEulerAngles = RotatingPartRightEulers;
                }
            }
        }

        public override void EndInteraction(FVRViveHand hand)
        {
            if (HasRotatingPart && !StaysRotatedWhenBack)
            {
                RotatingPart.localEulerAngles = RotatingPartNeutralEulers;
            }
            if (!Weapon.Bolt.IsBoltLocked())
            {
                Weapon.PlayAudioEvent(FirearmAudioEventType.BoltRelease, 1f);
            }
            base.EndInteraction(hand);
        }

        public void UpdateHandle()
        {
            bool flag = false;
            if (IsHeld)
            {
                flag = true;
            }
            if (flag)
            {
                Vector3 closestValidPoint = GetClosestValidPoint(Point_Forward.position, Point_Rear.position, m_hand.Input.Pos);
                m_posZ_heldTarget = Weapon.transform.InverseTransformPoint(closestValidPoint).z;
            }
            Vector2 vector = new Vector2(m_posZ_rear, m_posZ_forward);
            if (m_hasRotCatch)
            {
                float num = m_currentRot;
                if (IsSlappable && m_isAtLockAngle)
                {
                    foreach (FVRViveHand hand in GM.CurrentMovementManager.Hands)
                    {
                        if (hand.CurrentInteractable != Weapon)
                        {
                            float num2 = Vector3.Distance(SlapPoint.position, hand.Input.Pos);
                            float num3 = Vector3.Dot(SlapPoint.forward, hand.Input.VelLinearWorld.normalized);
                            if (num2 < SlapDistance && num3 > 0.3f)
                            {
                                float magnitude = hand.Input.VelLinearWorld.magnitude;
                                if (magnitude > 1f)
                                {
                                    num = Rot_Standard;
                                    Weapon.Bolt.ReleaseBolt();
                                    Weapon.PlayAudioEvent(FirearmAudioEventType.HandleDown, 1f);
                                    if (HasRotatingPart)
                                    {
                                        RotatingPart.localEulerAngles = RotatingPartNeutralEulers;
                                    }
                                }
                            }
                        }
                    }
                }
                float num4 = Mathf.InverseLerp(Mathf.Min(Rot_Standard, Rot_Safe), Mathf.Max(Rot_Standard, Rot_Safe), num);
                if (IsHeld)
                {
                    if (m_posZ_current < m_posZ_safetyrotLimit)
                    {
                        Vector3 vector2 = m_hand.Input.Pos - transform.position;
                        vector2 = Vector3.ProjectOnPlane(vector2, transform.forward).normalized;
                        Vector3 up = Weapon.transform.up;
                        num = Mathf.Atan2(Vector3.Dot(transform.forward, Vector3.Cross(up, vector2)), Vector3.Dot(up, vector2)) * 57.29578f;
                        num = Mathf.Clamp(num, Mathf.Min(Rot_Standard, Rot_Safe), Mathf.Max(Rot_Standard, Rot_Safe));
                    }
                }
                else if (num4 <= 0.5f)
                {
                    num = Mathf.Min(Rot_Standard, Rot_Safe);
                }
                else
                {
                    num = Mathf.Max(Rot_Standard, Rot_Safe);
                }
                if (Mathf.Abs(num - Rot_Safe) < Rot_SlipDistance)
                {
                    vector = new Vector2(m_posZ_rear, m_posZ_lock);
                    m_isAtLockAngle = true;
                }
                else if (Mathf.Abs(num - Rot_Standard) < Rot_SlipDistance)
                {
                    m_isAtLockAngle = false;
                }
                else
                {
                    vector = new Vector2(m_posZ_rear, m_posZ_safetyrotLimit);
                    m_isAtLockAngle = true;
                }
                if (Mathf.Abs(num - m_currentRot) > 0.1f)
                {
                    transform.localEulerAngles = new Vector3(0f, 0f, num);
                }
                m_currentRot = num;
            }
            if (flag)
            {
                m_curSpeed = 0f;
            }
            else if (m_curSpeed >= 0f || CurPos > ClosedBoltHandle.HandlePos.Forward)
            {
                m_curSpeed = Mathf.MoveTowards(m_curSpeed, Speed_Forward, Time.deltaTime * SpringStiffness);
            }
            float num5 = m_posZ_current;
            float num6 = m_posZ_current;
            if (flag)
            {
                num6 = m_posZ_heldTarget;
                num5 = Mathf.MoveTowards(m_posZ_current, num6, Speed_Held * Time.deltaTime);
            }
            else
            {
                num5 = m_posZ_current + m_curSpeed * Time.deltaTime;
            }
            num5 = Mathf.Clamp(num5, vector.x, vector.y);
            if (Mathf.Abs(num5 - m_posZ_current) > Mathf.Epsilon)
            {
                m_posZ_current = num5;
                transform.localPosition = new Vector3(transform.localPosition.x, transform.localPosition.y, m_posZ_current);
            }
            else
            {
                m_curSpeed = 0f;
            }
            ClosedBoltHandle.HandlePos handlePos = CurPos;
            if (Mathf.Abs(m_posZ_current - m_posZ_forward) < 0.001f)
            {
                handlePos = ClosedBoltHandle.HandlePos.Forward;
            }
            else if (Mathf.Abs(m_posZ_current - m_posZ_lock) < 0.001f)
            {
                handlePos = ClosedBoltHandle.HandlePos.Locked;
            }
            else if (Mathf.Abs(m_posZ_current - m_posZ_rear) < 0.001f)
            {
                handlePos = ClosedBoltHandle.HandlePos.Rear;
            }
            else if (m_posZ_current > m_posZ_lock)
            {
                handlePos = ClosedBoltHandle.HandlePos.ForwardToMid;
            }
            else
            {
                handlePos = ClosedBoltHandle.HandlePos.LockedToRear;
            }
            int curPos = (int)CurPos;
            CurPos = (ClosedBoltHandle.HandlePos)Mathf.Clamp((int)handlePos, curPos - 1, curPos + 1);
            if (CurPos == ClosedBoltHandle.HandlePos.Forward && LastPos != ClosedBoltHandle.HandlePos.Forward)
            {
                Event_ArriveAtFore();
            }
            else if (CurPos != ClosedBoltHandle.HandlePos.ForwardToMid || LastPos != ClosedBoltHandle.HandlePos.Forward)
            {
                if (CurPos != ClosedBoltHandle.HandlePos.Locked || LastPos != ClosedBoltHandle.HandlePos.ForwardToMid)
                {
                    if (CurPos != ClosedBoltHandle.HandlePos.ForwardToMid || LastPos != ClosedBoltHandle.HandlePos.Locked)
                    {
                        if (CurPos == ClosedBoltHandle.HandlePos.Locked && LastPos == ClosedBoltHandle.HandlePos.LockedToRear && m_isAtLockAngle)
                        {
                            Event_HitLockPosition();
                        }
                        else if (CurPos == ClosedBoltHandle.HandlePos.Rear && LastPos != ClosedBoltHandle.HandlePos.Rear)
                        {
                            Event_SmackRear();
                        }
                    }
                }
            }
            LastPos = CurPos;
        }

        private void Event_ArriveAtFore()
        {
            Weapon.PlayAudioEvent(FirearmAudioEventType.HandleForward, 1f);
            if (HasRotatingPart)
            {
                RotatingPart.localEulerAngles = RotatingPartNeutralEulers;
            }
        }

        private void Event_HitLockPosition()
        {
            Weapon.PlayAudioEvent(FirearmAudioEventType.HandleUp, 1f);
        }

        private void Event_SmackRear()
        {
            Weapon.PlayAudioEvent(FirearmAudioEventType.HandleBack, 1f);
        }

        [Header("Bolt Handle")]
        public AttachableClosedBoltWeapon Weapon;

        public float Speed_Forward;

        public float Speed_Held;

        public float SpringStiffness = 100f;

        public ClosedBoltHandle.HandlePos CurPos;

        public ClosedBoltHandle.HandlePos LastPos;

        public Transform Point_Forward;

        public Transform Point_LockPoint;

        public Transform Point_Rear;

        public Transform Point_SafetyRotLimit;

        private float m_curSpeed;

        private float m_posZ_current;

        private float m_posZ_heldTarget;

        private float m_posZ_forward;

        private float m_posZ_lock;

        private float m_posZ_rear;

        private float m_posZ_safetyrotLimit;

        [Header("Safety Catch Config")]
        public bool UsesRotation = true;

        public float Rot_Standard;

        public float Rot_Safe;

        public float Rot_SlipDistance;

        public bool IsSlappable;

        public Transform SlapPoint;

        public float SlapDistance = 0.1f;

        private bool m_hasRotCatch;

        private float m_currentRot;

        [Header("Rotating Bit")]
        public bool HasRotatingPart;

        public Transform RotatingPart;

        public Vector3 RotatingPartNeutralEulers;

        public Vector3 RotatingPartLeftEulers;

        public Vector3 RotatingPartRightEulers;

        public bool StaysRotatedWhenBack;

        public bool UsesSoundOnGrab;

        private bool m_isHandleHeld;

        private float m_HandleLerp;

        private bool m_isAtLockAngle;

        public enum HandlePos
        {
            Forward,
            ForwardToMid,
            Locked,
            LockedToRear,
            Rear
        }
    }
}
