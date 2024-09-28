using System;
using System.CodeDom;
using System.Collections.Generic;
using System.ComponentModel;
using KSP.Localization;
using KSP.UI.Screens;
using KSP.UI.TooltipTypes;
using UnityEngine;

namespace NoDeltaVPropEngineModule
{
    public class NoDeltaVPropEngineModule : PartModule
    {
        //CFG Variables
        [KSPField]
        public string resourceDrained;
        [KSPField]
        public float consumption;
        [KSPField]
        public bool useConsumptionCurve = false;
        [KSPField]
        public bool useThrustCurve = false;
        [KSPField]
        public bool useThrottleCurve = false;
        [KSPField]
        public FloatCurve consumptionCurve = new FloatCurve(new Keyframe[2]{new Keyframe(0f, 1f), new Keyframe(1f, 1f)}); //defaults to a curve that acts like stock.
        [KSPField]
        public FloatCurve thrustCurve = new FloatCurve(new Keyframe[2] { new Keyframe(0f, 1f), new Keyframe(1f, 1f) }); //defaults to a curve that acts like stock.
        [KSPField]
        public FloatCurve throttleCurve = new FloatCurve(new Keyframe[2] { new Keyframe(1f, 1f), new Keyframe(1f, 1f) }); //defaults to a curve that acts like stock.
        [KSPField]
        public Color progressBarBgColor = new Color(0.517f, 0.718f, 0.004f, 0.6f); //Default is DarkLime
        [KSPField]
        public Color progressBarColor = new Color(1f, 1f, 0.078f, 0.6f); //default is Yellow



        //Display module status. (currently disabled)
        [KSPField(guiName = "Secondary Status", guiActive = false)]
        public string Status;

        //Hook to ModuleEnginesFX
        [KSPField]
        public ModuleEngines EngineModule;
        //Resource Hash
        [KSPField]
        public int resHash = 0;
        //Resource Values
        [KSPField]
        public double resCurrent;
        [KSPField]
        public double resTotal;
        //Green Ressource Gauge for staging tab.
        [KSPField]
        public ProtoStageIconInfo resGauge;
        [KSPField]
        public ResourceFlowMode resourceDrainedFlowMode;
        [KSPField]
        public float thrustCurveRatio = 1f;

        //Boolean to check if launched (might be useless)
        [KSPField]
        public bool isLaunched = false;
        //Boolean to check if flameout was caused by ModuleEnergyDrain
        [KSPField]
        public bool flameoutDrainResource = false;
        //Boolean flag to say if something is wrong.
        [KSPField(guiName = "Hook Error", guiActive = false)]
        public bool hookError = true;
        [KSPField(guiName = "Resource Error", guiActive = false)]
        public bool resourceError = true;


        //Localisation memos. use the following to get the displayname of the ressource
        //PartResourceLibrary.Instance.GetDefinition(resourceDrained).displayName


        public override string GetModuleDisplayName() => "Engine Secondary Ressource Consumer";
        public override string GetInfo()
        {
            //this works
            return "Provided by <b>NoDeltaVPropEngineModule</b>\n\n<b><color=#99ff00ff>Consumes :</color></b>\n" + Localizer.Format("#autoLOC_220756", new string[2] { PartResourceLibrary.Instance.GetDefinition(resourceDrained).displayName, consumption.ToString()}) + GetFlowModeDescription();
        }

        public string GetFlowModeDescription()
        {
            resourceDrainedFlowMode = PartResourceLibrary.Instance.GetDefinition(resourceDrained).resourceFlowMode;
            string flowModeDescription = "";
            switch (resourceDrainedFlowMode)
            {
                case ResourceFlowMode.NO_FLOW:
                    flowModeDescription += Localizer.Format("#autoLOC_245149");
                    break;
                case ResourceFlowMode.ALL_VESSEL:
                case ResourceFlowMode.ALL_VESSEL_BALANCE:
                    flowModeDescription += Localizer.Format("#autoLOC_245153", new string[1]
                    {
          XKCDColors.HexFormat.KSPUnnamedCyan
                    });
                    break;
                case ResourceFlowMode.STAGE_PRIORITY_FLOW:
                case ResourceFlowMode.STAGE_PRIORITY_FLOW_BALANCE:
                    flowModeDescription += Localizer.Format("#autoLOC_245157", new string[1]
                    {
          XKCDColors.HexFormat.KSPBadassGreen
                    });
                    break;
                case ResourceFlowMode.STACK_PRIORITY_SEARCH:
                case ResourceFlowMode.STAGE_STACK_FLOW:
                case ResourceFlowMode.STAGE_STACK_FLOW_BALANCE:
                    flowModeDescription += Localizer.Format("#autoLOC_245162", new string[1]
                    {
          XKCDColors.HexFormat.YellowishOrange
                    });
                    break;
            }
            return flowModeDescription;
        }

    public override void OnStart(PartModule.StartState state)
        {
            base.OnStart(state);
            {
                Debug.Log("[ModuleEnergyDrain]:" + "OnStart");
                if (state != StartState.Editor && state != StartState.None)
                {
                    //things here won't get run either way, just keeping around to know it doesn't work.
                    Debug.Log("[ModuleEnergyDrain]:" + "Module Start");
                    isLaunched = true;
                    InitialiseModule();
                }
                else
                {
                    Debug.Log("[ModuleEnergyDrain]:" + "InEditor");
                    //unsure if necessary. can be removed without issues.
                    //Destroy(this);
                }
            }
        }

        public void InitialiseModule()
        {
            //A HORRIBLE MESS, but it works
            try
            {
                //Get the hook to the ModuleEngines, and the hash to the ressource drained. This worked earlier : this.part.FindModuleImplementing<ModuleEnginesFX>();
                EngineModule = this.part.FindModuleImplementing<ModuleEngines>();
                //EngineModule = this.part.FindModuleImplementing<ModuleEnginesFX>();
                resHash = PartResourceLibrary.Instance.GetDefinition(resourceDrained).id;
            }
            catch (Exception e) 
            {
                Debug.Log("[ModuleEnergyDrain]:" + e.ToString());
            }

            //Had problems with catch, so i run tests here.
            if (EngineModule != null)
            {
                //Base Errors are here to see if things run. It should be updated as soon as the next update. if it doesn't, there's a problem somewhere.
                hookError = false;
                Debug.Log("[ModuleEnergyDrain]: Engine Hook Log (no-error)");
            }
            else
            {
                hookError = true;
                Debug.Log("[ModuleEnergyDrain]: Engine Hook Log (ERROR)");
            }
            if (resHash != 0)
            {
                resourceError = false;
                Debug.Log("[ModuleEnergyDrain]: Resource Hash Log (no-error)");
            }
            else
            {
                resourceError = true;
                Debug.Log("[ModuleEnergyDrain]: Resource Hash Log (ERROR)");
            }
            Status = "Initialised, waiting for FixedUpdate";
        }
        
        //Engine ressource Gauge stuff
        public void ResetPropellantGauge()
        {
            if (resGauge != null)
            {
                this.part.stackIcon.RemoveInfo(resGauge);
                resGauge = null;
            }
        }
        public void UpdatePropellantGauge()
        {
            if (resGauge == null)
            {
                resGauge = this.part.stackIcon.DisplayInfo();
                if (resGauge == null)
                    return;
                resGauge.SetLength(2f);
                resGauge.SetMsgBgColor(new Color(0.517f, 0.718f, 0.004f, 0.6f)); //DarkLime
                resGauge.SetMsgTextColor(new Color(0.658f, 1f, 0.016f, 0.6f)); //ElectricLime
                resGauge.SetMessage(PartResourceLibrary.Instance.GetDefinition(resourceDrained).displayName);
                resGauge.SetProgressBarBgColor(progressBarBgColor);
                resGauge.SetProgressBarColor(progressBarColor);
            }
            if (resGauge == null)
                return;
            resGauge.SetValue((float)resCurrent, 0f, (float)resTotal);
        }

        public void FixedUpdate()
        {
            Debug.Log("[ModuleEnergyDrain]:" + "FixedUpdate");
            //to prevent it running in editor. (might no longer be needed)
            if (isLaunched)
            {
                if (hookError == true || resourceError == true)
                {
                    Debug.Log("[ModuleEnergyDrain]:" + "FixedUpdateError");
                    InitialiseModule();
                }


                this.vessel.GetConnectedResourceTotals(resHash, out resCurrent, out resTotal);
                if (EngineModule.EngineIgnited)
                {
                    //get the consumption by using the base consumption, the amount of time since last update, and the current throttle.
                    double consumptionDelta = consumption * TimeWarp.fixedDeltaTime * EngineModule.currentThrottle;
                    //if using consumptionCurve, then multiply the consumptionDelta by the consumptionCurve ratio
                    if (useConsumptionCurve)
                    {
                        consumptionDelta *= consumptionCurve.Evaluate((float)(resCurrent / resTotal));
                    }
                    if (useThrustCurve)
                    {
                        thrustCurveRatio = thrustCurve.Evaluate((float)(resCurrent / resTotal));
                        //might not even be necessary, EngineModule.currentThrottle might have the multiplier applied. don't think it's the case, but will have to test.
                        consumptionDelta *= thrustCurveRatio;
                    }
                    if (useThrottleCurve)
                    {
                        consumptionDelta *= throttleCurve.Evaluate((float)EngineModule.currentThrottle); ;
                    }
                    //Not sure if necessary, but think it might cause a crash if you run RequestResource when there's no storage of that resource onboard.
                    double resReturn = 0f;
                    if (resCurrent != 0)
                    {
                        //drain the ressource
                        resReturn = this.part.RequestResource(resHash, consumptionDelta, resourceDrainedFlowMode);
                    }
                    //check if it managed to drain what was needed. 0 might not be valid value
                    if (Math.Abs(consumptionDelta - resReturn) <= 0.001 && resCurrent != 0) //the resCurrent check is probably not needed now that i check it above, but it's not broke, so don't fix it.
                    {
                        Status = "Running";
                        UpdatePropellantGauge();
                        //Optionally do some other stuff while engine is running.
                    }
                    else
                    {
                        //shuts engine down
                        EngineModule.Shutdown();
                    }
                }
                else
                {
                    Debug.Log("[ModuleEnergyDrain]:" + "beforeReset");
                    ResetPropellantGauge();
                    Debug.Log("[ModuleEnergyDrain]:" + "afterReset");
                    //check if there was a error loading
                    if (!hookError && !resourceError)
                    {
                        //check if there's any amount of the ressource to be consumed
                        if (!(resCurrent == 0))
                        {
                            Status = "Idle";
                            if (flameoutDrainResource)
                            {
                                EngineModule.UnFlameout();
                                flameoutDrainResource = false;
                            }
                        }
                        else
                        {

                            Status = Localizer.Format("#autoLOC_219085", new string[1] { PartResourceLibrary.Instance.GetDefinition(resourceDrained).displayName });
                            EngineModule.Flameout(Localizer.Format("#autoLOC_219085", new string[1] { PartResourceLibrary.Instance.GetDefinition(resourceDrained).displayName }));
                            flameoutDrainResource = true;
                        }
                    }
                    //this is all only used for the Secondary Status, could be disabled if not using.
                    else
                    {
                        if (hookError == true && resourceError == true)
                        {
                            Status = "Critical Error, contact mod maker";
                        }
                        else if (hookError == true)
                        {
                            Status = "Error, Module Can't hook to Engine";
                        }
                        else //assumes it's ressource error, wouldn't get here otherwise
                        {
                            Status = "Error, Resource in part CFG unknown";
                        }
                    }

                }
            }
        }

    }
}