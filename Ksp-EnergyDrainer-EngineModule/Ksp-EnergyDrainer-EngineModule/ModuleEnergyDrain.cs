using System;
using UnityEngine;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Linq;

namespace Ksp_EnergyDrainer_EngineModule
{
    public class ModuleEnergyDrain : PartModule
    {
        [KSPField(isPersistant = true)]
        public ModuleEngines EngineModule;

        //Guiname and GuiActive are tests to see if they show up.
        [KSPField(guiName = "Target Ressource", guiActive = true, isPersistant = false)]
        public string ressourceDrained;
        [KSPField(guiName = "Base Consumption", guiActive = true, isPersistant = false)]
        public float consumption;
        [KSPField(guiName = "Current Throttle", guiActive = true, isPersistant = false)]
        public float currentThrottle;

        public override void OnStart(PartModule.StartState state)
        {
            base.OnStart(state);
            {
                if (state != StartState.Editor && state != StartState.None)
                {
                    this.enabled = true;
                    this.part.force_activate();
                    EngineModule = this.part.Modules.OfType<ModuleEngines>().Single();
                }
                else
                {
                    this.enabled = false;
                }
            }
        }



        public override void OnFixedUpdate()
        {
            currentThrottle = EngineModule.currentThrottle;
        }

    }
}