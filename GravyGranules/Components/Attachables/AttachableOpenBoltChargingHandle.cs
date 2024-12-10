using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FistVR;
using UnityEngine;

namespace GravyScripts.Components
{
    public class AttachableOpenBoltChargingHandle : FVRInteractiveObject
    {
        public override void Awake()
        {
            base.Awake();
            m_boltZ_forward = Point_Fore.localPosition.z;
            m_boltZ_rear = Point_Rear.localPosition.z;
            m_currentHandleZ = transform.localPosition.z;
        }

        public override void UpdateInteraction(FVRViveHand hand)
        {
            base.UpdateInteraction(hand);
            Vector3 closestValidPoint = GetClosestValidPoint(Point_Fore.position, Point_Rear.position, m_hand.Input.Pos);
            transform.position = closestValidPoint;
            m_currentHandleZ = transform.localPosition.z;
            float num = Mathf.InverseLerp(m_boltZ_forward, m_boltZ_rear, m_currentHandleZ);
            Bolt.ChargingHandleHeld(num);
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
            if (HasRotatingPart)
            {
                RotatingPart.localEulerAngles = RotatingPartNeutralEulers;
            }
            base.EndInteraction(hand);
            Bolt.ChargingHandleReleased();
        }

        public override void FVRUpdate()
        {
            base.FVRUpdate();
            if (!IsHeld && Mathf.Abs(m_currentHandleZ - m_boltZ_forward) > 0.001f)
            {
                m_currentHandleZ = Mathf.MoveTowards(m_currentHandleZ, m_boltZ_forward, Time.deltaTime * ForwardSpeed);
                    transform.localPosition = new Vector3(transform.localPosition.x, transform.localPosition.y, m_currentHandleZ);
            }
            if (Mathf.Abs(m_currentHandleZ - m_boltZ_forward) < 0.005f)
            {
                CurPos = BoltHandlePos.Forward;
            }
            else if (Mathf.Abs(m_currentHandleZ - m_boltZ_rear) < 0.005f)
            {
                CurPos = BoltHandlePos.Rear;
            }
            else
            {
                CurPos = BoltHandlePos.Middle;
            }
            if (CurPos == BoltHandlePos.Forward && LastPos != BoltHandlePos.Forward)
            {
                if (Receiver != null)
                {
                    Receiver.PlayAudioEvent(FirearmAudioEventType.HandleForward, 1f);
                }
            }
            else if (CurPos == BoltHandlePos.Rear && LastPos != BoltHandlePos.Rear && Receiver != null)
            {
                Receiver.PlayAudioEvent(FirearmAudioEventType.HandleBack, 1f);
            }
            LastPos = CurPos;
        }

        [Header("ChargingHandle")]
        public AttachableOpenBoltReceiver Receiver;
        public Transform Point_Fore;
        public Transform Point_Rear;
        public AttachableOpenBoltReceiverBolt Bolt;
        public float ForwardSpeed = 1f;
        private float m_boltZ_forward;
        private float m_boltZ_rear;
        private float m_currentHandleZ;
        public BoltHandlePos CurPos;
        public BoltHandlePos LastPos;

        [Header("Rotating Bit")]
        public bool HasRotatingPart;
        public Transform RotatingPart;
        public Vector3 RotatingPartNeutralEulers;
        public Vector3 RotatingPartLeftEulers;
        public Vector3 RotatingPartRightEulers;
        public enum BoltHandlePos
        {
            Forward,
            Middle,
            Rear
        }
    }
}
