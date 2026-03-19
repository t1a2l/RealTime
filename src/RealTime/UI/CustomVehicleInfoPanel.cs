// CustomVehicleInfoPanel.cs

namespace RealTime.UI
{
    using System;
    using HarmonyLib;
    using RealTime.CustomAI;
    using RealTime.Simulation;
    using SkyTools.Localization;
    using UnityEngine;

    /// <summary>
    /// A customized vehicle info panel that additionally shows the origin building of the owner citizen.
    /// </summary>
    internal sealed class CustomVehicleInfoPanel : RealTimeInfoPanelBase<VehicleWorldInfoPanel>
    {
        private const string GameInfoPanelName = "(Library) CitizenVehicleWorldInfoPanel";
        private const string GetDriverInstanceMethodName = "GetDriverInstance";

        private GetDriverInstanceDelegate<PassengerCarAI> passengerCarAIGetDriverInstance;
        private GetDriverInstanceDelegate<BicycleAI> bicycleAIGetDriverInstance;

        private CustomVehicleInfoPanel(string panelName, RealTimeResidentAI<ResidentAI, Citizen> residentAI, ILocalizationProvider localizationProvider)
            : base(panelName, residentAI, localizationProvider)
        {
            try
            {
                passengerCarAIGetDriverInstance = AccessTools.MethodDelegate<GetDriverInstanceDelegate<PassengerCarAI>>(AccessTools.Method(typeof(PassengerCarAI), GetDriverInstanceMethodName));

                bicycleAIGetDriverInstance = AccessTools.MethodDelegate<GetDriverInstanceDelegate<BicycleAI>>(AccessTools.Method(typeof(BicycleAI), GetDriverInstanceMethodName));

            }
            catch (Exception ex)
            {
                Debug.LogError("The 'Real Time' mod failed to obtain at least one of the GetDriverInstance methods. Error message: " + ex);
            }
        }

        private delegate ushort GetDriverInstanceDelegate<T>(T instance, ushort vehicleId, ref Vehicle vehicle);

        /// <summary>Enables the vehicle info panel customization. Can return null on failure.</summary>
        /// <param name="residentAI">The custom resident AI.</param>
        /// <param name="localizationProvider">The localization provider to use for text translation.</param>
        /// <param name="timeInfo">time info.</param>
        /// <returns>An instance of the <see cref="CustomVehicleInfoPanel"/> object that can be used for disabling
        /// the customization, or null when the customization fails.</returns>
        ///
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="residentAI"/> is <c>null</c>.</exception>
        public static CustomVehicleInfoPanel Enable(RealTimeResidentAI<ResidentAI, Citizen> residentAI, ILocalizationProvider localizationProvider)
        {
            if (residentAI == null)
            {
                throw new ArgumentNullException(nameof(residentAI));
            }

            if (localizationProvider == null)
            {
                throw new ArgumentNullException(nameof(localizationProvider));
            }

            var result = new CustomVehicleInfoPanel(GameInfoPanelName, residentAI, localizationProvider);
            return result.Initialize() ? result : null;
        }

        /// <summary>Updates the origin building display.</summary>
        /// <param name="instance">The game object instance to get the information from.</param>
        /// <param name="debugMode">add debug info.</param>
        public override void UpdateCustomInfo(ref InstanceID instance, bool debugMode = false)
        {
            ushort instanceId = 0;
            try
            {
                if (passengerCarAIGetDriverInstance == null || bicycleAIGetDriverInstance == null)
                {
                    return;
                }

                if (instance.Type != InstanceType.Vehicle || instance.Vehicle == 0)
                {
                    return;
                }

                ushort vehicleId = instance.Vehicle;
                vehicleId = VehicleManager.instance.m_vehicles.m_buffer[vehicleId].GetFirstVehicle(vehicleId);
                if (vehicleId == 0)
                {
                    return;
                }

                var vehicleInfo = VehicleManager.instance.m_vehicles.m_buffer[vehicleId].Info;

                try
                {
                    switch (vehicleInfo.m_vehicleAI)
                    {
                        case BicycleAI bicycleAI:
                            instanceId = bicycleAIGetDriverInstance(bicycleAI, vehicleId, ref VehicleManager.instance.m_vehicles.m_buffer[vehicleId]);
                            break;

                        case PassengerCarAI passengerCarAI:
                            instanceId = passengerCarAIGetDriverInstance(passengerCarAI, vehicleId, ref VehicleManager.instance.m_vehicles.m_buffer[vehicleId]);
                            break;

                        default:
                            return;
                    }
                }
                catch
                {
                    passengerCarAIGetDriverInstance = null;
                    bicycleAIGetDriverInstance = null;
                }
            }
            finally
            {
                uint citizenId = instanceId == 0
                    ? 0u
                    : CitizenManager.instance.m_instances.m_buffer[instanceId].m_citizen;
                UpdateCitizenInfo(citizenId, debugMode);
            }
        }
    }
}
