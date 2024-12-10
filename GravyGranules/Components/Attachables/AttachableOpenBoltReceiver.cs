using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FistVR;
using UnityEngine;

namespace GravyScripts.Components
{
    public class AttachableOpenBoltReceiver : AttachableFirearm
    {
        // Token: 0x060023DE RID: 9182 RVA: 0x001243DB File Offset: 0x001227DB
        public bool HasExtractedRound()
        {
            return m_proxy.IsFull;
        }

        public bool IsSeerEngaged
        {
            get
            {
                return m_isSeerEngaged;
            }
        }

        public bool IsHammerCocked
        {
            get
            {
                return m_isHammerCocked;
            }
        }

        public int FireSelectorModeIndex
        {
            get
            {
                return m_fireSelectorMode;
            }
        }

        public override void Awake()
        {
            base.Awake();
            m_CamBurst = 0;
            ResetCamBurst();
            GameObject gameObject = new ("m_proxyRound");
            m_proxy = gameObject.AddComponent<FVRFirearmMovingProxyRound>();
            m_proxy.Init(transform);
        }

        public void ResetCamBurst()
        {
            OpenBoltReceiver.FireSelectorMode fireSelectorMode = FireSelector_Modes[m_fireSelectorMode];
            m_CamBurst = fireSelectorMode.BurstAmount;
            m_engagementDelay = 0f;
        }

        public void SecondaryFireSelectorClicked()
        {
            PlayAudioEvent(FirearmAudioEventType.FireSelector, 1f);
        }

        // Token: 0x060023E6 RID: 9190 RVA: 0x00124532 File Offset: 0x00122932
        public bool IsBoltCatchEngaged()
        {
            return m_isSeerEngaged;
        }

        // Token: 0x060023E7 RID: 9191 RVA: 0x0012453A File Offset: 0x0012293A
        public void ReleaseSeer()
        {
            if (m_isSeerEngaged && Bolt.CurPos == AttachableOpenBoltReceiverBolt.BoltPos.Locked)
            {
                PlayAudioEvent(FirearmAudioEventType.Prefire, 1f);
            }
            m_isSeerEngaged = false;
        }

        // Token: 0x060023E8 RID: 9192 RVA: 0x0012456B File Offset: 0x0012296B
        public void EngageSeer()
        {
            m_isSeerEngaged = true;
        }

        // Token: 0x060023E9 RID: 9193 RVA: 0x00124574 File Offset: 0x00122974
        protected virtual void ToggleFireSelector()
        {
            m_engagementDelay = 0f;
            if (FireSelector_Modes.Length > 1)
            {
                m_fireSelectorMode++;
                if (m_fireSelectorMode >= FireSelector_Modes.Length)
                {
                    m_fireSelectorMode -= FireSelector_Modes.Length;
                }
                base.PlayAudioEvent(FirearmAudioEventType.FireSelector, 1f);
                if (FireSelectorSwitch != null)
                {
                    OpenBoltReceiver.InterpStyle fireSelector_InterpStyle = FireSelector_InterpStyle;
                    if (fireSelector_InterpStyle != OpenBoltReceiver.InterpStyle.Rotation)
                    {
                        if (fireSelector_InterpStyle == OpenBoltReceiver.InterpStyle.Translate)
                        {
                            Vector3 zero = Vector3.zero;
                            OpenBoltReceiver.Axis fireSelector_Axis = FireSelector_Axis;
                            if (fireSelector_Axis != OpenBoltReceiver.Axis.X)
                            {
                                if (fireSelector_Axis != OpenBoltReceiver.Axis.Y)
                                {
                                    if (fireSelector_Axis == OpenBoltReceiver.Axis.Z)
                                    {
                                        zero.z = FireSelector_Modes[m_fireSelectorMode].SelectorPosition;
                                    }
                                }
                                else
                                {
                                    zero.y = FireSelector_Modes[m_fireSelectorMode].SelectorPosition;
                                }
                            }
                            else
                            {
                                zero.x = FireSelector_Modes[m_fireSelectorMode].SelectorPosition;
                            }
                            FireSelectorSwitch.localPosition = zero;
                        }
                    }
                    else
                    {
                        Vector3 zero2 = Vector3.zero;
                        OpenBoltReceiver.Axis fireSelector_Axis2 = FireSelector_Axis;
                        if (fireSelector_Axis2 != OpenBoltReceiver.Axis.X)
                        {
                            if (fireSelector_Axis2 != OpenBoltReceiver.Axis.Y)
                            {
                                if (fireSelector_Axis2 == OpenBoltReceiver.Axis.Z)
                                {
                                    zero2.z = FireSelector_Modes[m_fireSelectorMode].SelectorPosition;
                                }
                            }
                            else
                            {
                                zero2.y = FireSelector_Modes[m_fireSelectorMode].SelectorPosition;
                            }
                        }
                        else
                        {
                            zero2.x = FireSelector_Modes[m_fireSelectorMode].SelectorPosition;
                        }
                        FireSelectorSwitch.localEulerAngles = zero2;
                    }
                }
                if (FireSelectorSwitch2 != null)
                {
                    OpenBoltReceiver.InterpStyle fireSelector_InterpStyle2 = FireSelector_InterpStyle2;
                    if (fireSelector_InterpStyle2 != OpenBoltReceiver.InterpStyle.Rotation)
                    {
                        if (fireSelector_InterpStyle2 == OpenBoltReceiver.InterpStyle.Translate)
                        {
                            Vector3 zero3 = Vector3.zero;
                            OpenBoltReceiver.Axis fireSelector_Axis3 = FireSelector_Axis2;
                            if (fireSelector_Axis3 != OpenBoltReceiver.Axis.X)
                            {
                                if (fireSelector_Axis3 != OpenBoltReceiver.Axis.Y)
                                {
                                    if (fireSelector_Axis3 == OpenBoltReceiver.Axis.Z)
                                    {
                                        zero3.z = FireSelector_Modes2[m_fireSelectorMode].SelectorPosition;
                                    }
                                }
                                else
                                {
                                    zero3.y = FireSelector_Modes2[m_fireSelectorMode].SelectorPosition;
                                }
                            }
                            else
                            {
                                zero3.x = FireSelector_Modes2[m_fireSelectorMode].SelectorPosition;
                            }
                            FireSelectorSwitch2.localPosition = zero3;
                        }
                    }
                    else
                    {
                        Vector3 zero4 = Vector3.zero;
                        OpenBoltReceiver.Axis fireSelector_Axis4 = FireSelector_Axis2;
                        if (fireSelector_Axis4 != OpenBoltReceiver.Axis.X)
                        {
                            if (fireSelector_Axis4 != OpenBoltReceiver.Axis.Y)
                            {
                                if (fireSelector_Axis4 == OpenBoltReceiver.Axis.Z)
                                {
                                    zero4.z = FireSelector_Modes2[m_fireSelectorMode].SelectorPosition;
                                }
                            }
                            else
                            {
                                zero4.y = FireSelector_Modes2[m_fireSelectorMode].SelectorPosition;
                            }
                        }
                        else
                        {
                            zero4.x = FireSelector_Modes2[m_fireSelectorMode].SelectorPosition;
                        }
                        FireSelectorSwitch2.localEulerAngles = zero4;
                    }
                }
            }
            ResetCamBurst();
        }

        // Token: 0x060023EA RID: 9194 RVA: 0x00124890 File Offset: 0x00122C90
        public void EjectExtractedRound()
        {
            if (Chamber.IsFull)
            {
                Chamber.EjectRound(RoundPos_Ejection.position, base.transform.right * EjectionSpeed.x + base.transform.up * EjectionSpeed.y + base.transform.forward * EjectionSpeed.z, base.transform.right * EjectionSpin.x + base.transform.up * EjectionSpin.y + base.transform.forward * EjectionSpin.z, RoundPos_Ejection.position, RoundPos_Ejection.rotation, false);
            }
        }

        // Token: 0x060023EB RID: 9195 RVA: 0x00124994 File Offset: 0x00122D94
        public void BeginChamberingRound()
        {
            OpenBoltReceiver.FireSelectorModeType modeType = FireSelector_Modes[m_fireSelectorMode].ModeType;
            OpenBoltReceiver.FireSelectorMode fireSelectorMode = FireSelector_Modes[m_fireSelectorMode];
            if (m_CamBurst > 0)
            {
                m_CamBurst--;
            }
            if (modeType == OpenBoltReceiver.FireSelectorModeType.Single || modeType == OpenBoltReceiver.FireSelectorModeType.SuperFastBurst || (modeType == OpenBoltReceiver.FireSelectorModeType.Burst && m_CamBurst <= 0))
            {
                EngageSeer();
            }
            bool flag = false;
            GameObject gameObject = null;
            /*
            if (HasBelt)
            {
                if (!m_proxy.IsFull && BeltDD.HasARound())
                {
                    if (AudioClipSet.BeltSettlingLimit > 0)
                    {
                        base.PlayAudioEvent(FirearmAudioEventType.BeltSettle, 1f);
                    }
                    flag = true;
                    gameObject = BeltDD.RemoveRound(false);
                }
            }
            else 
            */
            if (!m_proxy.IsFull && Magazine != null && !Magazine.IsBeltBox && Magazine.HasARound())
            {
                flag = true;
                gameObject = Magazine.RemoveRound(false);
            }
            if (!flag)
            {
                return;
            }
            if (flag)
            {
                m_proxy.SetFromPrefabReference(gameObject);
            }
            if (Bolt.HasLastRoundBoltHoldOpen && Magazine != null && !Magazine.HasARound() && Magazine.DoesFollowerStopBolt && !Magazine.IsBeltBox)
            {
                EngageSeer();
            }
        }

        // Token: 0x060023EC RID: 9196 RVA: 0x00124B24 File Offset: 0x00122F24
        public bool ChamberRound()
        {
            if (m_proxy.IsFull && !Chamber.IsFull)
            {
                Chamber.SetRound(m_proxy.Round, false);
                m_proxy.ClearProxy();
                return true;
            }
            return false;
        }

        // Token: 0x060023ED RID: 9197 RVA: 0x00124B76 File Offset: 0x00122F76
        public Transform GetMagMountingTransform()
        {
            if (UsesMagMountTransformOverride)
            {
                return MagMountTransformOverride;
            }
            return null;
        }

        // Token: 0x060023EE RID: 9198 RVA: 0x00124B90 File Offset: 0x00122F90
        public bool Fire(bool firedFromInterface)
        {
            if (!Chamber.Fire())
            {
                return false;
            }
            m_timeSinceFiredShot = 0f;
            FVRFireArm fvrfireArm = null;
            if (OverrideFA != null)
            {
                fvrfireArm = OverrideFA;
                Fire(Chamber, GetMuzzle(), true, fvrfireArm, 1f);
            }
            else if (firedFromInterface && Attachment.curMount != null)
            {
                fvrfireArm = Attachment.curMount.GetRootMount().MyObject as FVRFireArm;
                if (fvrfireArm != null)
                {
                    Fire(Chamber, GetMuzzle(), true, fvrfireArm, 1f);
                }
                else
                {
                    Fire(Chamber, GetMuzzle(), true, null, 1f);
                }
            }
            else
            {
                Fire(Chamber, GetMuzzle(), true, null, 1f);
            }
            FireMuzzleSmoke();
            /*
            if (UsesDelinker && HasBelt)
            {
                DelinkerSystem.Emit(1);
            }
            if (HasBelt)
            {
                BeltDD.PopEjectFlaps();
                BeltDD.AddJitter();
            }
            */
            Recoil(firedFromInterface, fvrfireArm);
            bool flag4 = false;
            OpenBoltReceiver.FireSelectorMode fireSelectorMode = FireSelector_Modes[m_fireSelectorMode];
            if (fireSelectorMode.ModeType == OpenBoltReceiver.FireSelectorModeType.SuperFastBurst)
            {
                for (int i = 1; i < SuperBurstAmount; i++)
                {
                    if (Magazine.HasARound())
                    {
                        Magazine.RemoveRound();
                        base.Fire(Chamber, GetMuzzle(), false, fvrfireArm, -1f);
                        flag4 = true;
                        FireMuzzleSmoke();
                        Recoil(firedFromInterface, fvrfireArm);
                    }
                }
            }
            if (UsesRecoilingSystem)
            {
                if (flag4)
                {
                    RecoilingSystem.Recoil(true);
                }
                else
                {
                    RecoilingSystem.Recoil(false);
                }
            }
            if (flag4)
            {
                base.PlayAudioGunShot(false, Chamber.GetRound().TailClass, Chamber.GetRound().TailClassSuppressed, GM.CurrentPlayerBody.GetCurrentSoundEnvironment());
            }
            else
            {
                base.PlayAudioGunShot(Chamber.GetRound(), GM.CurrentPlayerBody.GetCurrentSoundEnvironment(), 1f);
            }
            return true;
        }

        // Token: 0x060023EF RID: 9199 RVA: 0x00124D60 File Offset: 0x00123160
        public override void Update()
        {
            base.Update();

            if (m_engagementDelay > 0f)
            {
                m_engagementDelay -= Time.deltaTime;
            }
            Bolt.UpdateBolt();
            UpdateDisplayRoundPositions();
            if (m_timeSinceFiredShot < 1f)
            {
                m_timeSinceFiredShot += Time.deltaTime;
            }

        }

        // Token: 0x060023F0 RID: 9200 RVA: 0x00124DD0 File Offset: 0x001231D0
        public override void ProcessInput(FVRViveHand hand, bool fromInterface, FVRInteractiveObject o)
        {
            base.ProcessInput(hand, fromInterface, o);

            if (o.m_hasTriggeredUpSinceBegin && FireSelector_Modes[m_fireSelectorMode].ModeType != OpenBoltReceiver.FireSelectorModeType.Safe)
            {
                m_triggerFloat = hand.Input.TriggerFloat;
            }
            else
            {
                m_triggerFloat = 0f;
            }

            if (m_triggerFloat <= 0f)
            {
                EngageSeer();
            }

            if (Trigger != null)
            {
                if (TriggerInterpStyle == OpenBoltReceiver.InterpStyle.Translate)
                {
                    Trigger.localPosition = new Vector3(0f, 0f, Mathf.Lerp(Trigger_ForwardValue, Trigger_RearwardValue, m_triggerFloat));
                }
                else if (TriggerInterpStyle == OpenBoltReceiver.InterpStyle.Rotation)
                {
                    Trigger.localEulerAngles = new Vector3(Mathf.Lerp(Trigger_ForwardValue, Trigger_RearwardValue, m_triggerFloat), 0f, 0f);
                }
            }

            bool flag = false;
            if (Bolt.HasLastRoundBoltHoldOpen && Magazine != null && !Magazine.HasARound() && !Magazine.IsBeltBox)
            {
                flag = true;
            }
            if (!m_hasTriggerCycled && m_engagementDelay <= 0f)
            {
                if (m_triggerFloat >= TriggerFiringThreshold)
                {
                    m_hasTriggerCycled = true;
                    if (!flag)
                    {
                        ReleaseSeer();
                    }
                }
            }
            else if (m_triggerFloat <= TriggerResetThreshold && m_hasTriggerCycled)
            {
                EngageSeer();
                m_hasTriggerCycled = false;
                OpenBoltReceiver.FireSelectorModeType modeType = FireSelector_Modes[m_fireSelectorMode].ModeType;
                OpenBoltReceiver.FireSelectorMode fireSelectorMode = FireSelector_Modes[m_fireSelectorMode];
                m_CamBurst = FireSelector_Modes[m_fireSelectorMode].BurstAmount;
                base.PlayAudioEvent(FirearmAudioEventType.TriggerReset, 1f);
            }

            if (hand.IsInStreamlinedMode)
            {
                if (hand.Input.BYButtonDown && HasFireSelectorButton)
                {
                    ToggleFireSelector();
                }
                if (hand.Input.AXButtonDown && HasMagReleaseButton)
                {
                    EjectMag(false);
                }
            }
            else if (hand.Input.TouchpadDown && hand.Input.TouchpadAxes.magnitude > 0.1f)
            {
                if (HasFireSelectorButton && Vector2.Angle(hand.Input.TouchpadAxes, Vector2.left) <= 45f)
                {
                    ToggleFireSelector();
                }
                else if (HasMagReleaseButton && Vector2.Angle(hand.Input.TouchpadAxes, Vector2.down) <= 45f)
                {
                    EjectMag(false);
                }
            }
        }

        // Token: 0x060023F4 RID: 9204 RVA: 0x001251D8 File Offset: 0x001235D8
        private void UpdateDisplayRoundPositions()
        {
            float boltLerpBetweenLockAndFore = Bolt.GetBoltLerpBetweenLockAndFore();
            if (m_proxy.IsFull)
            {
                m_proxy.ProxyRound.position = Vector3.Lerp(RoundPos_MagazinePos.position, Chamber.transform.position, boltLerpBetweenLockAndFore);
                m_proxy.ProxyRound.rotation = Quaternion.Slerp(RoundPos_MagazinePos.rotation, Chamber.transform.rotation, boltLerpBetweenLockAndFore);
            }
            else if (Chamber.IsFull)
            {
                Chamber.ProxyRound.position = Vector3.Lerp(RoundPos_Ejecting.position, Chamber.transform.position, boltLerpBetweenLockAndFore);
                Chamber.ProxyRound.rotation = Quaternion.Slerp(RoundPos_Ejecting.rotation, Chamber.transform.rotation, boltLerpBetweenLockAndFore);
            }
            if (DoesForwardBoltDisableReloadWell)
            {
                if (Bolt.CurPos >= AttachableOpenBoltReceiverBolt.BoltPos.Locked)
                {
                    if (!ReloadTriggerWell.activeSelf)
                    {
                        ReloadTriggerWell.SetActive(true);
                    }
                }
                else if (ReloadTriggerWell.activeSelf)
                {
                    ReloadTriggerWell.SetActive(false);
                }
            }
        }

        public void ReleaseMag()
        {
            if (Magazine != null)
            {
                base.EjectMag(false);
            }
        }

        public void SetAnimatedComponent(Transform t, float val, FVRPhysicalObject.InterpStyle interp, FVRPhysicalObject.Axis axis)
        {
            if (interp != FVRPhysicalObject.InterpStyle.Rotation)
            {
                if (interp == FVRPhysicalObject.InterpStyle.Translate)
                {
                    Vector3 localPosition = t.localPosition;
                    if (axis != FVRPhysicalObject.Axis.X)
                    {
                        if (axis != FVRPhysicalObject.Axis.Y)
                        {
                            if (axis == FVRPhysicalObject.Axis.Z)
                            {
                                localPosition.z = val;
                            }
                        }
                        else
                        {
                            localPosition.y = val;
                        }
                    }
                    else
                    {
                        localPosition.x = val;
                    }
                    t.localPosition = localPosition;
                }
            }
            else
            {
                Vector3 zero = Vector3.zero;
                if (axis != FVRPhysicalObject.Axis.X)
                {
                    if (axis != FVRPhysicalObject.Axis.Y)
                    {
                        if (axis == FVRPhysicalObject.Axis.Z)
                        {
                            zero.z = val;
                        }
                    }
                    else
                    {
                        zero.y = val;
                    }
                }
                else
                {
                    zero.x = val;
                }
                t.localEulerAngles = zero;
            }
        }

        // Token: 0x04003D64 RID: 15716
        [Header("OpenBoltWeapon Config")]
        public bool HasTriggerButton = true;

        // Token: 0x04003D65 RID: 15717
        public bool HasFireSelectorButton = true;

        // Token: 0x04003D66 RID: 15718
        public bool HasMagReleaseButton = true;

        // Token: 0x04003D67 RID: 15719
        public bool DoesForwardBoltDisableReloadWell;

        // Token: 0x04003D68 RID: 15720
        [Header("Component Connections")]
        public AttachableOpenBoltReceiverBolt Bolt;

        // Token: 0x04003D69 RID: 15721
        public FVRFireArmChamber Chamber;

        // Token: 0x04003D6A RID: 15722
        public Transform Trigger;

        // Token: 0x04003D6B RID: 15723
        public Transform MagReleaseButton;

        // Token: 0x04003D6C RID: 15724
        public Transform FireSelectorSwitch;

        // Token: 0x04003D6D RID: 15725
        public Transform FireSelectorSwitch2;

        // Token: 0x04003D6E RID: 15726
        public GameObject ReloadTriggerWell;

        // Token: 0x04003D6F RID: 15727
        [Header("Round Positions")]
        public Transform RoundPos_Ejecting;

        // Token: 0x04003D70 RID: 15728
        public Transform RoundPos_Ejection;

        // Token: 0x04003D71 RID: 15729
        public Transform RoundPos_MagazinePos;

        // Token: 0x04003D72 RID: 15730
        private FVRFirearmMovingProxyRound m_proxy;

        // Token: 0x04003D73 RID: 15731
        public Vector3 EjectionSpeed;

        // Token: 0x04003D74 RID: 15732
        public Vector3 EjectionSpin;

        // Token: 0x04003D75 RID: 15733
        public bool UsesDelinker;

        // Token: 0x04003D76 RID: 15734
        public ParticleSystem DelinkerSystem;

        // Token: 0x04003D77 RID: 15735
        [Header("Trigger Config")]
        public float TriggerFiringThreshold = 0.8f;

        // Token: 0x04003D78 RID: 15736
        public float TriggerResetThreshold = 0.4f;

        // Token: 0x04003D79 RID: 15737
        public float Trigger_ForwardValue;

        // Token: 0x04003D7A RID: 15738
        public float Trigger_RearwardValue;

        // Token: 0x04003D7B RID: 15739
        public OpenBoltReceiver.InterpStyle TriggerInterpStyle = OpenBoltReceiver.InterpStyle.Rotation;

        // Token: 0x04003D7C RID: 15740
        private float m_triggerFloat;

        // Token: 0x04003D7D RID: 15741
        private bool m_hasTriggerCycled;

        // Token: 0x04003D7E RID: 15742
        private bool m_isSeerEngaged = true;

        // Token: 0x04003D7F RID: 15743
        private bool m_isHammerCocked;

        // Token: 0x04003D80 RID: 15744
        private bool m_isCamSet = true;

        // Token: 0x04003D81 RID: 15745
        private int m_CamBurst;

        // Token: 0x04003D82 RID: 15746
        private float m_engagementDelay;

        // Token: 0x04003D83 RID: 15747
        public int SuperBurstAmount = 3;

        // Token: 0x04003D84 RID: 15748
        private int m_fireSelectorMode;

        // Token: 0x04003D85 RID: 15749
        [Header("Fire Selector Config")]
        public OpenBoltReceiver.InterpStyle FireSelector_InterpStyle = OpenBoltReceiver.InterpStyle.Rotation;

        // Token: 0x04003D86 RID: 15750
        public OpenBoltReceiver.Axis FireSelector_Axis;

        // Token: 0x04003D87 RID: 15751
        public OpenBoltReceiver.FireSelectorMode[] FireSelector_Modes;

        // Token: 0x04003D88 RID: 15752
        [Header("Secondary Fire Selector Config")]
        public OpenBoltReceiver.InterpStyle FireSelector_InterpStyle2 = OpenBoltReceiver.InterpStyle.Rotation;

        // Token: 0x04003D89 RID: 15753
        public OpenBoltReceiver.Axis FireSelector_Axis2;

        // Token: 0x04003D8A RID: 15754
        public OpenBoltReceiver.FireSelectorMode[] FireSelector_Modes2;

        // Token: 0x04003D8B RID: 15755
        private float m_timeSinceFiredShot = 1f;

        // Token: 0x04003D8C RID: 15756
        [Header("SpecialFeatures")]
        public bool UsesRecoilingSystem;

        // Token: 0x04003D8D RID: 15757
        public G11RecoilingSystem RecoilingSystem;

        // Token: 0x04003D8E RID: 15758
        public bool UsesMagMountTransformOverride;

        // Token: 0x04003D8F RID: 15759
        public Transform MagMountTransformOverride;

        // Token: 0x02000621 RID: 1569
        public enum InterpStyle
        {
            // Token: 0x04003D91 RID: 15761
            Translate,
            // Token: 0x04003D92 RID: 15762
            Rotation
        }

        // Token: 0x02000622 RID: 1570
        public enum Axis
        {
            // Token: 0x04003D94 RID: 15764
            X,
            // Token: 0x04003D95 RID: 15765
            Y,
            // Token: 0x04003D96 RID: 15766
            Z
        }

        // Token: 0x02000623 RID: 1571
        public enum FireSelectorModeType
        {
            // Token: 0x04003D98 RID: 15768
            Safe,
            // Token: 0x04003D99 RID: 15769
            Single,
            // Token: 0x04003D9A RID: 15770
            FullAuto,
            // Token: 0x04003D9B RID: 15771
            SuperFastBurst,
            // Token: 0x04003D9C RID: 15772
            Burst
        }

        // Token: 0x02000624 RID: 1572
        [Serializable]
        public class FireSelectorMode
        {
            // Token: 0x04003D9D RID: 15773
            public float SelectorPosition;

            // Token: 0x04003D9E RID: 15774
            public OpenBoltReceiver.FireSelectorModeType ModeType;

            // Token: 0x04003D9F RID: 15775
            public float EngagementDelay;

            // Token: 0x04003DA0 RID: 15776
            public int BurstAmount = 3;
        }
    }
}
