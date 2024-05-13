using System;
using KSP.Localization;
using UnityEngine;

namespace Ksp_EnergyDrainer_EngineModule
{
    public class ModuleEnergyDrain : PartModule
    {
        //Guiname and GuiActive are tests to see if they show up.

        //CFG Variables
        [KSPField]
        public string resourceDrained;
        [KSPField]
        public float consumption;
        [KSPField]
        public bool useThrustCurve = false;
        //thrustCurve, current default is testing floatcurve provided by jadeofmaar.
        [KSPField]
        public FloatCurve thrustCurve;


        //Display Variables - Are ment to be visible in normal gameplay
        [KSPField(guiName = "Secondary Status", guiActive = true)]
        public string Status;

        //Other Variables

        //Hook to ModuleEnginesFX
        [KSPField]
        public ModuleEnginesFX EngineModule;
        //Resource Hash
        [KSPField]
        public int resHash = 0;
        //Resource Values
        [KSPField]
        public double resCurrent;
        [KSPField]
        public double resTotal;
        

        //Boolean to check if launched
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


        public override string GetModuleDisplayName() => "Engine Secondary Ressource Consumer";
        public override string GetInfo()
        {
            //this works
            //return "<b><color=#99ff00ff>Consumes :</color></b>\n- <b>"+ resourceDrained +"</b> : " + consumption + "/sec. Max\n"+ Localizer.Format("#autoLOC_245153", new string[1] { XKCDColors.HexFormat.KSPUnnamedCyan });
            return "<b><color=#99ff00ff>Consumes :</color></b>\n"+ Localizer.Format("#autoLOC_220756", new string[2] {resourceDrained,consumption.ToString()}) + Localizer.Format("#autoLOC_245153", new string[1] { XKCDColors.HexFormat.KSPUnnamedCyan });
        }

        public override void OnStart(PartModule.StartState state)
        {
            base.OnStart(state);
            {
                // possible improvement
                // could probably destroy the module in the editor, that way i could remove the isLaunched variable and the check for it every FixedUpdate.
                if (state != StartState.Editor && state != StartState.None)
                {
                    //things here won't get run either way, just keeping around to know it doesn't work.
                    Debug.Log("[ModuleEnergyDrain]:" + "ONSTART TRIGGER");
                    isLaunched = true;
                    InitialiseModule();
                }
                else
                {

                }
            }
        }

        public void InitialiseModule()
        {
            //A HORRIBLE MESS, but it works
            try
            {
                //Get the hook to the ModuleEnginesFX, and the hash to the ressource drained. This worked earlier : this.part.FindModuleImplementing<ModuleEnginesFX>();
                EngineModule = this.part.FindModuleImplementing<ModuleEnginesFX>();
                resHash = PartResourceLibrary.Instance.GetDefinition(resourceDrained).id;
                Debug.Log("[ModuleEnergyDrain]:" + "resHash");
                Debug.Log("[ModuleEnergyDrain]:" + resHash);
                Debug.Log("[ModuleEnergyDrain]:" + "EngineModule");
                Debug.Log("[ModuleEnergyDrain]:" + EngineModule.currentThrottle);
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
                Debug.Log("[ModuleEnergyDrain]: Base Hook Log (no-error)");
            }
            else
            {
                hookError = true;
                Debug.Log("[ModuleEnergyDrain]: Base Hook Log (ERROR)");
            }
            if (resHash != 0)
            {
                resourceError = false;
                Debug.Log("[ModuleEnergyDrain]: Base Resource Log (no-error)");
            }
            else
            {
                resourceError = true;
                Debug.Log("[ModuleEnergyDrain]: Base Resource Log (ERROR)");
            }
            Status = "Initialised, waiting for FixedUpdate";
        }

        public void FixedUpdate()
        {
            //to prevent it running in editor.
            if (isLaunched)
            {
                if (hookError == true || resourceError == true)
                {
                    InitialiseModule();
                }

                this.vessel.GetConnectedResourceTotals(resHash, out resCurrent, out resTotal);
                //EngineModule.isEnabled doesn't seem to work, as it might always be enabled ?
                if (EngineModule.EngineIgnited)
                {
                    //get the consumption by using the base consumption, the amount of time since last update, and the current throttle.
                    double consumptionDelta = consumption * TimeWarp.fixedDeltaTime * EngineModule.currentThrottle;
                    if (useThrustCurve)
                    {
                        consumptionDelta *= thrustCurve.Evaluate((float)(resCurrent / resTotal));
                    }
                    //drain the ressource
                    double resReturn = this.vessel.RequestResource(this.vessel.Parts[0], resHash, consumptionDelta, false);
                    //check if it managed to drain what was needed. 0 might not be valid value
                    if (Math.Abs(consumptionDelta - resReturn) <= 0.001 && resCurrent != 0)
                    {
                        //Optionally do some other stuff while engine is running.
                        Status = "Running";
                    }
                    else
                    {
                        //shuts engine down
                        EngineModule.Shutdown();
                    }
                }
                else
                {
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
                            Status = "Out of " + resourceDrained;
                            EngineModule.Flameout("Out of " + resourceDrained);
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