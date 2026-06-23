// RealTimeMod.cs

namespace RealTime.Core
{
    using System;
    using System.IO;
    using System.Linq;
    using CitiesHarmony.API;
    using ColossalFramework;
    using ColossalFramework.Globalization;
    using ColossalFramework.IO;
    using ColossalFramework.Math;
    using ColossalFramework.Plugins;
    using ICities;
    using RealTime.Config;
    using RealTime.CustomAI;
    using RealTime.GameConnection;
    using RealTime.Localization;
    using RealTime.Managers;
    using RealTime.UI;
    using RealTime.Utils;
    using SkyTools.Configuration;
    using SkyTools.Localization;
    using SkyTools.Tools;
    using SkyTools.UI;
    using UnityEngine;

    /// <summary>The main class of the Real Time mod.</summary>
    public class RealTimeMod : LoadingExtensionBase, IUserMod
    {
        private const long WorkshopId = 3059406297;
        private const string NoWorkshopMessage = "Real Time can only run when subscribed to in Steam Workshop";

        public static readonly string modVersion = "2.8";
        private readonly string modPath = GetModPath();


        public static ConfigurationProvider<RealTimeConfig> configProvider;
        private RealTimeCore core;
        private ConfigUI configUI;
        private LocalizationProvider localizationProvider;

#if BENCHMARK
        /// <summary>
        /// Initializes a new instance of the <see cref="RealTimeMod"/> class.
        /// </summary>
        public RealTimeMod()
        {
            RealTimeBenchmark.Setup();
        }
#endif

        /// <summary>Gets the name of this mod.</summary>
        public string Name => "Real Time";

        /// <summary>Gets the description string of this mod.</summary>
        public string Description => "Adjusts the time flow and the Cims behavior to make them more real. Version: " + modVersion;

        /// <summary>Called when this mod is enabled.</summary>
        public void OnEnabled()
        {
            Log.Info("The 'Real Time' mod has been enabled, version: " + modVersion);
            configProvider = new ConfigurationProvider<RealTimeConfig>(RealTimeConfig.StorageId, Name, () => new RealTimeConfig(latestVersion: true));
            configProvider.LoadDefaultConfiguration();
            localizationProvider = new LocalizationProvider(Name, modPath);
            HarmonyHelper.DoOnHarmonyReady(() => PatchUtil.PatchAll());
            AtlasUtils.CreateAtlas();
        }

        /// <summary>Called when this mod is disabled.</summary>
        public void OnDisabled()
        {
            CloseConfigUI();
            if (configProvider?.IsDefault == true)
            {
                configProvider.SaveDefaultConfiguration();
            }

            if (HarmonyHelper.IsHarmonyInstalled)
            {
                PatchUtil.UnpatchAll();
            }



            Log.Info("The 'Real Time' mod has been disabled.");
        }

        /// <summary>Called when this mod's settings page needs to be created.</summary>
        /// <param name="helper">
        /// An <see cref="UIHelperBase"/> reference that can be used to construct the mod's settings page.
        /// </param>
        public void OnSettingsUI(UIHelperBase helper)
        {
            if (helper == null || configProvider == null)
            {
                return;
            }

            if (configProvider.Configuration == null)
            {
                Log.Warning("The 'Real Time' mod wants to display the configuration page, but the configuration is unexpectedly missing.");
                configProvider.LoadDefaultConfiguration();
            }

            IViewItemFactory itemFactory = new CitiesViewItemFactory(helper);
            CloseConfigUI();
            configUI = ConfigUI.Create(configProvider, itemFactory);
            ApplyLanguage();
            if (localizationProvider != null)
            {
                configUI?.UpdateModalTranslations(localizationProvider);
            }
        }


        public override void OnCreated(ILoading loading)
        {
            base.OnCreated(loading);
            try
            {
                AcademicYearManager.Init();
                BuildingWorkTimeManager.Init();
                EventRouteTimeManager.Init();
                FireBurnTimeManager.Init();
                HotelManager.Init();
                CommercialBuildingTypesManager.Init();
                ParkBuildingTypesManager.Init();
            }
            catch (Exception e)
            {
                Debug.LogError(e.ToString());
                AcademicYearManager.Deinit();
                BuildingWorkTimeManager.Deinit();
                EventRouteTimeManager.Deinit();
                FireBurnTimeManager.Deinit();
                CommercialBuildingTypesManager.Deinit();
                ParkBuildingTypesManager.Deinit();
            }
        }

        /// <summary>
        /// Called when a game level is loaded. If applicable, activates the Real Time mod for the loaded level.
        /// </summary>
        /// <param name="mode">The <see cref="LoadMode"/> a game level is loaded in.</param>
        public override void OnLevelLoaded(LoadMode mode)
        {
            switch (mode)
            {
                case LoadMode.LoadGame:
                case LoadMode.NewGame:
                case LoadMode.LoadScenario:
                case LoadMode.NewGameFromScenario:
                    break;

                default:
                    return;
            }

            Log.Info($"The 'Real Time' mod starts, game mode {mode}.");

            var compatibility = Compatibility.Create(localizationProvider);

            if (configProvider.Configuration.AdvancedLoggingMode)
            {
                Log.SetupDebug(Name, LogCategory.Generic, LogCategory.Movement, LogCategory.Simulation, LogCategory.State, LogCategory.Schedule, LogCategory.Events, LogCategory.Advanced);
            }
            else if (configProvider.Configuration.LoggingMode)
            {
                Log.SetupDebug(Name, LogCategory.Generic, LogCategory.Movement, LogCategory.Simulation, LogCategory.State, LogCategory.Schedule, LogCategory.Events);
            }
            else
            {
                Log.SetupDebug(Name, LogCategory.Generic);
            }


            bool isNewGame = mode == LoadMode.NewGame || mode == LoadMode.NewGameFromScenario;
            core = RealTimeCore.Run(configProvider, modPath, localizationProvider, isNewGame, compatibility);
            if (core == null)
            {
                Log.Warning("Showing a warning message to user because the mod isn't working");
                MessageBox.Show(
                    localizationProvider.Translate(TranslationKeys.Warning),
                    localizationProvider.Translate(TranslationKeys.ModNotWorkingMessage));
            }
            else
            {
                CheckCompatibility(compatibility);
            }

            var buildings = Singleton<BuildingManager>.instance.m_buildings;

            for (ushort buildingId = 0; buildingId < buildings.m_size; buildingId++)
            {
                ref var building = ref buildings.m_buffer[buildingId];
                if ((building.m_flags & Building.Flags.Created) != 0)
                {
                    ClearGarbageAndMailBufferForOldVersion(building);
                    BuildingWorkTimeCheck(buildingId, building.Info);
                    HotelCheck(buildingId, ref building);
                    AcademicYearCheck(buildingId, building.Info);
                    CommercialBuildingTypeCheck(buildingId, building.Info);
                    ParkBuildingTypeCheck(buildingId);
                }
            }
        }

        /// <summary>
        /// Called when a game level is about to be unloaded. If the Real Time mod was activated for this level,
        /// deactivates the mod for this level.
        /// </summary>
        public override void OnLevelUnloading()
        {
            if (core != null)
            {
                Log.Info("The 'Real Time' mod stops.");
                core.Stop();
                core = null;
            }

            configProvider.LoadDefaultConfiguration();
        }

        private static string GetModPath()
        {
            string addonsPath = Path.Combine(DataLocation.localApplicationData, "Addons");
            string localModsPath = Path.Combine(addonsPath, "Mods");
            string localModPath = Path.Combine(localModsPath, "RealTime");

            if (Directory.Exists(localModPath))
            {
                return localModPath;
            }

            var pluginInfo = PluginManager.instance.GetPluginsInfo()
                .FirstOrDefault(pi => pi.publishedFileID.AsUInt64 == WorkshopId);

            return pluginInfo?.modPath;
        }

        private void CheckCompatibility(Compatibility compatibility)
        {
            if (core == null)
            {
                return;
            }

            string message = null;
            bool incompatibilitiesDetected = configProvider.Configuration.ShowIncompatibilityNotifications
                && compatibility.AreAnyIncompatibleModsActive(out message);

            if (core.IsRestrictedMode)
            {
                message += localizationProvider.Translate(TranslationKeys.RestrictedMode);
            }

            if (incompatibilitiesDetected || core.IsRestrictedMode)
            {
                Notification.Notify(Name + " - " + localizationProvider.Translate(TranslationKeys.Warning), message);
            }
        }

        private void ApplyLanguage()
        {
            if (!SingletonLite<LocaleManager>.exists || localizationProvider == null)
            {
                return;
            }

            if (localizationProvider.LoadTranslation(LocaleManager.instance.language))
            {
                localizationProvider.SetEnglishUSFormatsState(configProvider.Configuration.UseEnglishUSFormats);
                core?.Translate(localizationProvider);
            }

            configUI?.Translate(localizationProvider);
        }

        private void CloseConfigUI()
        {
            if (configUI != null)
            {
                configUI.Close();
                configUI = null;
            }
        }

        private void ClearGarbageAndMailBufferForOldVersion(Building building)
        {
            var version = typeof(RealTimeMod).Assembly.GetName().Version;
            int major = version.Major;
            int minor = version.Minor;
            building.m_garbageBuffer = 0;
            building.m_mailBuffer = 0;
            if (major < 2 || major >= 2 && minor < 6)
            {
                // zero
                building.m_garbageBuffer = 0;
                building.m_mailBuffer = 0;
            }
        }

        private void BuildingWorkTimeCheck(ushort buildingID, BuildingInfo buildingInfo)
        {
            if (BuildingWorkTimeManager.BuildingWorkTimeExist(buildingID) && !BuildingWorkTimeManager.ShouldHaveBuildingWorkTime(buildingID))
            {
                BuildingWorkTimeManager.RemoveBuildingWorkTime(buildingID);
            }
            else if (!BuildingWorkTimeManager.BuildingWorkTimeExist(buildingID) && BuildingWorkTimeManager.ShouldHaveBuildingWorkTime(buildingID))
            {
                BuildingWorkTimeManager.CreateBuildingWorkTime(buildingID, buildingInfo);

                if (BuildingWorkTimeManager.PrefabExist(buildingInfo))
                {
                    var buildignPrefab = BuildingWorkTimeManager.GetPrefab(buildingInfo);
                    UpdateBuildingSettings.SetBuildingToPrefab(buildingID, buildignPrefab);
                }
                else if (BuildingWorkTimeGlobalConfig.Config.GlobalSettingsExist(buildingInfo))
                {
                    var buildignGlobal = BuildingWorkTimeGlobalConfig.Config.GetGlobalSettings(buildingInfo);
                    UpdateBuildingSettings.SetBuildingToGlobal(buildingID, buildignGlobal);
                }
            }
        }

        private void HotelCheck(ushort buildingID, ref Building data)
        {
            if (BuildingManagerConnection.IsHotel(buildingID))
            {
                if(data.Info.GetAI() is PrivateBuildingAI privateBuildingAI)
                {
                    data.m_level = (byte)Mathf.Max(data.m_level, (int)privateBuildingAI.m_info.m_class.m_level);
                    privateBuildingAI.CalculateWorkplaceCount((ItemClass.Level)data.m_level, new Randomizer(buildingID), data.Width, data.Length, out int level, out int level2, out int level3, out int level4);
                    privateBuildingAI.AdjustWorkplaceCount(buildingID, ref data, ref level, ref level2, ref level3, ref level4);
                    int workCount = level + level2 + level3 + level4;
                    int visitCount = privateBuildingAI.CalculateVisitplaceCount((ItemClass.Level)data.m_level, new Randomizer(buildingID), data.Width, data.Length);
                    int hotelRoomCount = visitCount;
                    if (BuildingWorkTimeManager.HotelNamesList.ContainsKey(data.Info.name))
                    {
                        hotelRoomCount = BuildingWorkTimeManager.HotelNamesList[data.Info.name];
                    }
                    visitCount = hotelRoomCount * 20 / 100;
                    EnsureCitizenUnits(buildingID, ref data, 0, workCount, visitCount, 0, hotelRoomCount);
                    data.m_roomMax = (ushort)hotelRoomCount;
                    if (!HotelManager.HotelExist(buildingID) && data.m_roomUsed < data.m_roomMax)
                    {
                        HotelManager.AddHotel(buildingID);
                    }
                }
                else if (!HotelManager.HotelExist(buildingID))
                {
                    HotelManager.AddHotel(buildingID);
                }  
            }
        }

        private void AcademicYearCheck(ushort buildingID, BuildingInfo buildingInfo)
        {
            if (buildingInfo.GetAI() is MainCampusBuildingAI && !AcademicYearManager.MainCampusBuildingExist(buildingID))
            {
                AcademicYearManager.CreateAcademicYearData(buildingID);
            }
        }

        private void CommercialBuildingTypeCheck(ushort buildingID, BuildingInfo buildingInfo)
        {
            if (BuildingManagerConnection.IsAllowedCommercialBuildingType(buildingID) && !CommercialBuildingTypesManager.CommercialBuildingTypeExist(buildingID))
            {
                if (buildingInfo.m_class.m_subService == ItemClass.SubService.CommercialLeisure)
                {
                    CommercialBuildingTypesManager.CreateCommercialBuildingType(buildingID, CommercialBuildingType.Entertainment | CommercialBuildingType.Food);
                }
                else
                {
                    CommercialBuildingTypesManager.CreateCommercialBuildingType(buildingID, CommercialBuildingType.Shopping | CommercialBuildingType.Entertainment | CommercialBuildingType.Food);
                }
            }

        }

        private void ParkBuildingTypeCheck(ushort buildingID)
        {
            if (BuildingManagerConnection.IsAllowedParkBuildingType(buildingID) && !ParkBuildingTypesManager.ParkBuildingTypeExist(buildingID))
            {
                ParkBuildingTypesManager.CreateParkBuildingType(buildingID, ParkBuildingType.Generic);
            }
        }

        private void EnsureCitizenUnits(ushort buildingID, ref Building data, int homeCount = 0, int workCount = 0, int visitCount = 0, int studentCount = 0, int hotelCount = 0)
        {
            if ((data.m_flags & (Building.Flags.Abandoned | Building.Flags.Collapsed)) != 0)
            {
                return;
            }
            var wealthLevel = Citizen.GetWealthLevel((ItemClass.Level)data.m_level);
            var instance = Singleton<CitizenManager>.instance;
            uint num = 0u;
            uint num2 = data.m_citizenUnits;
            int num3 = 0;
            while (num2 != 0)
            {
                var flags = instance.m_units.m_buffer[num2].m_flags;
                if ((flags & CitizenUnit.Flags.Home) != 0)
                {
                    instance.m_units.m_buffer[num2].SetWealthLevel(wealthLevel);
                    homeCount--;
                }
                if ((flags & CitizenUnit.Flags.Work) != 0)
                {
                    workCount -= 5;
                }
                if ((flags & CitizenUnit.Flags.Visit) != 0)
                {
                    visitCount -= 5;
                }
                if ((flags & CitizenUnit.Flags.Student) != 0)
                {
                    studentCount -= 5;
                }
                if ((flags & CitizenUnit.Flags.Hotel) != 0)
                {
                    hotelCount -= 5;
                }
                num = num2;
                num2 = instance.m_units.m_buffer[num2].m_nextUnit;
                if (++num3 > Singleton<CitizenManager>.instance.m_units.m_size)
                {
                    CODebugBase<LogChannel>.Error(LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
                    break;
                }
            }
            homeCount = Mathf.Max(0, homeCount);
            workCount = Mathf.Max(0, workCount);
            visitCount = Mathf.Max(0, visitCount);
            studentCount = Mathf.Max(0, studentCount);
            hotelCount = Mathf.Max(0, hotelCount);
            if (homeCount == 0 && workCount == 0 && visitCount == 0 && studentCount == 0 && hotelCount == 0)
            {
                return;
            }
            if (instance.CreateUnits(out uint firstUnit, ref Singleton<SimulationManager>.instance.m_randomizer, buildingID, 0, homeCount, workCount, visitCount, 0, studentCount, hotelCount))
            {
                if (num != 0)
                {
                    instance.m_units.m_buffer[num].m_nextUnit = firstUnit;
                }
                else
                {
                    data.m_citizenUnits = firstUnit;
                }
            }
        }
    }
}
