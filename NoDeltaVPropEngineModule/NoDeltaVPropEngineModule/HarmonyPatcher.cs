using HarmonyLib;
using System;
using UnityEngine;
using NoDeltaVPropEngineModule;
using System.Runtime.CompilerServices;

namespace NoDeltaVPropEngineModule
{
    [KSPAddon(KSPAddon.Startup.Instantly, true)]
    public class HarmonyPatcher : MonoBehaviour
    {
        void Start()
        {
            var harmony = new Harmony("com.Lucaspec72.NoDeltaVPropEngineModule.PropMassHook");
            harmony.PatchAll();
        }
        [HarmonyPatch(typeof(ModuleEngines), nameof(ModuleEngines.RequiredPropellantMass))]
        public class RequiredPropellantMass_Patch
        {
            static void Postfix(ModuleEngines __instance, ref double __result)
            {
                if (HighLogic.LoadedSceneIsFlight)
                {
                    NoDeltaVPropEngineModule NoDVPropModule = __instance.part.FindModuleImplementing<NoDeltaVPropEngineModule>();
                    if (NoDVPropModule)
                    {
                        if (NoDVPropModule.useThrustCurve)
                        {
                            __instance.resultingThrust *= NoDVPropModule.thrustCurveRatio;
                            __result *= NoDVPropModule.thrustCurveRatio;
                        }
                    }
                }
            }
        }
    }
}
