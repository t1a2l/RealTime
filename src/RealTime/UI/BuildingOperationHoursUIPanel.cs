namespace RealTime.UI
{
    using ColossalFramework;
    using ColossalFramework.UI;
    using RealTime.Config;
    using RealTime.CustomAI;
    using RealTime.Localization;
    using RealTime.Managers;
    using RealTime.Utils;
    using RealTime.Utils.UIUtils;
    using SkyTools.Localization;
    using UnityEngine;

    internal class BuildingOperationHoursPanel : UIPanel
    {
        private UIPanel m_innerPanel;
        private UILabel m_settingsTitle;
        private UILabel m_settingsStatus;

        internal UICheckBox m_workAtNight;
        internal UICheckBox m_workAtWeekands;
        internal UICheckBox m_hasExtendedWorkShift;
        internal UICheckBox m_hasContinuousWorkShift;
        internal UICheckBox m_ignorePolicy;

        internal UILabel m_workShiftsLabel;
        internal UISlider m_workShifts;
        internal UILabel m_workShiftsCount;

        internal UIButton m_saveBuildingSettingsBtn;
        internal UIButton m_returnToDefaultBtn;
        internal UIButton m_applyPrefabSettingsBtn;
        internal UIButton m_applyGlobalSettingsBtn;

        internal UIButton m_setPrefabSettingsBtn;
        internal UIButton m_setGlobalSettingsBtn;

        internal UIButton m_deletePrefabSettingsBtn;
        internal UIButton m_deleteGlobalSettingsBtn;

        internal UIButton m_unlockSettingsBtn;
        internal UIButton m_lockUnlockChangesBtn;

        private string t_defaultSettingsStatus;
        private string t_buildingSettingsStatus;
        private string t_prefabSettingsStatus;
        private string t_globalSettingsStatus;
        private string t_confirmPanelSetPrefabTitle;
        private string t_confirmPanelSetPrefabText;
        private string t_confirmPanelSetGlobalTitle;
        private string t_confirmPanelSetGlobalText;
        private string t_confirmPanelDeletePrefabTitle;
        private string t_confirmPanelDeletePrefabText;
        private string t_confirmPanelDeleteGlobalTitle;
        private string t_confirmPanelDeleteGlobalText;

        public override void Awake()
        {
            base.Awake();

            name = "OperationHoursUIPanel";
            backgroundSprite = "SubcategoriesPanel";
            opacity = 0.90f;
            isVisible = false;
            height = 470f;
            width = 510f;

            m_settingsTitle = UILabels.CreateLabel(this, "SettingsTitle", "", "");
            m_settingsTitle.font = UIFonts.GetUIFont("OpenSans-Regular");
            m_settingsTitle.textAlignment = UIHorizontalAlignment.Center;
            m_settingsTitle.textColor = new Color32(78, 184, 126, 255);
            m_settingsTitle.relativePosition = new Vector3(130f, 20f);
            m_settingsTitle.textScale = 1.2f;

            m_settingsStatus = UILabels.CreateLabel(this, "SettingsStatus", "", "");
            m_settingsStatus.font = UIFonts.GetUIFont("OpenSans-Regular");
            m_settingsStatus.textAlignment = UIHorizontalAlignment.Center;
            m_settingsStatus.textColor = new Color32(240, 190, 199, 255);
            m_settingsStatus.relativePosition = new Vector3(110f, 95f);
            m_settingsStatus.textScale = 0.9f;

            m_workAtNight = UICheckBoxes.CreateCheckBox(this, "WorkAtNight", "", "", false);
            m_workAtNight.width = 110f;
            m_workAtNight.label.textColor = new Color32(185, 221, 254, 255);
            m_workAtNight.label.textScale = 0.8125f;
            m_workAtNight.relativePosition = new Vector3(30f, 130f);
            m_workAtNight.eventCheckChanged += (component, value) =>
            {
                m_workAtNight.isChecked = value;
                UpdateSlider();
            };
            
            m_workAtWeekands = UICheckBoxes.CreateCheckBox(this, "WorkAtWeekands", "", "", false);
            m_workAtWeekands.width = 110f;
            m_workAtWeekands.label.textColor = new Color32(185, 221, 254, 255);
            m_workAtWeekands.label.textScale = 0.8125f;   
            m_workAtWeekands.relativePosition = new Vector3(30f, 170f);
            m_workAtWeekands.eventCheckChanged += (component, value) => m_workAtWeekands.isChecked = value;

            m_hasExtendedWorkShift = UICheckBoxes.CreateCheckBox(this, "HasExtendedWorkShift", "", "", false);
            m_hasExtendedWorkShift.width = 110f;
            m_hasExtendedWorkShift.label.textColor = new Color32(185, 221, 254, 255);
            m_hasExtendedWorkShift.label.textScale = 0.8125f;
            m_hasExtendedWorkShift.relativePosition = new Vector3(30f, 210f);
            m_hasExtendedWorkShift.eventCheckChanged += (component, value) =>
            {
                m_hasExtendedWorkShift.isChecked = value;
                if (m_hasExtendedWorkShift.isChecked)
                {
                    m_hasContinuousWorkShift.isChecked = false;
                }
                UpdateSlider();
            };

            m_hasContinuousWorkShift = UICheckBoxes.CreateCheckBox(this, "HasContinuousWorkShift", "", "", false);
            m_hasContinuousWorkShift.width = 110f;
            m_hasContinuousWorkShift.label.textColor = new Color32(185, 221, 254, 255);
            m_hasContinuousWorkShift.label.textScale = 0.8125f;       
            m_hasContinuousWorkShift.relativePosition = new Vector3(30f, 250f);
            m_hasContinuousWorkShift.eventCheckChanged += (component, value) =>
            {
                m_hasContinuousWorkShift.isChecked = value;
                if (m_hasContinuousWorkShift.isChecked)
                {
                    m_hasExtendedWorkShift.isChecked = false;
                }
                UpdateSlider();
            };

            m_ignorePolicy = UICheckBoxes.CreateCheckBox(this, "IgnorePolicy", "", "", false);
            m_ignorePolicy.width = 110f;
            m_ignorePolicy.label.textColor = new Color32(185, 221, 254, 255);
            m_ignorePolicy.label.textScale = 0.8125f; 
            m_ignorePolicy.relativePosition = new Vector3(30f, 290f);
            m_ignorePolicy.eventCheckChanged += (component, value) =>
            {
                m_ignorePolicy.isChecked = value;
                UpdateSlider();
            };

            m_innerPanel = UIPanels.CreatePanel(this, "OperationHoursInnerPanel");
            m_innerPanel.backgroundSprite = "GenericPanelLight";
            m_innerPanel.color = new Color32(206, 206, 206, 255);
            m_innerPanel.size = new Vector2(235f, 66f);
            m_innerPanel.relativePosition = new Vector3(15f, 322f);

            m_workShiftsLabel = UILabels.CreateLabel(m_innerPanel, "WorkShiftsTitle", "", "");
            m_workShiftsLabel.font = UIFonts.GetUIFont("OpenSans-Regular");
            m_workShiftsLabel.textAlignment = UIHorizontalAlignment.Center;
            m_workShiftsLabel.relativePosition = new Vector3(10f, 10f);

            m_workShifts = UISliders.CreateSlider(m_innerPanel, "ShiftCount", "", 1, 3, 1, 1);
            m_workShifts.size = new Vector2(130f, 8f);
            m_workShifts.relativePosition = new Vector3(25f, 48f);
            m_workShifts.disabledColor = Color.black;
            m_workShifts.eventValueChanged += (component, value) =>
            {
                if (m_workShiftsCount != null)
                {
                    if (value == -1)
                    {
                        value = 1;
                    }
                    m_workShiftsCount.text = value.ToString();
                }
            };

            m_workShiftsCount = UILabels.CreateLabel(m_innerPanel, "OperationHoursInnerCount", "", "");
            m_workShiftsCount.textAlignment = UIHorizontalAlignment.Right;
            m_workShiftsCount.verticalAlignment = UIVerticalAlignment.Top;
            m_workShiftsCount.textColor = new Color32(185, 221, 254, 255);
            m_workShiftsCount.textScale = 1f;
            m_workShiftsCount.autoSize = false;
            m_workShiftsCount.size = new Vector2(30f, 16f);
            m_workShiftsCount.relativePosition = new Vector3(150f, 44f);

            m_saveBuildingSettingsBtn = UIButtons.CreateButton(this, 260f, 120f, "SaveBuildingSettings", "", "");
            m_saveBuildingSettingsBtn.eventClicked += SaveBuildingSettings;

            m_returnToDefaultBtn = UIButtons.CreateButton(this, 260f, 170f, "ReturnToDefault", "", "");
            m_returnToDefaultBtn.eventClicked += ReturnToDefault;

            m_applyPrefabSettingsBtn = UIButtons.CreateButton(this, 260f, 220f, "ApplyPrefabSettings", "", "");
            m_applyPrefabSettingsBtn.eventClicked += ApplyPrefabSettings;

            m_applyGlobalSettingsBtn = UIButtons.CreateButton(this, 260f, 270f, "ApplyGlobalSettings", "", "");
            m_applyGlobalSettingsBtn.eventClicked += ApplyGlobalSettings;

            m_setPrefabSettingsBtn = UIButtons.CreateButton(this, 260f, 320f, "SetPrefabSettings", "", "");
            m_setPrefabSettingsBtn.eventClicked += SetPrefabSettings;

            m_setGlobalSettingsBtn = UIButtons.CreateButton(this, 260f, 370f, "SetGlobalSettings", "", "");
            m_setGlobalSettingsBtn.eventClicked += SetGlobalSettings;

            m_deletePrefabSettingsBtn = UIButtons.CreateButton(this, 15f, 420f, "DeletePrefabSettings", "", "");
            m_deletePrefabSettingsBtn.eventClicked += DeletePrefabSettings;

            m_deleteGlobalSettingsBtn = UIButtons.CreateButton(this, 260f, 420f, "DeleteGlobalSettings", "", "");
            m_deleteGlobalSettingsBtn.eventClicked += DeleteGlobalSettings;

            m_unlockSettingsBtn = UIButtons.CreateButton(this, 130f, 55f, "UnlockSettings", "", "");
            m_unlockSettingsBtn.eventClicked += UnlockSettings;

            m_lockUnlockChangesBtn = UIButtons.CreateButton(this, 10f, 55f, "LockUnLockChanges", "", "", 32, 32);

            m_lockUnlockChangesBtn.atlas = TextureUtils.GetAtlas("LockButtonAtlas");
            m_lockUnlockChangesBtn.normalFgSprite = "UnLock";
            m_lockUnlockChangesBtn.disabledFgSprite = "UnLock";
            m_lockUnlockChangesBtn.focusedFgSprite = "UnLock";
            m_lockUnlockChangesBtn.hoveredFgSprite = "UnLock";
            m_lockUnlockChangesBtn.pressedFgSprite = "UnLock";

            m_lockUnlockChangesBtn.eventClicked += LockUnlockChanges;

            m_workAtNight.Disable();
            m_workAtWeekands.Disable();
            m_hasExtendedWorkShift.Disable();
            m_hasContinuousWorkShift.Disable();
            m_ignorePolicy.Disable();
            m_workShifts.Disable();

            m_saveBuildingSettingsBtn.Disable();
            m_returnToDefaultBtn.Disable();
            m_applyPrefabSettingsBtn.Disable();
            m_applyGlobalSettingsBtn.Disable();
            m_setPrefabSettingsBtn.Disable();
            m_setGlobalSettingsBtn.Disable();
            m_deletePrefabSettingsBtn.Disable();
            m_deleteGlobalSettingsBtn.Disable();

            isVisible = false;
        }

        public override void Start() => base.Start();

        public void UpdateData(float panelHeight, ILocalizationProvider localizationProvider)
        {
            Translate(localizationProvider);
            relativePosition = new Vector3(parent.width + 1, panelHeight);
        }

        private void Translate(ILocalizationProvider localizationProvider)
        {
            m_settingsTitle.text = localizationProvider.Translate(TranslationKeys.SettingsTitle);
            m_workAtNight.text = localizationProvider.Translate(TranslationKeys.WorkAtNight);
            m_workAtNight.tooltip = localizationProvider.Translate(TranslationKeys.WorkAtNightTooltip);
            m_workAtWeekands.text = localizationProvider.Translate(TranslationKeys.WorkAtWeekands);
            m_workAtWeekands.tooltip = localizationProvider.Translate(TranslationKeys.WorkAtWeekandsTooltip);
            m_hasExtendedWorkShift.text = localizationProvider.Translate(TranslationKeys.HasExtendedWorkShift);
            m_hasExtendedWorkShift.tooltip = localizationProvider.Translate(TranslationKeys.HasExtendedWorkShiftTooltip);
            m_hasContinuousWorkShift.text = localizationProvider.Translate(TranslationKeys.HasContinuousWorkShift);
            m_hasContinuousWorkShift.tooltip = localizationProvider.Translate(TranslationKeys.HasContinuousWorkShiftTooltip);
            m_ignorePolicy.text = localizationProvider.Translate(TranslationKeys.IgnorePolicy);
            m_ignorePolicy.tooltip = localizationProvider.Translate(TranslationKeys.IgnorePolicyTooltip);
            m_workShiftsLabel.text = localizationProvider.Translate(TranslationKeys.ShiftCountTitle);
            m_workShifts.tooltip = localizationProvider.Translate(TranslationKeys.ShiftCountTooltip);

            m_saveBuildingSettingsBtn.text = localizationProvider.Translate(TranslationKeys.SaveBuildingSettings);
            m_saveBuildingSettingsBtn.tooltip = localizationProvider.Translate(TranslationKeys.SaveBuildingSettingsTooltip);
            m_returnToDefaultBtn.text = localizationProvider.Translate(TranslationKeys.ReturnToDefault);
            m_returnToDefaultBtn.tooltip = localizationProvider.Translate(TranslationKeys.ReturnToDefaultTooltip);
            m_applyPrefabSettingsBtn.text = localizationProvider.Translate(TranslationKeys.ApplyPrefabSettings);
            m_applyPrefabSettingsBtn.tooltip = localizationProvider.Translate(TranslationKeys.ApplyPrefabSettingsTooltip);
            m_applyGlobalSettingsBtn.text = localizationProvider.Translate(TranslationKeys.ApplyGlobalSettings);
            m_applyGlobalSettingsBtn.tooltip = localizationProvider.Translate(TranslationKeys.ApplyGlobalSettingsTooltip);
            m_setPrefabSettingsBtn.text = localizationProvider.Translate(TranslationKeys.SetPrefabSettings);
            m_setPrefabSettingsBtn.tooltip = localizationProvider.Translate(TranslationKeys.SetPrefabSettingsTooltip);
            m_setGlobalSettingsBtn.text = localizationProvider.Translate(TranslationKeys.SetGlobalSettings);
            m_setGlobalSettingsBtn.tooltip = localizationProvider.Translate(TranslationKeys.SetGlobalSettingsTooltip);
            m_deletePrefabSettingsBtn.text = localizationProvider.Translate(TranslationKeys.DeletePrefabSettings);
            m_deletePrefabSettingsBtn.tooltip = localizationProvider.Translate(TranslationKeys.DeletePrefabSettingsTooltip);
            m_deleteGlobalSettingsBtn.text = localizationProvider.Translate(TranslationKeys.DeleteGlobalSettings);
            m_deleteGlobalSettingsBtn.tooltip = localizationProvider.Translate(TranslationKeys.DeleteGlobalSettingsTooltip);
            m_unlockSettingsBtn.text = localizationProvider.Translate(TranslationKeys.UnlockSettings);
            m_unlockSettingsBtn.tooltip = localizationProvider.Translate(TranslationKeys.UnlockSettingsTooltip);
            m_lockUnlockChangesBtn.tooltip = localizationProvider.Translate(TranslationKeys.LockUnlockChangesTooltip);

            t_defaultSettingsStatus = localizationProvider.Translate(TranslationKeys.DefaultSettingsStatus);
            t_buildingSettingsStatus = localizationProvider.Translate(TranslationKeys.BuildingSettingsStatus);
            t_prefabSettingsStatus = localizationProvider.Translate(TranslationKeys.PrefabSettingsStatus);
            t_globalSettingsStatus = localizationProvider.Translate(TranslationKeys.GlobalSettingsStatus);

            t_confirmPanelSetPrefabTitle = localizationProvider.Translate(TranslationKeys.ConfirmPanelSetPrefabTitle);
            t_confirmPanelSetPrefabText = localizationProvider.Translate(TranslationKeys.ConfirmPanelSetPrefabText);
            t_confirmPanelSetGlobalTitle = localizationProvider.Translate(TranslationKeys.ConfirmPanelSetGlobalTitle);
            t_confirmPanelSetGlobalText = localizationProvider.Translate(TranslationKeys.ConfirmPanelSetGlobalText);

            t_confirmPanelDeletePrefabTitle = localizationProvider.Translate(TranslationKeys.ConfirmPanelDeletePrefabTitle);
            t_confirmPanelDeletePrefabText = localizationProvider.Translate(TranslationKeys.ConfirmPanelDeletePrefabText);
            t_confirmPanelDeleteGlobalTitle = localizationProvider.Translate(TranslationKeys.ConfirmPanelDeleteGlobalTitle);
            t_confirmPanelDeleteGlobalText = localizationProvider.Translate(TranslationKeys.ConfirmPanelDeleteGlobalText);
        }

        private void UnlockSettings(UIComponent c, UIMouseEventParameter eventParameter)
        {
            m_workAtNight.Enable();
            m_workAtWeekands.Enable();
            m_hasExtendedWorkShift.Enable();
            m_hasContinuousWorkShift.Enable();
            m_ignorePolicy.Enable();
            m_workShifts.Enable();

            m_saveBuildingSettingsBtn.Enable();
            m_returnToDefaultBtn.Enable();

            ushort buildingID = WorldInfoPanel.GetCurrentInstanceID().Building;
            var building = Singleton<BuildingManager>.instance.m_buildings.m_buffer[buildingID];

            if (BuildingWorkTimeManager.PrefabExist(building.Info))
            {
                m_applyPrefabSettingsBtn.Enable();
                m_setPrefabSettingsBtn.Disable();
                m_deletePrefabSettingsBtn.Enable();
            }
            else
            {
                m_applyPrefabSettingsBtn.Disable();
                m_setPrefabSettingsBtn.Enable();
                m_deletePrefabSettingsBtn.Disable();
            }

            if (BuildingWorkTimeGlobalConfig.Config.GlobalSettingsExist(building.Info))
            {
                m_applyGlobalSettingsBtn.Enable();
                m_setGlobalSettingsBtn.Disable();
                m_deleteGlobalSettingsBtn.Enable();
            }
            else
            {
                m_applyGlobalSettingsBtn.Disable();
                m_setGlobalSettingsBtn.Enable();
                m_deleteGlobalSettingsBtn.Disable();
            }

            m_unlockSettingsBtn.Hide();
        }

        private void UpdateSlider()
        {
            if (m_hasContinuousWorkShift.isChecked)
            {
                if (m_workAtNight.isChecked)
                {
                    m_workShifts.maxValue = 2;
                    m_workShifts.minValue = 2;
                    m_workShifts.value = 2;
                    m_workShiftsCount.text = "2";
                    m_workShifts.Disable();
                }
                else
                {
                    m_workShifts.maxValue = 1;
                    m_workShifts.minValue = 1;
                    m_workShiftsCount.text = "1";
                    m_workShifts.value = 1;
                    m_workShifts.Disable();
                }
            }
            else
            {
                if (m_workAtNight.isChecked)
                {
                    m_workShifts.maxValue = 3;
                    m_workShifts.minValue = 3;
                    m_workShifts.value = 3;
                    m_workShiftsCount.text = "3";
                    m_workShifts.Disable();
                }
                else
                {
                    m_workShifts.maxValue = 2;
                    m_workShifts.minValue = 1;
                    if(!m_unlockSettingsBtn.isVisible)
                    {
                        m_workShifts.Enable();
                    }
                }
            }
        }

        public void RefreshData(ushort buildingID, BuildingWorkTimeManager.WorkTime buildingWorkTime)
        {
            var buildingInfo = Singleton<BuildingManager>.instance.m_buildings.m_buffer[buildingID].Info;

            if (!buildingWorkTime.IsPrefab && !buildingWorkTime.IsGlobal)
            {
                m_settingsStatus.text = buildingWorkTime.IsDefault ? t_defaultSettingsStatus : t_buildingSettingsStatus;
                m_workAtNight.isChecked = buildingWorkTime.WorkAtNight;
                m_workAtWeekands.isChecked = buildingWorkTime.WorkAtWeekands;
                m_hasExtendedWorkShift.isChecked = buildingWorkTime.HasExtendedWorkShift;
                m_hasContinuousWorkShift.isChecked = buildingWorkTime.HasContinuousWorkShift;
                m_ignorePolicy.isChecked = buildingWorkTime.IgnorePolicy;
                m_workShifts.value = buildingWorkTime.WorkShifts;
                m_workShiftsCount.text = buildingWorkTime.WorkShifts.ToString();
            }
            else if (BuildingWorkTimeManager.PrefabExist(buildingInfo) && buildingWorkTime.IsPrefab && !buildingWorkTime.IsLocked)
            {
                var buildingWorkTimePrefab = BuildingWorkTimeManager.GetPrefab(buildingInfo);

                m_settingsStatus.text = t_prefabSettingsStatus;
                m_workAtNight.isChecked = buildingWorkTimePrefab.WorkAtNight;
                m_workAtWeekands.isChecked = buildingWorkTimePrefab.WorkAtWeekands;
                m_hasExtendedWorkShift.isChecked = buildingWorkTimePrefab.HasExtendedWorkShift;
                m_hasContinuousWorkShift.isChecked = buildingWorkTimePrefab.HasContinuousWorkShift;
                m_ignorePolicy.isChecked = buildingWorkTimePrefab.IgnorePolicy;
                m_workShifts.value = buildingWorkTimePrefab.WorkShifts;
                m_workShiftsCount.text = buildingWorkTimePrefab.WorkShifts.ToString();
            }
            else if(BuildingWorkTimeGlobalConfig.Config.GlobalSettingsExist(buildingInfo) && buildingWorkTime.IsGlobal && !buildingWorkTime.IsLocked)
            {
                var buildingWorkTimeGlobal = BuildingWorkTimeGlobalConfig.Config.GetGlobalSettings(buildingInfo);

                m_settingsStatus.text = t_globalSettingsStatus;
                m_workAtNight.isChecked = buildingWorkTimeGlobal.WorkAtNight;
                m_workAtWeekands.isChecked = buildingWorkTimeGlobal.WorkAtWeekands;
                m_hasExtendedWorkShift.isChecked = buildingWorkTimeGlobal.HasExtendedWorkShift;
                m_hasContinuousWorkShift.isChecked = buildingWorkTimeGlobal.HasContinuousWorkShift;
                m_ignorePolicy.isChecked = buildingWorkTimeGlobal.IgnorePolicy;
                m_workShifts.value = buildingWorkTimeGlobal.WorkShifts;
                m_workShiftsCount.text = buildingWorkTimeGlobal.WorkShifts.ToString();
            }

            UpdateSlider();

            m_workAtNight.relativePosition = new Vector3(30f, 130f);
            m_workAtWeekands.relativePosition = new Vector3(30f, 170f);
            m_hasExtendedWorkShift.relativePosition = new Vector3(30f, 210f);
            m_hasContinuousWorkShift.relativePosition = new Vector3(30f, 250f);
            m_ignorePolicy.relativePosition = new Vector3(30f, 290f);

            m_workShiftsLabel.relativePosition = new Vector3(10f, 10f);
            m_workShifts.relativePosition = new Vector3(25f, 48f);
            m_workShiftsCount.relativePosition = new Vector3(150f, 44f);

            string spriteName = buildingWorkTime.IsLocked ? "Lock" : "UnLock";

            m_lockUnlockChangesBtn.normalFgSprite = spriteName;
            m_lockUnlockChangesBtn.disabledFgSprite = spriteName;
            m_lockUnlockChangesBtn.focusedFgSprite = spriteName;
            m_lockUnlockChangesBtn.hoveredFgSprite = spriteName;
            m_lockUnlockChangesBtn.pressedFgSprite = spriteName;
        }

        private void LockUnlockChanges(UIComponent c, UIMouseEventParameter eventParameter)
        {
            ushort buildingID = WorldInfoPanel.GetCurrentInstanceID().Building;

            var buildingWorkTime = BuildingWorkTimeManager.GetBuildingWorkTime(buildingID);

            string spriteName = buildingWorkTime.IsLocked ? "UnLock" : "Lock";

            m_lockUnlockChangesBtn.normalFgSprite = spriteName;
            m_lockUnlockChangesBtn.disabledFgSprite = spriteName;
            m_lockUnlockChangesBtn.focusedFgSprite = spriteName;
            m_lockUnlockChangesBtn.hoveredFgSprite = spriteName;
            m_lockUnlockChangesBtn.pressedFgSprite = spriteName;

            UpdateBuildingSettings.ChangeBuildingLockStatus(buildingID, !buildingWorkTime.IsLocked);
        }

        private void ReturnToDefault(UIComponent c, UIMouseEventParameter eventParameter)
        {
            ushort buildingID = WorldInfoPanel.GetCurrentInstanceID().Building;
            var buildingInfo = Singleton<BuildingManager>.instance.m_buildings.m_buffer[buildingID].Info;
            BackToDefault(buildingID, buildingInfo);
        }

        private void SaveBuildingSettings(UIComponent c, UIMouseEventParameter eventParameter)
        {
            ushort buildingID = WorldInfoPanel.GetCurrentInstanceID().Building;

            bool is_locked = false;
            if(m_lockUnlockChangesBtn.normalFgSprite == "Lock")
            {
                is_locked = true;
            }

            var newBuildingSettings = new BuildingWorkTimeManager.WorkTime
            {
                WorkAtNight = m_workAtNight.isChecked,
                WorkAtWeekands = m_workAtWeekands.isChecked,
                HasExtendedWorkShift = m_hasExtendedWorkShift.isChecked,
                HasContinuousWorkShift = m_hasContinuousWorkShift.isChecked,
                IgnorePolicy = m_ignorePolicy.isChecked,
                WorkShifts = (int)m_workShifts.value,
                IsLocked = is_locked
            };

            UpdateBuildingSettings.SaveNewSettings(buildingID, newBuildingSettings);

            RefreshData(buildingID, newBuildingSettings);
        }

        private void ApplyPrefabSettings(UIComponent c, UIMouseEventParameter eventParameter)
        {
            ushort buildingID = WorldInfoPanel.GetCurrentInstanceID().Building;
            var buildingInfo = Singleton<BuildingManager>.instance.m_buildings.m_buffer[buildingID].Info;

            var buildingWorkTime = BuildingWorkTimeManager.GetBuildingWorkTime(buildingID);

            if (BuildingWorkTimeManager.PrefabExist(buildingInfo) && !buildingWorkTime.IsLocked)
            {
                var prefabRecord = BuildingWorkTimeManager.GetPrefab(buildingInfo);
                m_workAtNight.isChecked = prefabRecord.WorkAtNight;
                m_workAtWeekands.isChecked = prefabRecord.WorkAtWeekands;
                m_hasExtendedWorkShift.isChecked = prefabRecord.HasExtendedWorkShift;
                m_hasContinuousWorkShift.isChecked = prefabRecord.HasContinuousWorkShift;
                m_ignorePolicy.isChecked = prefabRecord.IgnorePolicy;
                m_workShifts.value = prefabRecord.WorkShifts;
                m_settingsStatus.text = t_prefabSettingsStatus;
                m_workShiftsCount.text = prefabRecord.WorkShifts.ToString();

                UpdateBuildingSettings.SetBuildingToPrefab(buildingID, prefabRecord);
            }
        }

        private void ApplyGlobalSettings(UIComponent c, UIMouseEventParameter eventParameter)
        {
            ushort buildingID = WorldInfoPanel.GetCurrentInstanceID().Building;
            var buildingInfo = Singleton<BuildingManager>.instance.m_buildings.m_buffer[buildingID].Info;

            var buildingWorkTime = BuildingWorkTimeManager.GetBuildingWorkTime(buildingID);

            if (BuildingWorkTimeGlobalConfig.Config.GlobalSettingsExist(buildingInfo) && !buildingWorkTime.IsLocked)
            {
                var buildingWorkTimeGlobal = BuildingWorkTimeGlobalConfig.Config.GetGlobalSettings(buildingInfo);

                m_workAtNight.isChecked = buildingWorkTimeGlobal.WorkAtNight;
                m_workAtWeekands.isChecked = buildingWorkTimeGlobal.WorkAtWeekands;
                m_hasExtendedWorkShift.isChecked = buildingWorkTimeGlobal.HasExtendedWorkShift;
                m_hasContinuousWorkShift.isChecked = buildingWorkTimeGlobal.HasContinuousWorkShift;
                m_ignorePolicy.isChecked = buildingWorkTimeGlobal.IgnorePolicy;
                m_workShifts.value = buildingWorkTimeGlobal.WorkShifts;
                m_settingsStatus.text = t_globalSettingsStatus;
                m_workShiftsCount.text = buildingWorkTimeGlobal.WorkShifts.ToString();

                UpdateBuildingSettings.SetBuildingToGlobal(buildingID, buildingWorkTimeGlobal);
            }
        }

        private void SetPrefabSettings(UIComponent c, UIMouseEventParameter eventParameter) => ConfirmPanel.ShowModal(t_confirmPanelSetPrefabTitle, t_confirmPanelSetPrefabText, (comp, ret) =>
        {
            if (ret != 1)
            {
                return;
            }
            ushort buildingID = WorldInfoPanel.GetCurrentInstanceID().Building;

            var buildingWorkTime = BuildingWorkTimeManager.GetBuildingWorkTime(buildingID);

            var newPrefabSettings = new BuildingWorkTimeManager.WorkTime
            {
                WorkAtNight = m_workAtNight.isChecked,
                WorkAtWeekands = m_workAtWeekands.isChecked,
                HasExtendedWorkShift = m_hasExtendedWorkShift.isChecked,
                HasContinuousWorkShift = m_hasContinuousWorkShift.isChecked,
                IgnorePolicy = m_ignorePolicy.isChecked,
                WorkShifts = (int)m_workShifts.value
            };

            if (!buildingWorkTime.IsLocked)
            {
                m_settingsStatus.text = t_prefabSettingsStatus;
                m_workShiftsCount.text = newPrefabSettings.WorkShifts.ToString();
            }

            UpdateBuildingSettings.CreatePrefabSettings(buildingID, newPrefabSettings);

        });

        private void SetGlobalSettings(UIComponent c, UIMouseEventParameter eventParameter) => ConfirmPanel.ShowModal(t_confirmPanelSetGlobalTitle, t_confirmPanelSetGlobalText, (comp, ret) =>
        {
            if (ret != 1)
            {
                return;
            }
            ushort buildingID = WorldInfoPanel.GetCurrentInstanceID().Building;

            var buildingWorkTime = BuildingWorkTimeManager.GetBuildingWorkTime(buildingID);

            var newGlobalSettings = new BuildingWorkTimeManager.WorkTime
            {
                WorkAtNight = m_workAtNight.isChecked,
                WorkAtWeekands = m_workAtWeekands.isChecked,
                HasExtendedWorkShift = m_hasExtendedWorkShift.isChecked,
                HasContinuousWorkShift = m_hasContinuousWorkShift.isChecked,
                IgnorePolicy = m_ignorePolicy.isChecked,
                WorkShifts = (int)m_workShifts.value
            };

            if (!buildingWorkTime.IsLocked)
            {
                m_settingsStatus.text = t_globalSettingsStatus;
                m_workShiftsCount.text = newGlobalSettings.WorkShifts.ToString();
            }

            UpdateBuildingSettings.CreateGlobalSettings(buildingID, newGlobalSettings);
        });

        private void DeletePrefabSettings(UIComponent c, UIMouseEventParameter eventParameter) => ConfirmPanel.ShowModal(t_confirmPanelDeletePrefabTitle, t_confirmPanelDeletePrefabText, (comp, ret) =>
        {
            if (ret != 1)
            {
                return;
            }

            ushort buildingID = WorldInfoPanel.GetCurrentInstanceID().Building;
            var buildingInfo = Singleton<BuildingManager>.instance.m_buildings.m_buffer[buildingID].Info;

            if (BuildingWorkTimeManager.PrefabExist(buildingInfo))
            {
                foreach (var item in BuildingWorkTimeManager.BuildingsWorkTime)
                {
                    var Info = Singleton<BuildingManager>.instance.m_buildings.m_buffer[item.Key].Info;
                    if (Info.name == buildingInfo.name)
                    {
                        BackToDefault(item.Key, Info);
                    }
                }

                var prefabRecord = BuildingWorkTimeManager.GetPrefab(buildingInfo);

                BuildingWorkTimeManager.RemovePrefab(prefabRecord);
            }
        });

        private void DeleteGlobalSettings(UIComponent c, UIMouseEventParameter eventParameter) => ConfirmPanel.ShowModal(t_confirmPanelDeleteGlobalTitle, t_confirmPanelDeleteGlobalText, (comp, ret) =>
        {
            if (ret != 1)
            {
                return;
            }

            ushort buildingID = WorldInfoPanel.GetCurrentInstanceID().Building;
            var buildingInfo = Singleton<BuildingManager>.instance.m_buildings.m_buffer[buildingID].Info;

            if (BuildingWorkTimeGlobalConfig.Config.GlobalSettingsExist(buildingInfo))
            {
                foreach (var item in BuildingWorkTimeManager.BuildingsWorkTime)
                {
                    var Info = Singleton<BuildingManager>.instance.m_buildings.m_buffer[item.Key].Info;
                    if (Info.name == buildingInfo.name)
                    {
                        BackToDefault(item.Key, Info);
                    }
                }

                var globalRecord = BuildingWorkTimeGlobalConfig.Config.GetGlobalSettings(buildingInfo);

                BuildingWorkTimeGlobalConfig.Config.RemoveGlobalSettings(globalRecord);
            }
        });

        private void BackToDefault(ushort buildingID, BuildingInfo buildingInfo)
        {
            var buildingWorkTimeDefault = BuildingWorkTimeManager.CreateDefaultBuildingWorkTime(buildingID, buildingInfo);

            m_workAtNight.isChecked = buildingWorkTimeDefault.WorkAtNight;
            m_workAtWeekands.isChecked = buildingWorkTimeDefault.WorkAtWeekands;
            m_hasExtendedWorkShift.isChecked = buildingWorkTimeDefault.HasExtendedWorkShift;
            m_hasContinuousWorkShift.isChecked = buildingWorkTimeDefault.HasContinuousWorkShift;
            m_ignorePolicy.isChecked = buildingWorkTimeDefault.IgnorePolicy;
            m_workShifts.value = buildingWorkTimeDefault.WorkShifts;

            UpdateBuildingSettings.UpdateBuildingToDefaultSettings(buildingID, buildingWorkTimeDefault);

            RefreshData(buildingID, buildingWorkTimeDefault);
        }

    }
}
