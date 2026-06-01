namespace RealTime.UI
{
    using System;
    using System.Collections.Generic;
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
        // ── Sub-panels ──────────────────────────────────────────────────
        private UIPanel m_headerRow;          // title + status + lock btn
        private UIPanel m_daysRow;            // 7 day toggle buttons
        private UIPanel m_shiftsSummaryRow;   // read-only shift summary labels
        private UIPanel m_actionRow;          // Save / Return / Apply buttons
        private UIPanel m_dangerRow;          // Set/Delete Prefab/Global buttons

        private UILabel m_activeDaysLabel;

        // ── Day buttons (array, not 7 fields) ───────────────────────────
        private readonly bool[] m_activeDays = new bool[7];

        // ── Day buttons (array, not 7 fields) ───────────────────────────
        private static readonly DayOfWeek[] DayOrder =
        [
            DayOfWeek.Sunday, DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday,
            DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday
        ];

        private UIButton[] m_dayButtons; // indexed by DayOrder position

        // ── Shift rows (array of row structs) ───────────────────────────
        private ShiftRow[] m_shiftRows;  // up to MaxShifts = 5
        private UIButton m_addShiftBtn;

        // ── Header controls ─────────────────────────────────────────────
        private UILabel m_settingsTitle;
        private UILabel m_settingsStatus;
        private UIPanel m_innerPanel;
        internal UIButton m_unlockSettingsBtn;
        internal UIButton m_lockUnlockChangesBtn;

        // ── Action buttons ───────────────────────────────────────────────
        internal UIButton m_saveBuildingSettingsBtn;
        internal UIButton m_returnToDefaultBtn;
        internal UIButton m_applyPrefabSettingsBtn;
        internal UIButton m_applyGlobalSettingsBtn;
        internal UIButton m_setPrefabSettingsBtn;
        internal UIButton m_setGlobalSettingsBtn;
        internal UIButton m_deletePrefabSettingsBtn;
        internal UIButton m_deleteGlobalSettingsBtn;

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

        private static readonly string[] DayTranslationKeys =
        {
            TranslationKeys.Sunday,
            TranslationKeys.Monday,
            TranslationKeys.Tuesday,
            TranslationKeys.Wednesday,
            TranslationKeys.Thursday,
            TranslationKeys.Friday,
            TranslationKeys.Saturday
        };

        // ────────────────────────────────────────────────────────────────
        //  Nested type: one shift row's worth of controls
        // ────────────────────────────────────────────────────────────────
        private sealed class ShiftRow
        {
            public UIPanel Panel;
            public UILabel IndexLabel;   // "Shift 1", "Shift 2", etc.
            public UITextField StartField;  // "08:00"
            public UITextField EndField;    // "17:00"
            public UIButton RemoveBtn;    // ×

            public bool IsVisible
            {
                get => Panel.isVisible;
                set => Panel.isVisible = value;
            }

            public BuildingWorkTimeManager.WorkShiftTime GetEntry() => new()
            {
                StartHour = ParseHour(StartField.text),
                EndHour = ParseHour(EndField.text),
            };

            public void SetEntry(int index, BuildingWorkTimeManager.WorkShiftTime entry)
            {
                IndexLabel.text = $"Shift {index + 1}";
                StartField.text = FormatHour(entry.StartHour);
                EndField.text = FormatHour(entry.EndHour);
                IsVisible = entry.IsValid;
            }

            private static float ParseHour(string s) => float.TryParse(s, out float h) ? h : 0f;
            private static string FormatHour(float h) => $"{(int)h:D2}:{(int)(h % 1 * 60):D2}";
        }

        public override void Awake()
        {
            base.Awake();

            name = "OperationHoursUIPanel";
            backgroundSprite = "SubcategoriesPanel";
            opacity = 0.90f;
            isVisible = false;
            height = 470f;
            width = 510f;

            CreateHeader();
            CreateDaysRow();
            CreateShiftRows();
            CreateActionButtons();
            CreateDangerButtons();

            SetAllControlsToDisabled();
        }

        private void CreateHeader()
        {
            // ----------------------------------  settings ----------------------------------
            m_settingsTitle = UILabels.CreateLabel(this, "SettingsTitle", "", "");
            m_settingsTitle.font = UIFonts.GetUIFont("OpenSans-Regular");
            m_settingsTitle.textAlignment = UIHorizontalAlignment.Center;
            m_settingsTitle.textColor = new Color32(78, 184, 126, 255);
            m_settingsTitle.relativePosition = new Vector3(130f, 20f);
            m_settingsTitle.textScale = 1.2f;


            // status label that shows if the current settings are default, building specific, prefab specific or global specific
            m_settingsStatus = UILabels.CreateLabel(this, "SettingsStatus", "", "");
            m_settingsStatus.font = UIFonts.GetUIFont("OpenSans-Regular");
            m_settingsStatus.textAlignment = UIHorizontalAlignment.Center;
            m_settingsStatus.textColor = new Color32(240, 190, 199, 255);
            m_settingsStatus.relativePosition = new Vector3(110f, 55f);
            m_settingsStatus.textScale = 0.9f;


            // ----------------------------------  settings container ----------------------------------
            m_innerPanel = UIPanels.CreatePanel(this, "OperationHoursInnerPanel");
            m_innerPanel.backgroundSprite = "GenericPanelLight";
            m_innerPanel.color = new Color32(206, 206, 206, 255);
            m_innerPanel.size = new Vector2(235f, 66f);
            m_innerPanel.relativePosition = new Vector3(15f, 322f);


            // unlock settings button
            m_unlockSettingsBtn = UIButtons.CreateButton(this, 0f, 20f, "UnlockSettings", "", "", 100f);
            m_unlockSettingsBtn.eventClicked += UnlockSettings;


            // lock/unlock changes button
            m_lockUnlockChangesBtn = UIButtons.CreateButton(this, 250f, 20f, "LockUnLockChanges", "", "", 32, 32);
            m_lockUnlockChangesBtn.atlas = TextureUtils.GetAtlas("LockButtonAtlas");
            m_lockUnlockChangesBtn.normalFgSprite = "UnLock";
            m_lockUnlockChangesBtn.disabledFgSprite = "UnLock";
            m_lockUnlockChangesBtn.focusedFgSprite = "UnLock";
            m_lockUnlockChangesBtn.hoveredFgSprite = "UnLock";
            m_lockUnlockChangesBtn.pressedFgSprite = "UnLock";
            m_lockUnlockChangesBtn.eventClicked += LockUnlockChanges;
        }

        private void CreateDaysRow()
        {
            m_dayButtons = new UIButton[7];
            for (int i = 0; i < DayOrder.Length; i++)
            {
                int idx = i; // capture for lambda
                var day = DayOrder[i];
                var btn = UIButtons.CreateButton(this, 10f + i * 70f, 120f, DayTranslationKeys[i].ToString(), "", "", 60f);

                btn.normalBgSprite = "ButtonMenu";
                btn.pressedBgSprite = "ButtonMenuFocused";
                btn.focusedBgSprite = "ButtonMenuFocused";
                btn.color = new Color32(100, 110, 140, 255);
                btn.textColor = new Color32(80, 88, 120, 255);

                btn.eventClicked += (c, _) =>
                {
                    m_activeDays[idx] = !m_activeDays[idx];
                    ApplyToggleVisual((UIButton)c, m_activeDays[idx]);
                    ((UIButton)c).stringUserData = m_activeDays[idx] ? "1" : "0";
                };

                m_dayButtons[idx] = btn;
            }
        }

        private void CreateShiftRows()
        {
            m_shiftRows = new ShiftRow[5];
            for (int i = 0; i < 5; i++)
            {
                float y = 180f + i * 36f;
                var row = new ShiftRow
                {
                    Panel = UIPanels.CreatePanel(this, $"ShiftRow_{i}")
                };
                row.Panel.size = new Vector2(480f, 30f);
                row.Panel.relativePosition = new Vector3(15f, y);

                row.IndexLabel = UILabels.CreateLabel(row.Panel, $"ShiftLabel_{i}", "", "");
                row.StartField = /* UITextField creation */ null;
                row.EndField = /* UITextField creation */ null;
                row.RemoveBtn = UIButtons.CreateButton(row.Panel, 440f, 0f, $"RemoveShift_{i}", "×", "", 28f);

                int captured = i;
                row.RemoveBtn.eventClicked += (_, __) => RemoveShift(captured);

                row.IsVisible = false;  // hidden until AddShift is clicked
                m_shiftRows[i] = row;
            }

            m_addShiftBtn = UIButtons.CreateButton(this, 15f, 180f + 5 * 36f, "AddShift", "+ Add Shift", "", 480f);
            m_addShiftBtn.eventClicked += (_, __) => AddShift();
        }

        /* Save, ReturnToDefault, ApplyPrefab, ApplyGlobal */
        private void CreateActionButtons()
        {
            m_saveBuildingSettingsBtn = UIButtons.CreateButton(this, 260f, 120f, "SaveBuildingSettings", "", "", 460f);
            m_saveBuildingSettingsBtn.eventClicked += SaveBuildingSettings;

            m_returnToDefaultBtn = UIButtons.CreateButton(this, 0f, 320f, "ReturnToDefault", "", "", 100f);
            m_returnToDefaultBtn.eventClicked += ReturnToDefault;

            m_applyPrefabSettingsBtn = UIButtons.CreateButton(this, 110f, 320f, "ApplyPrefabSettings", "", "", 100f);
            m_applyPrefabSettingsBtn.eventClicked += ApplyPrefabSettings;

            m_applyGlobalSettingsBtn = UIButtons.CreateButton(this, 220f, 320f, "ApplyGlobalSettings", "", "", 100f);
            m_applyGlobalSettingsBtn.eventClicked += ApplyGlobalSettings;
        }

        /* SetPrefab, SetGlobal, DeletePrefab, DeleteGlobal */
        private void CreateDangerButtons()
        {
            m_setPrefabSettingsBtn = UIButtons.CreateButton(this, 0f, 520f, "SetPrefabSettings", "", "");
            m_setPrefabSettingsBtn.eventClicked += SetPrefabSettings;

            m_setGlobalSettingsBtn = UIButtons.CreateButton(this, 240f, 520f, "SetGlobalSettings", "", "");
            m_setGlobalSettingsBtn.eventClicked += SetGlobalSettings;

            m_deletePrefabSettingsBtn = UIButtons.CreateButton(this, 0f, 720f, "DeletePrefabSettings", "", "");
            m_deletePrefabSettingsBtn.eventClicked += DeletePrefabSettings;

            m_deleteGlobalSettingsBtn = UIButtons.CreateButton(this, 240f, 720f, "DeleteGlobalSettings", "", "");
            m_deleteGlobalSettingsBtn.eventClicked += DeleteGlobalSettings;
        }

        public void UpdateData(float panelHeight, ILocalizationProvider localizationProvider)
        {
            Translate(localizationProvider);
            relativePosition = new Vector3(parent.width + 1, panelHeight);
        }

        private void Translate(ILocalizationProvider localizationProvider)
        {
            m_settingsTitle.text = localizationProvider.Translate(TranslationKeys.SettingsTitle);

            t_defaultSettingsStatus = localizationProvider.Translate(TranslationKeys.DefaultSettingsStatus);
            t_buildingSettingsStatus = localizationProvider.Translate(TranslationKeys.BuildingSettingsStatus);
            t_prefabSettingsStatus = localizationProvider.Translate(TranslationKeys.PrefabSettingsStatus);
            t_globalSettingsStatus = localizationProvider.Translate(TranslationKeys.GlobalSettingsStatus);

            m_unlockSettingsBtn.text = localizationProvider.Translate(TranslationKeys.UnlockSettings);
            m_unlockSettingsBtn.tooltip = localizationProvider.Translate(TranslationKeys.UnlockSettingsTooltip);
            m_lockUnlockChangesBtn.tooltip = localizationProvider.Translate(TranslationKeys.LockUnlockChangesTooltip);

            m_activeDaysLabel.text = localizationProvider.Translate(TranslationKeys.ActiveDays);

            foreach (var btn in m_dayButtons)
            {
                btn.text = localizationProvider.Translate(btn.name);
                btn.tooltip = localizationProvider.Translate(btn.name + "Tooltip");
            }

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
            foreach (var btn in m_dayButtons)
            {
                btn.Enable();
            }

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

        public void RefreshData(ushort buildingID, BuildingWorkTimeManager.WorkTime buildingWorkTime)
        {
            var buildingInfo = Singleton<BuildingManager>.instance.m_buildings.m_buffer[buildingID].Info;

            if (!buildingWorkTime.IsPrefab && !buildingWorkTime.IsGlobal)
            {
                m_settingsStatus.text = buildingWorkTime.IsDefault ? t_defaultSettingsStatus : t_buildingSettingsStatus;
                SetActiveDays(buildingWorkTime.WorkDays);
            }
            else if (BuildingWorkTimeManager.PrefabExist(buildingInfo) && buildingWorkTime.IsPrefab && !buildingWorkTime.IsLocked)
            {
                var buildingWorkTimePrefab = BuildingWorkTimeManager.GetPrefab(buildingInfo);

                m_settingsStatus.text = t_prefabSettingsStatus;
                SetActiveDays(buildingWorkTimePrefab.WorkDays);
            }
            else if(BuildingWorkTimeGlobalConfig.Config.GlobalSettingsExist(buildingInfo) && buildingWorkTime.IsGlobal && !buildingWorkTime.IsLocked)
            {
                var buildingWorkTimeGlobal = BuildingWorkTimeGlobalConfig.Config.GetGlobalSettings(buildingInfo);

                m_settingsStatus.text = t_globalSettingsStatus;
                SetActiveDays(buildingWorkTimeGlobal.WorkDays);
            }

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

        private void SetActiveDays(DayOfWeek[] workDays)
        {
            // reset all first
            for (int i = 0; i < m_activeDays.Length; i++)
            {
                m_activeDays[i] = false;
            }

            foreach (var day in workDays)
            {
                int idx = Array.IndexOf(DayOrder, day);
                if (idx >= 0)
                {
                    m_activeDays[idx] = true;
                }
            }

            // sync button visuals
            for (int i = 0; i < m_dayButtons.Length; i++)
            {
                m_dayButtons[i].stringUserData = m_activeDays[i] ? "1" : "0";
                ApplyToggleVisual(m_dayButtons[i], m_activeDays[i]);
            }
        }

        private DayOfWeek[] GetActiveDays()
        {
            var list = new List<DayOfWeek>();
            for (int i = 0; i < m_activeDays.Length; i++)
            {
                if (m_activeDays[i])
                {
                    list.Add(DayOrder[i]);
                }
            }

            return [.. list];
        }

        // ────────────────────────────────────────────────────────────────
        //  Shift row helpers
        // ────────────────────────────────────────────────────────────────
        private void AddShift()
        {
            for (int i = 0; i < m_shiftRows.Length; i++)
            {
                if (!m_shiftRows[i].IsVisible)
                {
                    m_shiftRows[i].SetEntry(i, new BuildingWorkTimeManager.WorkShiftTime { StartHour = 8f, EndHour = 17f });
                    m_shiftRows[i].IsVisible = true;
                    m_addShiftBtn.isVisible = i < 4;
                    return;
                }
            }
        }

        private void RemoveShift(int index)
        {
            m_shiftRows[index].IsVisible = false;
            m_addShiftBtn.isVisible = true;
            // compact: shift rows above index stay, just hide this one
        }

        private void SetAllControlsToDisabled()
        {
            foreach (var btn in m_dayButtons)
            {
                btn.Disable();
            }

            // action/danger buttons follow their own conditional logic
            m_saveBuildingSettingsBtn.Disable();
            m_returnToDefaultBtn.Disable();
            m_applyPrefabSettingsBtn.Disable();
            m_applyGlobalSettingsBtn.Disable();
            m_setPrefabSettingsBtn.Disable();
            m_setGlobalSettingsBtn.Disable();
            m_deletePrefabSettingsBtn.Disable();
            m_deleteGlobalSettingsBtn.Disable();
        }

        private void ApplyToggleVisual(UIButton btn, bool active)
        {
            if (active)
            {
                btn.normalBgSprite = "ButtonMenuFocused";
                btn.color = new Color32(255, 255, 255, 255);
                btn.textColor = new Color32(110, 203, 216, 255); // teal
            }
            else
            {
                btn.normalBgSprite = "ButtonMenu";
                btn.color = new Color32(100, 110, 140, 255);
                btn.textColor = new Color32(80, 88, 120, 255);
            }
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
                WorkDays = GetActiveDays(),
                WorkShifts = [],
                IsLocked = is_locked,
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
                m_workAtNight.isChecked = prefabRecord.WorkDays;
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
