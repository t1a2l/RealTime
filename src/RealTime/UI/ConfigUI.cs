// ConfigUI.cs

namespace RealTime.UI
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Reflection;
    using RealTime.Config;
    using RealTime.Core;
    using RealTime.Managers;
    using RealTime.Patches;
    using SkyTools.Configuration;
    using SkyTools.Localization;
    using SkyTools.UI;
    using UnityEngine;

    /// <summary>Manages the mod's configuration page.</summary>
    internal sealed class ConfigUI
    {
        private const string ResetToDefaultsId = "ResetToDefaults";
        private const string UseForNewGamesId = "UseForNewGames";
        private const string ClearStuckCitizensScheduleId = "ClearStuckCitizensSchedule";
        private const string ClearStuckTouristsInHotelsId = "ClearStuckTouristsInHotels";
        private const string ClearStuckCitizensInClosedBuildingsId = "ClearStuckCitizensInClosedBuildings";
        private const string ClearFireBurnTimeManagerId = "ClearFireBurnTimeManager";
        private const string ClearBuildingsWorkTimePrefabsId = "ClearBuildingsWorkTimePrefabs";
        private const string ClearBuildingWorkTimeGlobalSettingsId = "ClearBuildingWorkTimeGlobalSettings";
        private const string ResetBuildingsGarbageBufferId = "ResetBuildingsGarbageBuffer";
        private const string ResetBuildingsMailBufferId = "ResetBuildingsMailBuffer";
        private const string ExecuteSelectedActionId = "ExecuteSelectedAction";
        private const string ClearActionsGroupTitleId = "ClearActionsGroupTitle";
        private const string ToolsId = "Tools";

        private string ConfirmPanelClearFireBurnTimeManagerTitleId;
        private string ConfirmPanelClearFireBurnTimeManagerTextId;
        private string ConfirmPanelClearBuildingsWorkTimePrefabsTitleId;
        private string ConfirmPanelClearBuildingsWorkTimePrefabsTextId;
        private string ConfirmPanelClearBuildingsWorkTimeGlobalSettingsTitleId;
        private string ConfirmPanelClearBuildingsWorkTimeGlobalSettingsTextId;

        private readonly ConfigurationProvider<RealTimeConfig> configProvider;
        private readonly IEnumerable<IViewItem> viewItems;

        private readonly RadioButtonsConfig radioConfig = new();
        private readonly List<IViewItem> radioCheckboxes = [];

        private static readonly Dictionary<RadioButtonsConfig.ModeType, string> ModeToIdMap = new() {
            { RadioButtonsConfig.ModeType.ClearStuckCitizensSchedule, ClearStuckCitizensScheduleId },
            { RadioButtonsConfig.ModeType.ClearStuckTouristsInHotels, ClearStuckTouristsInHotelsId },
            { RadioButtonsConfig.ModeType.ClearStuckCitizensInClosedBuildings, ClearStuckCitizensInClosedBuildingsId },
            { RadioButtonsConfig.ModeType.ClearFireBurnTimeManager, ClearFireBurnTimeManagerId },
            { RadioButtonsConfig.ModeType.ClearBuildingsWorkTimePrefabs, ClearBuildingsWorkTimePrefabsId },
            { RadioButtonsConfig.ModeType.ClearBuildingWorkTimeGlobalSettings, ClearBuildingWorkTimeGlobalSettingsId },
            { RadioButtonsConfig.ModeType.ResetBuildingsGarbageBuffer, ResetBuildingsGarbageBufferId },
            { RadioButtonsConfig.ModeType.ResetBuildingsMailBuffer, ResetBuildingsMailBufferId }
        };


        private ConfigUI(ConfigurationProvider<RealTimeConfig> configProvider, IEnumerable<IViewItem> viewItems)
        {
            this.configProvider = configProvider;
            this.viewItems = viewItems;
            this.configProvider.Changed += ConfigProviderChanged;
        }

        /// <summary>
        /// Creates the mod's configuration page using the specified object as data source.
        /// </summary>
        /// <param name="configProvider">The mod's configuration provider.</param>
        /// <param name="itemFactory">The view item factory to use for creating the UI elements.</param>
        /// <returns>A configured instance of the <see cref="ConfigUI"/> class.</returns>
        /// <exception cref="ArgumentNullException">Thrown when any argument is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the specified <see cref="ConfigurationProvider{RealTimeConfig}"/>
        /// is not initialized yet.</exception>
        public static ConfigUI Create(ConfigurationProvider<RealTimeConfig> configProvider, IViewItemFactory itemFactory)
        {
            if (configProvider == null)
            {
                throw new ArgumentNullException(nameof(configProvider));
            }

            if (itemFactory == null)
            {
                throw new ArgumentNullException(nameof(itemFactory));
            }

            if (configProvider.Configuration == null)
            {
                throw new InvalidOperationException("The configuration provider has no configuration yet");
            }

            var viewItems = new List<IViewItem>();
            CreateViewItems(configProvider, itemFactory, viewItems);

            var result = new ConfigUI(configProvider, viewItems);

            var toolsTab = viewItems.OfType<IContainerViewItem>().FirstOrDefault(i => i.Id == ToolsId);
            if (toolsTab == null)
            {
                toolsTab = itemFactory.CreateTabItem(ToolsId);
                viewItems.Add(toolsTab);
            }

            var resetButton = itemFactory.CreateButton(toolsTab, ResetToDefaultsId, result.ResetToDefaults);
            viewItems.Add(resetButton);
            var newGameConfigButton = itemFactory.CreateButton(toolsTab, UseForNewGamesId, result.UseForNewGames);
            viewItems.Add(newGameConfigButton);

            var radioGroup = itemFactory.CreateGroup(toolsTab, ClearActionsGroupTitleId);

            var props = typeof(RadioButtonsConfig).GetProperties().Where(p => p.Name.StartsWith("Is") && p.PropertyType == typeof(bool)).ToArray();

            var tempRadios = new List<IViewItem>();

            foreach (RadioButtonsConfig.ModeType mode in Enum.GetValues(typeof(RadioButtonsConfig.ModeType)))
            {
                string propName = $"Is{mode}Mode";  // "IsClearStuckCitizensScheduleMode"
                var prop = typeof(RadioButtonsConfig).GetProperty(propName);
                if (prop == null)
                {
                    continue;  // Safety
                }

                string id = ModeToIdMap[mode];  // Your const ID

                var radioCB = itemFactory.CreateCheckBox(radioGroup, id, prop, () => result.radioConfig);
                tempRadios.Add(radioCB);
                viewItems.Add(radioCB);
            }

            result.radioCheckboxes.Clear();
            result.radioCheckboxes.AddRange(tempRadios);
            result.radioConfig.PropertyChanged += result.RefreshRadioCheckboxes;

            viewItems.Add(radioGroup);

            var executeButton = itemFactory.CreateButton(radioGroup ?? toolsTab, ExecuteSelectedActionId,  result.ExecuteSelectedAction);
            viewItems.Add(executeButton);

            return result;
        }

        /// <summary>Closes this instance.</summary>
        public void Close() => configProvider.Changed -= ConfigProviderChanged;

        /// <summary>Translates the UI using the specified localization provider.</summary>
        /// <param name="localizationProvider">The localization provider to use for translation.</param>
        public void Translate(ILocalizationProvider localizationProvider)
        {
            foreach (var item in viewItems)
            {
                item.Translate(localizationProvider);
            }
        }

        public void UpdateModalTranslations(ILocalizationProvider localizationProvider)
        {
            ConfirmPanelClearFireBurnTimeManagerTitleId = localizationProvider.Translate("ConfirmPanelClearFireBurnTimeManagerTitle");
            ConfirmPanelClearFireBurnTimeManagerTextId = localizationProvider.Translate("ConfirmPanelClearFireBurnTimeManagerText");
            ConfirmPanelClearBuildingsWorkTimePrefabsTitleId = localizationProvider.Translate("ConfirmPanelClearBuildingsWorkTimePrefabsTitle");
            ConfirmPanelClearBuildingsWorkTimePrefabsTextId = localizationProvider.Translate("ConfirmPanelClearBuildingsWorkTimePrefabsText");
            ConfirmPanelClearBuildingsWorkTimeGlobalSettingsTitleId = localizationProvider.Translate("ConfirmPanelClearBuildingsWorkTimeGlobalSettingsTitle");
            ConfirmPanelClearBuildingsWorkTimeGlobalSettingsTextId = localizationProvider.Translate("ConfirmPanelClearBuildingsWorkTimeGlobalSettingsText");
        }

        private static void CreateViewItems(ConfigurationProvider<RealTimeConfig> configProvider, IViewItemFactory itemFactory, ICollection<IViewItem> viewItems)
        {
            var properties = configProvider.Configuration.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Select(p => new { Property = p, Attribute = GetCustomItemAttribute<ConfigItemAttribute>(p) })
                .Where(v => v.Attribute != null);

            foreach (var tab in properties.GroupBy(p => p.Attribute.TabId).OrderBy(p => p.Key))
            {
                var tabItem = itemFactory.CreateTabItem(tab.Key);

                viewItems.Add(tabItem);

                foreach (var group in tab.GroupBy(p => p.Attribute.GroupId).OrderBy(p => p.Key))
                {
                    IContainerViewItem containerItem;
                    if (string.IsNullOrEmpty(group.Key))
                    {
                        containerItem = tabItem;
                    }
                    else
                    {
                        containerItem = itemFactory.CreateGroup(tabItem, group.Key);
                        viewItems.Add(containerItem);
                    }

                    foreach (var item in group.OrderBy(i => i.Attribute.Order))
                    {
                        var viewItem = CreateViewItem(containerItem, item.Property, configProvider, itemFactory);
                        if (viewItem != null)
                        {
                            viewItems.Add(viewItem);
                        }
                    }
                }
            }
        }

        private static IViewItem CreateViewItem(
            IContainerViewItem container,
            PropertyInfo property,
            ConfigurationProvider<RealTimeConfig> configProvider,
            IViewItemFactory itemFactory)
        {
            object Config() => configProvider.Configuration;

            switch (GetCustomItemAttribute<ConfigItemUIBaseAttribute>(property))
            {
                case ConfigItemSliderAttribute slider when property.PropertyType.IsPrimitive:
                    if (property.PropertyType == typeof(bool) || property.PropertyType == typeof(IntPtr)
                        || property.PropertyType == typeof(UIntPtr) || property.PropertyType == typeof(char))
                    {
                        goto default;
                    }

                    return itemFactory.CreateSlider(
                        container,
                        property.Name,
                        property,
                        Config,
                        slider.Min,
                        slider.Max,
                        slider.Step,
                        slider.ValueType,
                        slider.DisplayMultiplier);

                case ConfigItemCheckBoxAttribute _ when property.PropertyType == typeof(bool):
                    return itemFactory.CreateCheckBox(container, property.Name, property, Config);

                case ConfigItemComboBoxAttribute _ when property.PropertyType.IsEnum:
                    return itemFactory.CreateComboBox(container, property.Name, property, Config, Enum.GetNames(property.PropertyType));

                default:
                    return null;
            }
        }

        private static T GetCustomItemAttribute<T>(PropertyInfo property, bool inherit = false)
            where T : Attribute
            => (T)property.GetCustomAttributes(typeof(T), inherit).FirstOrDefault();

        private void ExecuteSelectedAction()
        {
            switch (radioConfig.SelectedMode)
            {
                case RadioButtonsConfig.ModeType.ClearStuckCitizensSchedule:
                    ClearStuckCitizensSchedule();
                    break;
                case RadioButtonsConfig.ModeType.ClearStuckTouristsInHotels:
                    ClearStuckTouristsInHotels();
                    break;
                case RadioButtonsConfig.ModeType.ClearStuckCitizensInClosedBuildings:
                    ClearStuckCitizensInClosedBuildings();
                    break;
                case RadioButtonsConfig.ModeType.ClearFireBurnTimeManager:
                    ClearFireBurnTimeManager();
                    break;
                case RadioButtonsConfig.ModeType.ClearBuildingsWorkTimePrefabs:
                    ClearBuildingsWorkTimePrefabs();
                    break;
                case RadioButtonsConfig.ModeType.ClearBuildingWorkTimeGlobalSettings:
                    ClearBuildingWorkTimeGlobalSettings();
                    break;
                case RadioButtonsConfig.ModeType.ResetBuildingsGarbageBuffer:
                    ResetBuildingsGarbageBuffer();
                    break;
                case RadioButtonsConfig.ModeType.ResetBuildingsMailBuffer:
                    ResetBuildingsMailBuffer();
                    break;
                default:
                    return;
            }
        }

        private void RefreshRadioCheckboxes(object sender, PropertyChangedEventArgs e)
        {
            Debug.Log($"Radio refresh triggered. e.PropertyName={e?.PropertyName}, Count={radioCheckboxes.Count}");
            foreach (var cb in radioCheckboxes)
            {
                Debug.Log($"Refreshing {cb.Id}");
                if (cb is IValueViewItem valueItem)
                {
                    valueItem.Refresh();
                }
            }
        }

        private void ResetToDefaults()
        {
            configProvider.Configuration.ResetToDefaults();
            RefreshAllItems();
        }

        private void UseForNewGames() => configProvider.SaveDefaultConfiguration();

        private void ClearStuckCitizensSchedule() => ResidentAIPatch.RealTimeResidentAI.ClearStuckCitizensSchedule();

        private void ClearStuckTouristsInHotels() => ResidentAIPatch.RealTimeResidentAI.ClearStuckTouristsInHotels();

        private void ClearStuckCitizensInClosedBuildings() => ResidentAIPatch.RealTimeResidentAI.ClearStuckCitizensInClosedBuildings();

        public void ClearFireBurnTimeManager() =>
            ConfirmPanel.ShowModal(ConfirmPanelClearFireBurnTimeManagerTitleId, ConfirmPanelClearFireBurnTimeManagerTextId, (comp, ret) =>
        {
            if (ret != 1)
            {
                return;
            }

            FireBurnTimeManager.FireBurnTime.Clear();
        });


        public void ClearBuildingsWorkTimePrefabs() =>
            ConfirmPanel.ShowModal(ConfirmPanelClearBuildingsWorkTimePrefabsTitleId, ConfirmPanelClearBuildingsWorkTimePrefabsTextId, (comp, ret) =>
        {
            if (ret != 1)
            {
                return;
            }

            BuildingWorkTimeManager.BuildingsWorkTimePrefabs.Clear();
        });


        public void ClearBuildingWorkTimeGlobalSettings() =>
            ConfirmPanel.ShowModal(ConfirmPanelClearBuildingsWorkTimeGlobalSettingsTitleId, ConfirmPanelClearBuildingsWorkTimeGlobalSettingsTextId, (comp, ret) =>
        {
            if (ret != 1)
            {
                return;
            }

            BuildingWorkTimeGlobalConfig.Config.BuildingWorkTimeGlobalSettings.Clear();
        });

        private void ResetBuildingsGarbageBuffer() => ResourceSlowdownManager.ResetAllGarbage();

        private void ResetBuildingsMailBuffer() => ResourceSlowdownManager.ResetAllMail();

        private void ConfigProviderChanged(object sender, EventArgs e) => RefreshAllItems();

        private void RefreshAllItems()
        {
            foreach (var item in viewItems.OfType<IValueViewItem>())
            {
                item.Refresh();
            }
        }
    }
}
