// CustomCitizenInfoPanel.cs

namespace RealTime.UI
{
    using RealTime.CustomAI;
    // using RealTime.Simulation;
    using SkyTools.Localization;

    /// <summary>
    /// A customized citizen info panel that additionally shows the origin building of the citizen.
    /// </summary>
    internal sealed class CustomCitizenInfoPanel : RealTimeInfoPanelBase<HumanWorldInfoPanel>
    {
        private const string GameInfoPanelName = "(Library) CitizenWorldInfoPanel";

        private CustomCitizenInfoPanel(string panelName, RealTimeResidentAI<ResidentAI, Citizen> residentAI, ILocalizationProvider localizationProvider)
            : base(panelName, residentAI, localizationProvider)
        {
        }

        /// <summary>Enables the citizen info panel customization. Can return null on failure.</summary>
        /// <param name="residentAI">The custom resident AI.</param>
        /// <param name="localizationProvider">The localization provider to use for text translation.</param>
        /// <param name="timeInfo">time info.</param>
        /// <returns>An instance of the <see cref="CustomCitizenInfoPanel"/> object that can be used for disabling
        /// the customization, or null when the customization fails.</returns>
        public static CustomCitizenInfoPanel Enable(RealTimeResidentAI<ResidentAI, Citizen> residentAI, ILocalizationProvider localizationProvider)
        {
            var result = new CustomCitizenInfoPanel(GameInfoPanelName, residentAI, localizationProvider);
            return result.Initialize() ? result : null;
        }

        /// <summary>Updates the origin building display.</summary>
        /// <param name="instance">The game object instance to get the information from.</param>
        /// <param name="debugMode">add debug info.</param>
        public override void UpdateCustomInfo(ref InstanceID instance, bool debugMode)
        {
            if (instance.Type != InstanceType.Citizen)
            {
                UpdateCitizenInfo(0, debugMode);
            }
            else
            {
                UpdateCitizenInfo(instance.Citizen, debugMode);
            }
        }
    }
}
