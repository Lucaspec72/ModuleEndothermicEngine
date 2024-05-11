using UnityEngine;

namespace Ksp_EnergyDrainer_EngineModule
{
    [KSPAddon(KSPAddon.Startup.Instantly, true)]
    class StartModule : MonoBehaviour
    {
        public void Start()
        {
            //UNUSED REMNANTS OF EARLY TEST VERSIONS
            //Example for Adding Ressources to Ignore List
            //DeltaVGlobals.PropellantsToIgnore.Add(PartResourceLibrary.Instance.GetDefinition("LiquidFuel").id);
            //DeltaVGlobals.PropellantsToIgnore.Add("LiquidFuel".GetHashCode());
        }
    }
}
