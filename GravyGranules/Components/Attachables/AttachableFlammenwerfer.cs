using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FistVR;
using UnityEngine;

namespace GravyScripts.Components
{
    // I'm so sorry Cityrobo.
    public class AttachableFlammenwerfer : AttachableFirearm
    {
        public override void Awake()
        {
            if (UsesPilotLightSystem)
            {
                PilotLight.gameObject.SetActive(false);
            }
        }
        private float GetVLerp()
        {
            if (UsesValve)
            {
                return Valve.ValvePos;
            }
            if (UsesMF2Valve)
            {
                return MF2Valve.Lerp;
            }
            return 0.5f;
        }

        public override void Update()
        {
            base.Update();

            UpdateFire();
            if (UsesPilotLightSystem)
            {
                if (Magazine != null && Magazine.FuelAmountLeft > 0f)
                {
                    if (!m_isPilotLightOn)
                    {
                        PilotOn();
                    }
                }
                else if (m_isPilotLightOn)
                {
                    PilotOff();
                }
                if (m_isPilotLightOn)
                {
                    PilotLight.localScale = Vector3.one + UnityEngine.Random.onUnitSphere * 0.05f;
                }
            }
        }

        private void PilotOn()
        {
            m_isPilotLightOn = true;
            SM.PlayCoreSound(FVRPooledAudioType.GenericClose, AudEvent_PilotOn, GetMuzzle().position);
            PilotLight.gameObject.SetActive(true);
        }

        // Token: 0x0600466E RID: 18030 RVA: 0x00222539 File Offset: 0x00220939
        private void PilotOff()
        {
            m_isPilotLightOn = false;
            PilotLight.gameObject.SetActive(false);
        }

        // Token: 0x0600466F RID: 18031 RVA: 0x00222554 File Offset: 0x00220954
        private void AirBlast()
        {
            GameObject gameObject = Instantiate(AirBlastGo, AirBlastCenter.position, AirBlastCenter.rotation);
            gameObject.GetComponent<Explosion>().IFF = GM.CurrentPlayerBody.GetPlayerIFF();
            gameObject.GetComponent<ExplosionSound>().IFF = GM.CurrentPlayerBody.GetPlayerIFF();
        }

        // Token: 0x06004671 RID: 18033 RVA: 0x002227F4 File Offset: 0x00220BF4
        public void UpdateFire()
        {
            ParticleSystem.EmissionModule emission = FireParticles.emission;
            ParticleSystem.MinMaxCurve rateOverTime = emission.rateOverTime;
            if (m_isFiring)
            {
                rateOverTime.mode = ParticleSystemCurveMode.Constant;
                rateOverTime.constantMax = ParticleVolume;
                rateOverTime.constantMin = ParticleVolume;
                float vlerp = GetVLerp();
                ParticleSystem.MainModule main = FireParticles.main;
                ParticleSystem.MinMaxCurve startSpeed = main.startSpeed;
                startSpeed.mode = ParticleSystemCurveMode.TwoConstants;
                startSpeed.constantMax = Mathf.Lerp(SpeedRangeMax.x, SpeedRangeMax.y, vlerp);
                startSpeed.constantMin = Mathf.Lerp(SpeedRangeMin.x, SpeedRangeMin.y, vlerp);
                main.startSpeed = startSpeed;
                ParticleSystem.MinMaxCurve startSize = main.startSize;
                startSize.mode = ParticleSystemCurveMode.TwoConstants;
                startSize.constantMax = Mathf.Lerp(SizeRangeMax.x, SizeRangeMax.y, vlerp);
                startSize.constantMin = Mathf.Lerp(SizeRangeMin.x, SizeRangeMin.y, vlerp  );
                main.startSize = startSize;
                ParticleSystem.ShapeModule shape = FireParticles.shape;
                shape.angle = Mathf.Lerp(FireWidthRange.x, FireWidthRange.y, vlerp);
            }
            else
            {
                rateOverTime.mode = ParticleSystemCurveMode.Constant;
                rateOverTime.constantMax = 0f;
                rateOverTime.constantMin = 0f;
            }
            emission.rateOverTime = rateOverTime;
        }

        // Token: 0x06004672 RID: 18034 RVA: 0x00222972 File Offset: 0x00220D72
        private bool HasFuel()
        {
            return !(Magazine == null) && Magazine.FuelAmountLeft > 0f;
        }

        // Token: 0x06004673 RID: 18035 RVA: 0x002229A0 File Offset: 0x00220DA0
        private void StopFiring()
        {
            if (m_isFiring)
            {
                SM.PlayCoreSound(FVRPooledAudioType.GenericClose, AudEvent_Extinguish, GetMuzzle().position);
                AudSource_FireLoop.Stop();
                AudSource_FireLoop.volume = 0f;
            }
            m_isFiring = false;
            m_hasFiredStartSound = false;
        }

        public override void ProcessInput(FVRViveHand hand, bool fromInterface, FVRInteractiveObject o)
        {
            base.ProcessInput(hand, fromInterface, o);

            if (o.IsHeld)
            {
                if (o.m_hasTriggeredUpSinceBegin)
                {
                    m_triggerFloat = hand.Input.TriggerFloat;
                }
                else
                {
                    m_triggerFloat = 0f;
                }
                if (UsesAirBlast && m_airBurstRecovery <= 0f && HasFuel() && ((hand.IsInStreamlinedMode && hand.Input.BYButtonDown) || (!hand.IsInStreamlinedMode && hand.Input.TouchpadDown)))
                {
                    m_airBurstRecovery = 1f;
                    AirBlast();
                    Magazine.DrainFuel(5f);
                }
                if (m_airBurstRecovery > 0f)
                {
                    m_airBurstRecovery -= Time.deltaTime;
                }
                if (m_triggerFloat > 0.2f && HasFuel() && m_airBurstRecovery <= 0f)
                {
                    if (m_triggerHasBeenHeldFor < 2f)
                    {
                        m_triggerHasBeenHeldFor += Time.deltaTime;
                    }
                    m_isFiring = true;
                    if (!m_hasFiredStartSound)
                    {
                        m_hasFiredStartSound = true;
                        SM.PlayCoreSound(FVRPooledAudioType.GenericClose, AudEvent_Ignite, GetMuzzle().position);
                    }
                    float num = Mathf.Clamp(m_triggerHasBeenHeldFor * 2f, 0f, 0.4f);
                    AudSource_FireLoop.volume = num;
                    float vlerp = GetVLerp();
                    AudSource_FireLoop.pitch = Mathf.Lerp(AudioPitchRange.x, AudioPitchRange.y, vlerp);
                    if (!AudSource_FireLoop.isPlaying)
                    {
                        AudSource_FireLoop.Play();
                    }
                    Magazine.DrainFuel(Time.deltaTime);
                }
                else
                {
                    m_triggerHasBeenHeldFor = 0f;
                    StopFiring();
                }
            }
            else
            {
                m_triggerFloat = 0f;
            }
            if (m_triggerFloat <= 0f)
            {
                StopFiring();
            }
            if (Trigger != null)
            {
                if (TriggerInterpStyle == FVRPhysicalObject.InterpStyle.Translate)
                {
                    Trigger.localPosition = new Vector3(0f, 0f, Mathf.Lerp(Trigger_ForwardValue, Trigger_RearwardValue, m_triggerFloat));
                }
                else if (TriggerInterpStyle == FVRPhysicalObject.InterpStyle.Rotation)
                {
                    Trigger.localEulerAngles = new Vector3(Mathf.Lerp(Trigger_ForwardValue, Trigger_RearwardValue, m_triggerFloat), 0f, 0f);
                }
            }
        }
        [Header("FlameThrower Params")]
        public FlameThrowerValve Valve;
        public bool UsesValve = true;
        public MF2_FlamethrowerValve MF2Valve;
        public bool UsesMF2Valve;

        [Header("Trigger Config")]
        public Transform Trigger;
        public float TriggerFiringThreshold = 0.8f;
        public float Trigger_ForwardValue;
        public float Trigger_RearwardValue;
        public FVRPhysicalObject.InterpStyle TriggerInterpStyle = FVRPhysicalObject.InterpStyle.Rotation;
        private float m_triggerFloat;

        [Header("Special Audio Config")]
        public AudioEvent AudEvent_Ignite;
        public AudioEvent AudEvent_Extinguish;
        public AudioSource AudSource_FireLoop;
        private float m_triggerHasBeenHeldFor;
        private bool m_hasFiredStartSound;
        private bool m_isFiring;
        public ParticleSystem FireParticles;
        public Vector2 FireWidthRange;
        public Vector2 SpeedRangeMin;
        public Vector2 SpeedRangeMax;
        public Vector2 SizeRangeMin;
        public Vector2 SizeRangeMax;
        public Vector2 AudioPitchRange = new(1.5f, 0.5f);
        public float ParticleVolume = 40f;
        public bool UsesPilotLightSystem;
        public bool UsesAirBlastSystem;

        [Header("PilotLight")]
        public Transform PilotLight;
        private bool m_isPilotLightOn;
        public AudioEvent AudEvent_PilotOn;

        [Header("Airblast")]
        public bool UsesAirBlast;
        public Transform AirBlastCenter;
        public GameObject AirBlastGo;
        private float m_airBurstRecovery;
    }
}
