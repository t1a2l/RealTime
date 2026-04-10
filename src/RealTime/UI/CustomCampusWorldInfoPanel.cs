// CustomCampusWorldInfoPanel.cs

namespace RealTime.UI
{
    using System;
    using ColossalFramework.UI;
    using RealTime.Config;
    using RealTime.Localization;
    using RealTime.Managers;
    using SkyTools.Localization;
    using SkyTools.UI;
    using UnityEngine;

    /// <summary>
    /// A customized campus info panel that shows the correct length of the academic year.
    /// </summary>
    internal sealed class CustomCampusWorldInfoPanel : CustomInfoPanelBase<CampusWorldInfoPanel>
    {
        private const string GameInfoPanelName = "(Library) CampusWorldInfoPanel";
        private const string ProgressTooltipLabelName = "ProgressTooltipLabel";
        private const string ComponentId = "RealTimeAcademicYearProgress";

        private readonly ILocalizationProvider localizationProvider;
        private readonly RealTimeConfig realTimeConfig;

        private UILabel progressTooltipLabel;
        private UILabel originalProgressTooltipLabel;

        private CustomCampusWorldInfoPanel(string infoPanelName, ILocalizationProvider localizationProvider, RealTimeConfig realTimeConfig)
            : base(infoPanelName)
        {
            this.localizationProvider = localizationProvider;
            this.realTimeConfig = realTimeConfig;
        }

        /// <summary>Enables the campus info panel customization. Can return null on failure.</summary>
        ///
        /// <param name="localizationProvider">The localization provider to use for text translation.</param>
        ///
        /// <returns>An instance of the <see cref="CustomCampusWorldInfoPanel"/> object that can be used for disabling
        /// the customization, or null when the customization fails.</returns>
        ///
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="localizationProvider"/> is <c>null</c>.</exception>
        public static CustomCampusWorldInfoPanel Enable(ILocalizationProvider localizationProvider, RealTimeConfig realTimeConfig)
        {
            if (localizationProvider == null)
            {
                throw new ArgumentNullException(nameof(localizationProvider));
            }

            var result = new CustomCampusWorldInfoPanel(GameInfoPanelName, localizationProvider, realTimeConfig);
            return result.Initialize() ? result : null;
        }

        /// <summary>Updates the custom information in this panel.</summary>
        /// <param name="instance">The game object instance to get the information from.</param>
        /// <param name="debugMode">add debug info.</param>
        public override void UpdateCustomInfo(ref InstanceID instance, bool debugMode = false)
        {
            ushort mainGate = DistrictManager.instance.m_parks.m_buffer[instance.Park].m_mainGate;
            ushort eventIndex = BuildingManager.instance.m_buildings.m_buffer[mainGate].m_eventIndex;

            var academicYearData = AcademicYearManager.GetAcademicYearData(mainGate);

            if (eventIndex == 0)
            {
                if (academicYearData.IsFirstAcademicYear)
                {
                    progressTooltipLabel.text = localizationProvider.Translate(TranslationKeys.AcademicYearStartDelay);
                }
                return;
            }

            ref var eventData = ref EventManager.instance.m_events.m_buffer[eventIndex];

            if (eventData.Info.m_eventAI is not AcademicYearAI)
            {
                return;
            }

            if (academicYearData.DidLastYearEnd)
            {
                float hours_since_last_year_ended = AcademicYearManager.CalculateHoursSinceLastYearEnded(mainGate);
                if (hours_since_last_year_ended >= 23f)
                {
                    progressTooltipLabel.text = localizationProvider.Translate(TranslationKeys.AcademicYearStartsSoon);
                }
                else
                {
                    string template = localizationProvider.Translate(TranslationKeys.AcademicYearHoursUntil);
                    progressTooltipLabel.text = string.Format(template, Mathf.RoundToInt(24 - hours_since_last_year_ended));
                }
                return;
            }

            float duration = realTimeConfig.AcademicYearLength * 24f;

            long endFrame = eventData.m_startFrame + (int)(duration * SimulationManager.DAYTIME_HOUR_TO_FRAME);
            long framesLeft = endFrame - SimulationManager.instance.m_currentFrameIndex;
            if (framesLeft < 0)
            {
                progressTooltipLabel.text = localizationProvider.Translate(TranslationKeys.AcademicYearEndDelay);
                return;
            }
            float hoursLeft = framesLeft * SimulationManager.DAYTIME_FRAME_TO_HOUR;
            if (hoursLeft < 1f)
            {
                progressTooltipLabel.text = localizationProvider.Translate(TranslationKeys.AcademicYearEndsSoon);
            }
            else if (hoursLeft < 24f)
            {
                string template = localizationProvider.Translate(TranslationKeys.AcademicYearHoursLeft);
                progressTooltipLabel.text = string.Format(template, Mathf.RoundToInt(hoursLeft));
            }
            else
            {
                float daysLeft = hoursLeft / 24f;
                string template = localizationProvider.Translate(TranslationKeys.AcademicYearDaysLeft);
                progressTooltipLabel.text = string.Format(template, (int)Math.Ceiling(daysLeft));
            }
        }

        /// <summary>Destroys the custom UI objects for the info panel.</summary>
        protected override void DisableCore()
        {
            progressTooltipLabel.parent.RemoveUIComponent(progressTooltipLabel);
            UnityEngine.Object.Destroy(progressTooltipLabel.gameObject);
            originalProgressTooltipLabel.isVisible = true;
            progressTooltipLabel = null;
            originalProgressTooltipLabel = null;
        }

        /// <summary>Builds up the custom UI objects for the info panel.</summary>
        /// <returns><c>true</c> on success; otherwise, <c>false</c>.</returns>
        protected override bool InitializeCore()
        {
            originalProgressTooltipLabel = ItemsPanel.parent.Find<UILabel>(ProgressTooltipLabelName);
            if (originalProgressTooltipLabel == null)
            {
                return false;
            }

            progressTooltipLabel = UIComponentTools.CreateCopy(originalProgressTooltipLabel, originalProgressTooltipLabel.parent, ComponentId);
            originalProgressTooltipLabel.isVisible = false;
            return true;
        }
    }
}
