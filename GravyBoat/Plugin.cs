using BepInEx;
using BepInEx.Logging;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using OtherLoader;
using UnityEngine;
using FistVR;
using HarmonyLib;
using BepInEx.Configuration;
using GravyScripts.Patches;
using BepInEx.Bootstrap;

namespace GravyScripts
{
    [BepInPlugin("h3vr.gravyscripts.patches", "The Gravy Boat", "0.0.1")]
    [BepInProcess("h3vr.exe")]
    public class GravyScripts : BaseUnityPlugin
    {
        public static ConfigEntry<bool> enablePatches;
        public static ConfigEntry<bool> enableExperimentalFeatures;

        public void Awake()
        {
            Logger = base.Logger;

            LoadConfigFile();

            // Totally haven't stolen this from Cityrobo. Nope. Not at all.
            // StartCoroutine(WaitForOtherloader());

            // Harmony.CreateAndPatchAll(typeof(BreakActionForegripPatches));
            // Harmony.CreateAndPatchAll(typeof(PalmingFix));
            // Harmony.CreateAndPatchAll(typeof(RevolverPatch));
            // Harmony.CreateAndPatchAll(typeof(RevolverCylinderPatch));

                Harmony.CreateAndPatchAll(typeof(AmmoPatches));
                Harmony.CreateAndPatchAll(typeof(ChamberPatches));
                Harmony.CreateAndPatchAll(typeof(BoltActionPatches));

                    Harmony.CreateAndPatchAll(typeof(RevolverPatch));
                    Harmony.CreateAndPatchAll(typeof(RevolverCylinderPatch));
        }
        private void LoadConfigFile()
        {
            enablePatches = Config.Bind
            (
                "Options",
                "Enable patches",
                false,
                "The gravy boat."
            );
            enableExperimentalFeatures = Config.Bind
            (
                "Options",
                "Experimental features",
                false,
                "Enable work-in-progress features that are likely to be buggy or incomplete. Currently includes: Revolver hinge physics."
            );
        }

        IEnumerator WaitForOtherloader()
        {
            while (LoaderStatus.GetLoaderProgress() != 1)
            {
                yield return null;
            }

            PrefabReplacement();
        }

        private void PrefabReplacement()
        {
            Logger.LogInfo("Starting Prefab Replacement!");
            for (int i = 0; i < IM.OD.Count; i++)
            {
                if (IM.OD.ContainsKey("J." + IM.OD.ElementAt(i).Key))
                {
                    string Key = IM.OD.ElementAt(i).Key;
                    FVRObject Value = IM.OD.ElementAt(i).Value;

                    Logger.LogInfo($"Found Prefab {Key}");
                    IM.OD.Add("old_" + Key, Value);
                    IM.OD[Key] = IM.OD["J." + Key];

                    IM.OD[Key].SpawnedFromId = IM.OD["old_" + Key].SpawnedFromId;
                    IM.OD[Key].CompatibleMagazines = IM.OD["old_" + Key].CompatibleMagazines;
                    IM.OD[Key].CompatibleClips = IM.OD["old_" + Key].CompatibleClips;
                    IM.OD[Key].CompatibleSpeedLoaders = IM.OD["old_" + Key].CompatibleSpeedLoaders;
                    IM.OD[Key].CompatibleSingleRounds = IM.OD["old_" + Key].CompatibleSingleRounds;
                    if (IM.OD[Key].BespokeAttachments == new List<FVRObject>())
                    {
                        IM.OD[Key].BespokeAttachments = IM.OD["old_" + Key].BespokeAttachments;
                    }
                    IM.OD[Key].MinCapacityRelated = IM.OD["old_" + Key].MinCapacityRelated;
                    IM.OD[Key].MaxCapacityRelated = IM.OD["old_" + Key].MaxCapacityRelated;

                    IM.OD["old_" + Key].OSple = false;
                }
            }
        }

        // The line below allows access to your plugin's logger from anywhere in your code, including outside of this file.
        // Use it with 'YourPlugin.Logger.LogInfo(message)' (or any of the other Log* methods)
        internal new static ManualLogSource Logger { get; private set; }
    }
}
