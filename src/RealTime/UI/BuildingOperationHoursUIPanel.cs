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
        // ─────────────────────────────────────────── Sub-panels ────────────────────────────────────────────
        internal UIPanel m_headerRow;          // title + status + lock btn
        internal UIPanel m_daysRow;            // 7 day toggle buttons
        internal UIPanel m_shiftsSummaryRow;   // read-only shift summary labels
        internal UIPanel m_shiftsEditorPanel; // shift editor rows + add shift button
        internal UIPanel m_actionRow;          // Save / Return / Apply buttons
        internal UIPanel m_advancedSettingsPanel;          // Set/Delete Prefab/Global buttons
        internal UIButton m_accessAdvancedSettingsBtn; // opens the advanced settings panel (which contains the danger buttons on the right side of the main panel)
        internal UIPanel m_dangerRow; // Set Prefab/Global and Delete Prefab/Global buttons

        // ─────────────────────────────────────────── Header controls ──────────────────────────────────────
        internal UILabel m_settingsTitle;
        internal UILabel m_settingsStatusLabel;
        internal UIPanel m_settingsStatusContainer;
        internal UILabel m_settingsStatus;
        internal UIButton m_settingsCopyBtn;
        internal UIButton m_settingsPasteBtn;
        internal UIPanel m_innerPanel;
        internal UIButton m_editSettingsBtn;
        internal UIButton m_lockUnlockChangesBtn;
        internal bool m_editMode;

        // ─────────────────────────────────────────── Days Panel ───────────────────────────────────────────
        internal UILabel m_activeDaysLabel; // Active days Label
        internal readonly bool[] m_activeDays = new bool[7]; // Day buttons (array, not 7 fields)
        internal UIButton[] m_dayButtons; // indexed by DayOrder position
        internal static readonly DayOfWeek[] DayOrder = // Define the order of days for consistent indexing
        [
            DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday,
            DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday
        ];

        // ─────────────────────────────────────────── Shifts Summary Panel ───────────────────────────────────────────
        internal UILabel m_shiftsSummaryLabel;
        internal UIPanel m_shiftsSummaryContainer; // container for shift summary rows  
        internal ShiftSummaryRow[] m_shiftSummaryRows; // up to MaxShifts = 5
        internal UIButton m_shiftsEditBtn; // opens the shifts editor panel

        // ─────────────────────────────────────────── Shifts Editor Panel ───────────────────────────────────────────
        internal UILabel m_shiftsEditorLabel;
        internal ShiftRow[] m_shiftEditRows;  // up to MaxShifts = 5
        internal UIButton m_addShiftBtn;
        internal UICheckBox m_ignorePolicy;

        // ─────────────────────────────────────────── Action buttons ───────────────────────────────────────────────
        internal UIButton m_saveBuildingSettingsBtn;
        internal UIButton m_returnToDefaultBtn;
        internal UIButton m_applyPrefabSettingsBtn;
        internal UIButton m_applyGlobalSettingsBtn;

        // ─────────────────────────────────────────── Danger buttons ───────────────────────────────────────────────
        internal UIButton m_setPrefabSettingsBtn;
        internal UIButton m_setGlobalSettingsBtn;
        internal UIButton m_deletePrefabSettingsBtn;
        internal UIButton m_deleteGlobalSettingsBtn;

        // ─────────────────────────────────────────── Translations ───────────────────────────────────────────────
        internal string t_shiftLabelIndex;

        internal string t_defaultSettingsStatus;
        internal string t_buildingSettingsStatus;
        internal string t_prefabSettingsStatus;
        internal string t_globalSettingsStatus;
        internal string t_confirmPanelSetPrefabTitle;
        internal string t_confirmPanelSetPrefabText;
        internal string t_confirmPanelSetGlobalTitle;
        internal string t_confirmPanelSetGlobalText;
        internal string t_confirmPanelDeletePrefabTitle;
        internal string t_confirmPanelDeletePrefabText;
        internal string t_confirmPanelDeleteGlobalTitle;
        internal string t_confirmPanelDeleteGlobalText;

        internal string t_invalidShiftsTitle;
        internal string t_invalidShiftsText;

        internal static readonly string[] DayTranslationKeys =
        [
            TranslationKeys.Monday,
            TranslationKeys.Tuesday,
            TranslationKeys.Wednesday,
            TranslationKeys.Thursday,
            TranslationKeys.Friday,
            TranslationKeys.Saturday,
            TranslationKeys.Sunday
        ];

        private bool m_isCopied = false;

        private List<BuildingWorkTimeManager.WorkShiftTime> m_shiftsClipboard;

        private List<DayOfWeek> m_daysOfWeekClipboard;

        private bool m_ignorePolicyClipboard;

        private bool m_isLockedClipboard;

        internal class ShiftSummaryRow
        {
            public UIPanel Panel;
            public UILabel IndexLabel;   // "Shift 1", "Shift 2", etc.
            public UILabel StartField;  // "08:00"
            public UILabel Arrow;       // ->
            public UILabel EndField;    // "17:00"

            public bool IsActive;

            public BuildingWorkTimeManager.WorkShiftTime GetEntry() => new()
            {
                StartTime = ParseHour(StartField.text),
                EndTime = ParseHour(EndField.text) 
            };

            public void SetEntry(BuildingWorkTimeManager.WorkShiftTime entry)
            {
                StartField.text = FormatHour(entry.StartTime);
                EndField.text = FormatHour(entry.EndTime);
                IsActive = true;
            }
        }

        internal class ShiftRow
        {
            public UIPanel Panel;
            public UILabel IndexLabel;   // "Shift 1", "Shift 2", etc.
            public UITextField StartHourField;  // "08"
            public UILabel StartColon; // :
            public UITextField StartMinuteField; // "00"
            public UILabel Arrow;       // ->
            public UITextField EndHourField; // "17"
            public UILabel EndColon; // :
            public UITextField EndMinuteField; // "00"
            public UIButton RemoveBtn;    // ×

            public bool IsActive;

            public BuildingWorkTimeManager.WorkShiftTime GetEntry() => new()
            {
                StartTime = int.Parse(StartHourField.text) + int.Parse(StartMinuteField.text) / 60f,
                EndTime = int.Parse(EndHourField.text) + int.Parse(EndMinuteField.text) / 60f
            };

            public void SetEntry(BuildingWorkTimeManager.WorkShiftTime entry)
            {
                int startHour = (int)entry.StartTime;
                int startMinute = (int)(entry.StartTime % 1f * 60f);
                StartHourField.text = startHour.ToString("D2");
                StartMinuteField.text = startMinute.ToString("D2");

                int endHour = (int)entry.EndTime;
                int endMinute = (int)(entry.EndTime % 1f * 60f);
                EndHourField.text = endHour.ToString("D2");
                EndMinuteField.text = endMinute.ToString("D2");

                IsActive = true;
            }
        }

        public override void Awake()
        {
            base.Awake();

            name = "OperationHoursUIPanel";
            backgroundSprite = "SubcategoriesPanel";
            opacity = 0.90f;
            isVisible = false;
            width = 300f;
            height = 500f;
            
            m_innerPanel = UIPanels.CreatePanel(this, "OperationHoursInnerPanel");
            m_innerPanel.color = new Color32(206, 206, 206, 255);
            m_innerPanel.size = new Vector2(300f, 480f);
            m_innerPanel.relativePosition = new Vector3(5f, 5f);

            m_headerRow = UIPanels.CreateRow(m_innerPanel, "HeaderRow", 10f, 80f);
            m_daysRow = UIPanels.CreateRow(m_innerPanel, "DaysRow", 100f, 90f);
            m_shiftsSummaryRow = UIPanels.CreateRow(m_innerPanel, "ShiftsSummaryRow", 210f, 30f);

            m_shiftsEditorPanel = UIPanels.CreatePanel(this, "ShiftsEditorPanel");
            m_shiftsEditorPanel.backgroundSprite = "SubcategoriesPanel";
            m_shiftsEditorPanel.size = new Vector2(300f, 300f);
            m_shiftsEditorPanel.relativePosition = new Vector3(302f, 75f);
            m_shiftsEditorPanel.isVisible = false;

            m_actionRow = UIPanels.CreateRow(m_innerPanel, "ActionRow", 380f, 80f);

            m_accessAdvancedSettingsBtn = UIButtons.CreateButton(m_innerPanel, 20f, 500f, "AccessAdvancedSettings", "", "", 260f);
            m_accessAdvancedSettingsBtn.eventClicked += OpenAdvancedSettingsPanel;

            m_advancedSettingsPanel = UIPanels.CreatePanel(this, "AdvancedSettings");
            m_advancedSettingsPanel.backgroundSprite = "SubcategoriesPanel";
            m_advancedSettingsPanel.size = new Vector2(300f, 80f);
            m_advancedSettingsPanel.relativePosition = new Vector3(302f, 380f);
            m_advancedSettingsPanel.isVisible = false;

            m_dangerRow = UIPanels.CreateRow(m_advancedSettingsPanel, "DangerRow", 0f, 80f);

            m_shiftsClipboard = [];
            m_daysOfWeekClipboard = [];
            m_ignorePolicyClipboard = false;
            m_isLockedClipboard = false;

            CreateHeader();
            CreateDaysRow();
            CreateShiftSummaryRows();
            CreateShiftEditRows();
            CreateActionButtons();
            CreateDangerButtons();

            SetAllControlsToDisabled();
        }

        public override void Update() => m_settingsPasteBtn.isEnabled = m_isCopied;

        public void RefreshData(ushort buildingID, BuildingWorkTimeManager.WorkTime buildingWorkTime)
        {
            var buildingInfo = Singleton<BuildingManager>.instance.m_buildings.m_buffer[buildingID].Info;

            if (!buildingWorkTime.IsPrefab && !buildingWorkTime.IsGlobal)
            {
                m_settingsStatus.text = buildingWorkTime.IsDefault ? t_defaultSettingsStatus : t_buildingSettingsStatus;
                SetActiveDays(buildingWorkTime.WorkDays);
                SetActiveShifts(buildingWorkTime.WorkShifts);
            }
            else if (BuildingWorkTimeManager.PrefabExist(buildingInfo) && buildingWorkTime.IsPrefab && !buildingWorkTime.IsLocked)
            {
                var buildingWorkTimePrefab = BuildingWorkTimeManager.GetPrefab(buildingInfo);

                m_settingsStatus.text = t_prefabSettingsStatus;
                SetActiveDays(buildingWorkTimePrefab.WorkDays);
                SetActiveShifts(buildingWorkTimePrefab.WorkShifts);
            }
            else if (BuildingWorkTimeGlobalConfig.Config.GlobalSettingsExist(buildingInfo) && buildingWorkTime.IsGlobal && !buildingWorkTime.IsLocked)
            {
                var buildingWorkTimeGlobal = BuildingWorkTimeGlobalConfig.Config.GetGlobalSettings(buildingInfo);

                m_settingsStatus.text = t_globalSettingsStatus;
                SetActiveDays(buildingWorkTimeGlobal.WorkDays);
                SetActiveShifts(buildingWorkTimeGlobal.WorkShiftTimes);
            }

            string spriteName = buildingWorkTime.IsLocked ? "Lock" : "UnLock";

            m_lockUnlockChangesBtn.normalFgSprite = spriteName;
            m_lockUnlockChangesBtn.disabledFgSprite = spriteName;
            m_lockUnlockChangesBtn.focusedFgSprite = spriteName;
            m_lockUnlockChangesBtn.hoveredFgSprite = spriteName;
            m_lockUnlockChangesBtn.pressedFgSprite = spriteName;

            m_lockUnlockChangesBtn.size = buildingWorkTime.IsLocked ? new Vector2(20f, 32f) : new Vector2(27f, 32f);

            m_lockUnlockChangesBtn.Invalidate();
            m_lockUnlockChangesBtn.parent.Invalidate();
        }

        private void CreateHeader()
        {
            // ----------------------------------  settings ----------------------------------
            m_settingsTitle = UILabels.CreateLabel(m_headerRow, "SettingsTitle", "", "");
            m_settingsTitle.font = UIFonts.GetUIFont("OpenSans-Regular");
            m_settingsTitle.textAlignment = UIHorizontalAlignment.Left;
            m_settingsTitle.textColor = new Color32(255, 255, 255, 255);
            m_settingsTitle.relativePosition = new Vector3(0f, 4f);
            m_settingsTitle.width = 220f;
            m_settingsTitle.textScale = 1.3f;

            // status label header
            m_settingsStatusLabel = UILabels.CreateLabel(m_headerRow, "SettingsStatusLabel", "", "");
            m_settingsStatusLabel.font = UIFonts.GetUIFont("OpenSans-Regular");
            m_settingsStatusLabel.textAlignment = UIHorizontalAlignment.Left;
            m_settingsStatusLabel.textColor = new Color32(255, 255, 255, 255);
            m_settingsStatusLabel.relativePosition = new Vector3(0f, 50f);
            m_settingsStatusLabel.width = 100f;
            m_settingsStatusLabel.textScale = 1.1f;

            m_settingsStatusContainer = UIPanels.CreatePanel(m_headerRow, "ShiftsSummaryContainer");
            m_settingsStatusContainer.backgroundSprite = "GenericPanelLight";
            m_settingsStatusContainer.size = new Vector2(170f, 30f);
            m_settingsStatusContainer.relativePosition = new Vector3(60f, 45f);

            // status label that shows if the current settings are default, building specific, prefab specific or global specific
            m_settingsStatus = UILabels.CreateLabel(m_settingsStatusContainer, "SettingsStatus", "", "");
            m_settingsStatus.font = UIFonts.GetUIFont("OpenSans-Regular");
            m_settingsStatus.textAlignment = UIHorizontalAlignment.Left;
            m_settingsStatus.textColor = new Color32(0, 0, 0, 255);
            m_settingsStatus.relativePosition = new Vector3(20f, 5f);
            m_settingsStatus.width = 220f;
            m_settingsStatus.textScale = 1.1f;
            m_settingsStatus.font.lineHeight = 22;

            // edit settings button
            m_editSettingsBtn = UIButtons.CreateButton(m_headerRow, 245f, 50f, "EditSettings", "", "", 23f, 24f);
            m_editSettingsBtn.atlas = TextureUtils.GetAtlas("EditButtonAtlas");
            m_editSettingsBtn.normalFgSprite = "Edit";
            m_editSettingsBtn.disabledFgSprite = "Edit";
            m_editSettingsBtn.focusedFgSprite = "Edit";
            m_editSettingsBtn.hoveredFgSprite = "Edit";
            m_editSettingsBtn.pressedFgSprite = "Edit";
            m_editSettingsBtn.eventClicked += EditSettings;

            m_editMode = false;

            // lock/unlock changes button
            m_lockUnlockChangesBtn = UIButtons.CreateButton(m_headerRow, 250f, 0f, "LockUnLockChanges", "", "", 27f, 32f);
            m_lockUnlockChangesBtn.atlas = TextureUtils.GetAtlas("LockButtonAtlas");
            m_lockUnlockChangesBtn.normalFgSprite = "UnLock";
            m_lockUnlockChangesBtn.disabledFgSprite = "UnLock";
            m_lockUnlockChangesBtn.focusedFgSprite = "UnLock";
            m_lockUnlockChangesBtn.hoveredFgSprite = "UnLock";
            m_lockUnlockChangesBtn.pressedFgSprite = "UnLock";
            m_lockUnlockChangesBtn.eventClicked += LockUnlockChanges;

            m_settingsCopyBtn = UIButtons.CreateButton(m_headerRow, 165f, 3f, "CopySettings", "", "", 32f, 32f);
            m_settingsCopyBtn.atlas = TextureUtils.GetAtlas("CopyPasteAtlas");
            m_settingsCopyBtn.normalFgSprite = "Copy";
            m_settingsCopyBtn.foregroundSpriteMode = UIForegroundSpriteMode.Scale;
            m_settingsCopyBtn.normalBgSprite = "ButtonMenu";
            m_settingsCopyBtn.disabledBgSprite = "ButtonMenuDisabled";
            m_settingsCopyBtn.hoveredBgSprite = "ButtonMenuHovered";
            m_settingsCopyBtn.focusedBgSprite = "ButtonMenuHovered";
            m_settingsCopyBtn.pressedBgSprite = "ButtonMenuHovered";
            m_settingsCopyBtn.eventClicked += CopySettings;

            m_settingsPasteBtn = UIButtons.CreateButton(m_headerRow, 205f, 3f, "PasteSettings", "", "", 32f, 32f);
            m_settingsPasteBtn.atlas = TextureUtils.GetAtlas("CopyPasteAtlas");
            m_settingsPasteBtn.normalFgSprite = "Paste";
            m_settingsPasteBtn.foregroundSpriteMode = UIForegroundSpriteMode.Scale;
            m_settingsPasteBtn.normalBgSprite = "ButtonMenu";
            m_settingsPasteBtn.disabledBgSprite = "ButtonMenuDisabled";
            m_settingsPasteBtn.hoveredBgSprite = "ButtonMenuHovered";
            m_settingsPasteBtn.focusedBgSprite = "ButtonMenuHovered";
            m_settingsPasteBtn.pressedBgSprite = "ButtonMenuHovered";
            m_settingsPasteBtn.eventClicked += PasteSettings;
        }

        private void CreateDaysRow()
        {
            m_activeDaysLabel = UILabels.CreateLabel(m_daysRow, "ActiveDays", "", "");
            m_activeDaysLabel.font = UIFonts.GetUIFont("OpenSans-Regular");
            m_activeDaysLabel.textAlignment = UIHorizontalAlignment.Left;
            m_activeDaysLabel.textColor = new Color32(255, 255, 255, 255);
            m_activeDaysLabel.relativePosition = new Vector3(5f, 6f);
            m_activeDaysLabel.textScale = 1.1f;

            m_dayButtons = new UIButton[DayOrder.Length];
            int half = DayOrder.Length / 2;

            for (int i = 0; i < DayOrder.Length; i++)
            {
                int idx = i; // capture for lambda
                var day = DayOrder[i];
                var btn = new UIButton();
                if (i <= half)
                {
                    // first 4 buttons on the first row
                    btn = UIButtons.CreateButton(m_daysRow, 10 + i * 64f, 30f, day.ToString(), string.Empty, string.Empty, 60f);
                }
                else
                {
                    // remaining 3 buttons on the second row, centered under the first row
                    btn = UIButtons.CreateButton(m_daysRow, 43f + (i - (half + 1)) * 64f, 70f, day.ToString(), string.Empty, string.Empty, 60f);
                }

                btn.normalBgSprite = "ButtonMenu";
                btn.hoveredBgSprite = "ButtonMenuHovered";
                btn.pressedBgSprite = "ButtonMenuFocused";
                btn.focusedBgSprite = "ButtonMenuFocused";
                btn.disabledBgSprite = "ButtonMenu";

                btn.color = new Color32(100, 110, 140, 255);
                btn.textColor = new Color32(80, 88, 120, 255);
                btn.textScale = 0.75f;
                btn.stringUserData = "0";

                btn.eventClicked += (c, _) =>
                {
                    var button = (UIButton)c;
                    m_activeDays[idx] = !m_activeDays[idx];
                    ApplyToggleVisual(button, m_activeDays[idx]);
                    button.stringUserData = m_activeDays[idx] ? "1" : "0";
                    button.Unfocus();
                };

                m_dayButtons[idx] = btn;
            }
        }

        private void CreateShiftSummaryRows()
        {
            m_shiftsSummaryLabel = UILabels.CreateLabel(m_shiftsSummaryRow, "ShiftsSummaryLabel", "", "");
            m_shiftsSummaryLabel.font = UIFonts.GetUIFont("OpenSans-Regular");
            m_shiftsSummaryLabel.textAlignment = UIHorizontalAlignment.Left;
            m_shiftsSummaryLabel.textColor = new Color32(206, 206, 206, 255);
            m_shiftsSummaryLabel.relativePosition = new Vector3(5f, 6f);
            m_shiftsSummaryLabel.textScale = 1.1f;

            m_shiftsSummaryContainer = UIPanels.CreatePanel(m_shiftsSummaryRow, "ShiftsSummaryContainer");
            m_shiftsSummaryContainer.backgroundSprite = "GenericPanelDark";
            m_shiftsSummaryContainer.size = new Vector2(260f, 150f);
            m_shiftsSummaryContainer.relativePosition = new Vector3(10f, 30f);

            m_shiftSummaryRows = new ShiftSummaryRow[5];

            for (int i = 0; i < 5; i++)
            {
                float y = 5 + i * 36f;
                var row = new ShiftSummaryRow
                {
                    Panel = UIPanels.CreatePanel(m_shiftsSummaryContainer, $"ShiftRow_{i + 1}")
                };
                row.Panel.size = new Vector2(220f, 30f);
                row.Panel.relativePosition = new Vector3(10f, y);

                row.IndexLabel = UILabels.CreateLabel(row.Panel, $"ShiftSummaryLabel_{i + 1}", "", "");
                row.IndexLabel.relativePosition = new Vector3(15f, 6f);
                row.IndexLabel.width = 55f;

                row.StartField = UILabels.CreateLabel(row.Panel, $"ShiftSummaryStart_{i + 1}", "", "");
                row.StartField.size = new Vector2(70f, 24f);
                row.StartField.relativePosition = new Vector3(80f, 6f);

                row.Arrow = UILabels.CreateLabel(row.Panel, $"ShiftSummaryArrow_{i + 1}", "->", "");
                row.Arrow.size = new Vector2(70f, 24f);
                row.Arrow.relativePosition = new Vector3(125f, 6f);

                row.EndField = UILabels.CreateLabel(row.Panel, $"ShiftSummaryEnd_{i + 1}", "", "");
                row.EndField.size = new Vector2(70f, 24f);
                row.EndField.relativePosition = new Vector3(145f, 6f);

                row.IsActive = false;
                row.Panel.isVisible = false;
                m_shiftSummaryRows[i] = row;
            }

            m_shiftsEditBtn = UIButtons.CreateButton(m_shiftsSummaryRow, 10f, 190f, "EditShifts", "", "", 260f);
            m_shiftsEditBtn.eventClicked += OpenShiftEditor;
        }

        private void OpenAdvancedSettingsPanel(UIComponent c, UIMouseEventParameter eventParameter) => m_advancedSettingsPanel.isVisible = !m_advancedSettingsPanel.isVisible;

        private void OpenShiftEditor(UIComponent c, UIMouseEventParameter eventParameter) => m_shiftsEditorPanel.isVisible = !m_shiftsEditorPanel.isVisible;

        private void RefreshShiftsSummary()
        {
            foreach (var row in m_shiftSummaryRows)
            {
                row.IsActive = false;
                row.Panel.isVisible = false;
            }

            var shifts = GetActiveShifts();

            if (shifts == null || shifts.Length == 0)
            {
                return;
            }

            for(int i = 0; i < shifts.Length && i < m_shiftSummaryRows.Length; i++)
            {
                m_shiftSummaryRows[i].SetEntry(shifts[i]);
                m_shiftSummaryRows[i].Panel.isVisible = true;
                int row_index = i + 1;
                m_shiftSummaryRows[i].IndexLabel.text = t_shiftLabelIndex + " " + row_index;
            }

            float containerHeight = shifts.Length * 36f + 10f;
            m_shiftsSummaryContainer.height = containerHeight;
            m_shiftsSummaryRow.height = containerHeight + 50f;
            m_shiftsEditBtn.relativePosition = new Vector3(10f, containerHeight + 40f);

            float height = 80f + containerHeight;
            m_actionRow.relativePosition = new Vector3(10f, 210f + height);
            m_accessAdvancedSettingsBtn.relativePosition = new Vector3(20f, 210f + height + 80f);
            this.height = 350f + height;
        }

        private static float ParseHour(string s)
        {
            string[] parts = s.Split(':');
            if (parts.Length == 2 && int.TryParse(parts[0], out int h) && int.TryParse(parts[1], out int m))
            {
                return h + m / 60f;
            }
            return 0f;
        }

        private static string FormatHour(float h)
        {
            int hour = (int)h;
            int minute = (int)(h % 1f * 60f);
            return $"{hour:D2}:{minute:D2}";
        }

        private void CreateShiftEditRows()
        {
            m_shiftsEditorLabel = UILabels.CreateLabel(m_shiftsEditorPanel, "ShiftsEditorLabel", "", "");
            m_shiftsEditorLabel.font = UIFonts.GetUIFont("OpenSans-Regular");
            m_shiftsEditorLabel.textAlignment = UIHorizontalAlignment.Left;
            m_shiftsEditorLabel.textColor = new Color32(255, 255, 255, 255);
            m_shiftsEditorLabel.relativePosition = new Vector3(15f, 10f);
            m_shiftsEditorLabel.textScale = 1.3f;

            m_shiftEditRows = new ShiftRow[5];

            for (int i = 0; i < 5; i++)
            {
                float y = 40 + i * 36f;
                var row = new ShiftRow
                {
                    Panel = UIPanels.CreatePanel(m_shiftsEditorPanel, $"ShiftEditRow_{i + 1}")
                };
                row.Panel.size = new Vector2(460f, 30f);
                row.Panel.relativePosition = new Vector3(0f, y);

                row.IndexLabel = UILabels.CreateLabel(row.Panel, $"ShiftEditLabel_{i + 1}", "", "");
                row.IndexLabel.relativePosition = new Vector3(10f, 7f);
                row.IndexLabel.width = 55f;

                row.StartHourField = UITextFields.CreateTextField(row.Panel, $"ShiftEditStartHour_{i + 1}", "");
                row.StartHourField.size = new Vector2(30f, 24f);
                row.StartHourField.maxLength = 2;
                row.StartHourField.numericalOnly = true;
                row.StartHourField.relativePosition = new Vector3(85f, 3f);
                row.StartHourField.eventLostFocus += (_, __) => row.StartHourField.text = int.TryParse(row.StartHourField.text, out int v) ? v.ToString("D2") : "00"; // pad to 2 digits on blur
                row.StartHourField.eventTextChanged += (_, value) =>
                {
                    if (int.TryParse(value, out int v))
                    {
                        if (v > 23)
                        {
                            row.StartMinuteField.text = "23";
                        }
                        else if (v < 0)
                        {
                            row.StartMinuteField.text = "00";
                        }
                    }
                };
               
                row.StartColon = UILabels.CreateLabel(row.Panel, $"ShiftEditStartColon_{i + 1}", ":", "");
                row.StartColon.size = new Vector2(11.25f, 18f);
                row.StartColon.relativePosition = new Vector3(120f, 7f);

                row.StartMinuteField = UITextFields.CreateTextField(row.Panel, $"ShiftEditStartMinute_{i + 1}", "");
                row.StartMinuteField.size = new Vector2(30f, 24f);
                row.StartMinuteField.maxLength = 2;
                row.StartMinuteField.numericalOnly = true;
                row.StartMinuteField.relativePosition = new Vector3(130f, 3f);
                row.StartMinuteField.eventLostFocus += (_, __) => row.StartMinuteField.text = int.TryParse(row.StartMinuteField.text, out int v) ? v.ToString("D2") : "00"; // pad to 2 digits on blur
                row.StartMinuteField.eventTextChanged += (_, value) =>
                {
                    if (int.TryParse(value, out int v))
                    {
                        if (v > 59)
                        {
                            row.StartMinuteField.text = "59";
                        }
                        else if (v < 0)
                        {
                            row.StartMinuteField.text = "00";
                        }
                    }
                };

                row.Arrow = UILabels.CreateLabel(row.Panel, $"ShiftEditArrow_{i + 1}", "->", "");
                row.Arrow.size = new Vector2(15f, 18f);
                row.Arrow.relativePosition = new Vector3(165f, 7f);

                row.EndHourField = UITextFields.CreateTextField(row.Panel, $"ShiftEditEnd_{i + 1}", "");
                row.EndHourField.size = new Vector2(30f, 24f);
                row.EndHourField.relativePosition = new Vector3(185f, 3f);
                row.EndHourField.eventLostFocus += (_, __) => row.EndHourField.text = int.TryParse(row.EndHourField.text, out int v) ? v.ToString("D2") : "00"; // pad to 2 digits on blur
                row.EndHourField.eventTextChanged += (_, value) =>
                {
                    if (int.TryParse(value, out int v))
                    {
                        if (v > 23)
                        {
                            row.EndHourField.text = "23";
                        }
                        else if (v < 0)
                        {
                            row.EndHourField.text = "00";
                        }
                    }
                };

                row.EndColon = UILabels.CreateLabel(row.Panel, $"ShiftEditEndColon_{i + 1}", ":", "");
                row.EndColon.size = new Vector2(11.25f, 18f);
                row.EndColon.relativePosition = new Vector3(220f, 7f);

                row.EndMinuteField = UITextFields.CreateTextField(row.Panel, $"ShiftEditEndMinute_{i + 1}", "");
                row.EndMinuteField.size = new Vector2(30f, 24f);
                row.EndMinuteField.maxLength = 2;
                row.EndMinuteField.relativePosition = new Vector3(230f, 3f);
                row.EndMinuteField.eventLostFocus += (_, __) => row.EndMinuteField.text = int.TryParse(row.EndMinuteField.text, out int v) ? v.ToString("D2") : "00"; // pad to 2 digits on blur
                row.EndMinuteField.eventTextChanged += (_, value) =>
                {
                    if (int.TryParse(value, out int v))
                    {
                        if (v > 59)
                        {
                            row.EndMinuteField.text = "59";
                        }
                        else if (v < 0)
                        {
                            row.EndMinuteField.text = "00";
                        }
                    }
                };

                if (i > 0)
                {
                    row.RemoveBtn = UIButtons.CreateButton(row.Panel, 265f, 1f, $"RemoveShift_{i + 1}", "×", "", 28f);
                    int captured = i;
                    row.RemoveBtn.eventClicked += (_, __) => RemoveShift(captured);
                }

                row.IsActive = false;

                row.Panel.isVisible = false;

                m_shiftEditRows[i] = row;
            }

            m_addShiftBtn = UIButtons.CreateButton(m_shiftsEditorPanel, 5f, 220f, "AddShift", "+ Add Shift", "", 280f);
            m_addShiftBtn.eventClicked += (_, __) => AddShift();
            
            m_ignorePolicy = UICheckBoxes.CreateCheckBox(m_shiftsEditorPanel, "IgnorePolicy", "", "", false);
            m_ignorePolicy.width = 110f;
            m_ignorePolicy.label.textColor = new Color32(185, 221, 254, 255);
            m_ignorePolicy.label.textScale = 0.8125f;
            m_ignorePolicy.relativePosition = new Vector3(10f, 260f);

            RefreshShiftsSummary();
        }

        private void CreateActionButtons()
        {
            m_saveBuildingSettingsBtn = UIButtons.CreateButton(m_actionRow, 10f, 0f, "SaveBuildingSettings", "", "", 120f);
            m_saveBuildingSettingsBtn.eventClicked += SaveBuildingSettings;

            m_returnToDefaultBtn = UIButtons.CreateButton(m_actionRow, 150f, 0f, "ReturnToDefault", "", "", 120f);
            m_returnToDefaultBtn.eventClicked += ReturnToDefault;

            m_applyPrefabSettingsBtn = UIButtons.CreateButton(m_actionRow, 10f, 40f, "ApplyPrefabSettings", "", "", 120f);
            m_applyPrefabSettingsBtn.eventClicked += ApplyPrefabSettings;

            m_applyGlobalSettingsBtn = UIButtons.CreateButton(m_actionRow, 150f, 40f, "ApplyGlobalSettings", "", "", 120f);
            m_applyGlobalSettingsBtn.eventClicked += ApplyGlobalSettings;
        }

        private void CreateDangerButtons()
        {
            m_setPrefabSettingsBtn = UIButtons.CreateButton(m_dangerRow, 10f, 5f, "SetPrefabSettings", "", "", 120f);
            m_setPrefabSettingsBtn.eventClicked += SetPrefabSettings;

            m_setGlobalSettingsBtn = UIButtons.CreateButton(m_dangerRow, 150f, 5f, "SetGlobalSettings", "", "", 120f);
            m_setGlobalSettingsBtn.eventClicked += SetGlobalSettings;

            m_deletePrefabSettingsBtn = UIButtons.CreateButton(m_dangerRow, 10f, 40f, "DeletePrefabSettings", "", "", 120f);
            m_deletePrefabSettingsBtn.eventClicked += DeletePrefabSettings;

            m_deleteGlobalSettingsBtn = UIButtons.CreateButton(m_dangerRow, 150f, 40f, "DeleteGlobalSettings", "", "", 120f);
            m_deleteGlobalSettingsBtn.eventClicked += DeleteGlobalSettings;
        }

        public void UpdateData(float panelHeight, ILocalizationProvider localizationProvider)
        {
            Translate(localizationProvider);
            relativePosition = new Vector3(parent.width + 1, panelHeight);
        }

        private void Translate(ILocalizationProvider localizationProvider)
        {
            if(localizationProvider == null)
            {
                Debug.LogError("Localization provider is null. Cannot translate Operation Hours UI.");
                return;
            }

            // ─────────────────────────────────────────── Header translation ──────────────────────────────────────
            m_settingsTitle?.text = localizationProvider.Translate(TranslationKeys.SettingsTitle);

            m_editSettingsBtn?.tooltip = localizationProvider.Translate(TranslationKeys.EditSettingsTooltip);
            m_lockUnlockChangesBtn?.tooltip = localizationProvider.Translate(TranslationKeys.LockUnlockChangesTooltip);
            m_settingsStatusLabel?.text = localizationProvider.Translate(TranslationKeys.BuildingStatusLabel);
            m_settingsCopyBtn?.tooltip = localizationProvider.Translate(TranslationKeys.CopySettingsTooltip);
            m_settingsPasteBtn?.tooltip = localizationProvider.Translate(TranslationKeys.PasteSettingsTooltip);
            t_defaultSettingsStatus = localizationProvider.Translate(TranslationKeys.DefaultSettingsStatus);
            t_buildingSettingsStatus = localizationProvider.Translate(TranslationKeys.BuildingSettingsStatus);
            t_prefabSettingsStatus = localizationProvider.Translate(TranslationKeys.PrefabSettingsStatus);
            t_globalSettingsStatus = localizationProvider.Translate(TranslationKeys.GlobalSettingsStatus);
            // ────────────────────────────────────────── Days row translation ─────────────────────────────────────────
            m_activeDaysLabel?.text = localizationProvider.Translate(TranslationKeys.ActiveDays);

            if(m_dayButtons == null)
            {
                Debug.LogError("Day buttons array is null. Cannot set translations.");
                return;
            }

            foreach (var btn in m_dayButtons)
            {
                btn?.text = localizationProvider.Translate(btn.name);
                btn?.tooltip = localizationProvider.Translate(btn.name + "Tooltip");
            }

            // ──────────────────────────────────────── Shifts summary and editor common translations ─────────────────────────────────────────
            t_shiftLabelIndex = localizationProvider.Translate(TranslationKeys.ShiftLabelIndex);

            // ───────────────────────────────────────── Shifts summary translation ─────────────────────────────────────────
            m_shiftsSummaryLabel?.text = localizationProvider.Translate(TranslationKeys.ShiftsSummaryLabel);

            if(m_shiftSummaryRows == null)
            {
                Debug.LogError("Shift summary rows array is null. Cannot set translations.");
                return;
            }

            for (int i = 0; i < m_shiftSummaryRows.Length; i++)
            {
                int index = i + 1;
                m_shiftSummaryRows[i].IndexLabel?.text = t_shiftLabelIndex + " " + index;
            }

            m_shiftsEditBtn?.text = localizationProvider.Translate(TranslationKeys.EditShifts);
            m_shiftsEditBtn?.tooltip = localizationProvider.Translate(TranslationKeys.EditShiftsTooltip);

            // ───────────────────────────────────────── Shifts editor translation ─────────────────────────────────────────
            m_shiftsEditorLabel?.text = localizationProvider.Translate(TranslationKeys.ShiftsEditorLabel);
            for (int i = 0; i < m_shiftEditRows.Length; i++)
            {
                int index = i + 1;
                m_shiftEditRows[i].IndexLabel.text = t_shiftLabelIndex + " " + index;
            }

            m_addShiftBtn?.text = localizationProvider.Translate(TranslationKeys.AddShift);
            m_addShiftBtn?.tooltip = localizationProvider.Translate(TranslationKeys.AddShiftTooltip);

            m_ignorePolicy?.text = localizationProvider.Translate(TranslationKeys.IgnorePolicy);
            m_ignorePolicy?.tooltip = localizationProvider.Translate(TranslationKeys.IgnorePolicyTooltip);

            // ──────────────────────────────────────── Invalid shifts error message translation ─────────────────────────────────────────
            t_invalidShiftsTitle = localizationProvider.Translate(TranslationKeys.InvalidShiftsTitle);
            t_invalidShiftsText = localizationProvider.Translate(TranslationKeys.InvalidShiftsText);

            // ──────────────────────────────────────── Advanced Settings button translation ─────────────────────────────────────────
            m_accessAdvancedSettingsBtn?.text = localizationProvider.Translate(TranslationKeys.AccessAdvancedSettings);
            m_accessAdvancedSettingsBtn?.tooltip = localizationProvider.Translate(TranslationKeys.AccessAdvancedSettingsTooltip);

            // ──────────────────────────────────────── Action and Danger buttons translation ─────────────────────────────────────────
            m_saveBuildingSettingsBtn?.text = localizationProvider.Translate(TranslationKeys.SaveBuildingSettings);
            m_saveBuildingSettingsBtn?.tooltip = localizationProvider.Translate(TranslationKeys.SaveBuildingSettingsTooltip);
            m_returnToDefaultBtn?.text = localizationProvider.Translate(TranslationKeys.ReturnToDefault);
            m_returnToDefaultBtn?.tooltip = localizationProvider.Translate(TranslationKeys.ReturnToDefaultTooltip);

            m_applyPrefabSettingsBtn?.text = localizationProvider.Translate(TranslationKeys.ApplyPrefabSettings);
            m_applyPrefabSettingsBtn?.tooltip = localizationProvider.Translate(TranslationKeys.ApplyPrefabSettingsTooltip);
            m_applyGlobalSettingsBtn?.text = localizationProvider.Translate(TranslationKeys.ApplyGlobalSettings);
            m_applyGlobalSettingsBtn?.tooltip = localizationProvider.Translate(TranslationKeys.ApplyGlobalSettingsTooltip);

            m_setPrefabSettingsBtn?.text = localizationProvider.Translate(TranslationKeys.SetPrefabSettings);
            m_setPrefabSettingsBtn?.tooltip = localizationProvider.Translate(TranslationKeys.SetPrefabSettingsTooltip);
            m_setGlobalSettingsBtn?.text = localizationProvider.Translate(TranslationKeys.SetGlobalSettings);
            m_setGlobalSettingsBtn?.tooltip = localizationProvider.Translate(TranslationKeys.SetGlobalSettingsTooltip);

            m_deletePrefabSettingsBtn?.text = localizationProvider.Translate(TranslationKeys.DeletePrefabSettings);
            m_deletePrefabSettingsBtn?.tooltip = localizationProvider.Translate(TranslationKeys.DeletePrefabSettingsTooltip);
            m_deleteGlobalSettingsBtn?.text = localizationProvider.Translate(TranslationKeys.DeleteGlobalSettings);
            m_deleteGlobalSettingsBtn?.tooltip = localizationProvider.Translate(TranslationKeys.DeleteGlobalSettingsTooltip);

            t_confirmPanelSetPrefabTitle = localizationProvider.Translate(TranslationKeys.ConfirmPanelSetPrefabTitle);
            t_confirmPanelSetPrefabText = localizationProvider.Translate(TranslationKeys.ConfirmPanelSetPrefabText);
            t_confirmPanelSetGlobalTitle = localizationProvider.Translate(TranslationKeys.ConfirmPanelSetGlobalTitle);
            t_confirmPanelSetGlobalText = localizationProvider.Translate(TranslationKeys.ConfirmPanelSetGlobalText);

            t_confirmPanelDeletePrefabTitle = localizationProvider.Translate(TranslationKeys.ConfirmPanelDeletePrefabTitle);
            t_confirmPanelDeletePrefabText = localizationProvider.Translate(TranslationKeys.ConfirmPanelDeletePrefabText);
            t_confirmPanelDeleteGlobalTitle = localizationProvider.Translate(TranslationKeys.ConfirmPanelDeleteGlobalTitle);
            t_confirmPanelDeleteGlobalText = localizationProvider.Translate(TranslationKeys.ConfirmPanelDeleteGlobalText);
        }

        private void EditSettings(UIComponent c, UIMouseEventParameter eventParameter)
        {
            m_editMode = !m_editMode;

            string spriteName = m_editMode ? "NoneEdit" : "Edit";

            foreach (var btn in m_dayButtons)
            {
                btn.isEnabled = m_editMode;
            }

            m_saveBuildingSettingsBtn.isEnabled = m_editMode;
            m_returnToDefaultBtn.isEnabled = m_editMode;
            m_shiftsEditBtn.isEnabled = m_editMode;
            m_accessAdvancedSettingsBtn.isEnabled = m_editMode;
                
            ushort buildingID = WorldInfoPanel.GetCurrentInstanceID().Building;
            var building = Singleton<BuildingManager>.instance.m_buildings.m_buffer[buildingID];

            bool prefabExists = BuildingWorkTimeManager.PrefabExist(building.Info);

            m_applyPrefabSettingsBtn.isEnabled = m_editMode && prefabExists;
            m_setPrefabSettingsBtn.isEnabled = m_editMode && !prefabExists;
            m_deletePrefabSettingsBtn.isEnabled = m_editMode && prefabExists;

            bool globalSettingsExist = BuildingWorkTimeGlobalConfig.Config.GlobalSettingsExist(building.Info);

            m_applyGlobalSettingsBtn.isEnabled = m_editMode && globalSettingsExist;
            m_setGlobalSettingsBtn.isEnabled = m_editMode && !globalSettingsExist;
            m_deleteGlobalSettingsBtn.isEnabled = m_editMode && globalSettingsExist;

            m_editSettingsBtn.normalFgSprite = spriteName;
            m_editSettingsBtn.disabledFgSprite = spriteName;
            m_editSettingsBtn.focusedFgSprite = spriteName;
            m_editSettingsBtn.hoveredFgSprite = spriteName;
            m_editSettingsBtn.pressedFgSprite = spriteName;

            m_editSettingsBtn.Invalidate();
            m_editSettingsBtn.parent.Invalidate();
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

            m_lockUnlockChangesBtn.size = buildingWorkTime.IsLocked ? new Vector2(27f, 32f) : new Vector2(20f, 32f);

            m_lockUnlockChangesBtn.Invalidate();
            m_lockUnlockChangesBtn.parent.Invalidate();

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

        private void SetActiveShifts(BuildingWorkTimeManager.WorkShiftTime[] workShifts)
        {
            // hide all rows first
            foreach (var row in m_shiftEditRows)
            {
                row.IsActive = false;
                row.Panel.isVisible = false;
            }

            if (workShifts == null)
            {
                return;
            }

            for (int i = 0; i < workShifts.Length && i < m_shiftEditRows.Length; i++)
            {
                m_shiftEditRows[i].SetEntry(workShifts[i]);
                m_shiftEditRows[i].Panel.isVisible = true;
            }

            // only show add button if there's still room
            m_addShiftBtn.isEnabled = workShifts.Length < m_shiftEditRows.Length;
            RefreshShiftsSummary();
        }

        private BuildingWorkTimeManager.WorkShiftTime[] GetActiveShifts()
        {
            var list = new List<BuildingWorkTimeManager.WorkShiftTime>();
            foreach (var row in m_shiftEditRows)
            {
                if (row.IsActive)
                {
                    var entry = row.GetEntry();
                    if (entry.IsValid)
                    {
                        list.Add(entry);
                    }
                }
            }
            return [.. list];
        }

        private void AddShift()
        {
            for (int i = 1; i < m_shiftEditRows.Length; i++)
            {
                if (!m_shiftEditRows[i].IsActive)
                {
                    var workShiftTime = m_shiftEditRows[i - 1].GetEntry();
                    float startHour = workShiftTime.EndTime;
                    float endHour = workShiftTime.EndTime + 8f;
                    if(endHour > 23f)
                    {
                        endHour -= 24f;
                    }
                    m_shiftEditRows[i].SetEntry(new BuildingWorkTimeManager.WorkShiftTime { StartTime = startHour, EndTime = endHour });
                    m_shiftEditRows[i].Panel.isVisible = true;
                    m_addShiftBtn.isEnabled = i < 4;
                    RefreshShiftsSummary();
                    return;
                }
            }
        }

        private void RemoveShift(int index)
        {
            // collect all active shifts except the removed one
            var remaining = new List<BuildingWorkTimeManager.WorkShiftTime>();
            for (int i = 0; i < m_shiftEditRows.Length; i++)
            {
                if (i != index && m_shiftEditRows[i].IsActive)
                {
                    remaining.Add(m_shiftEditRows[i].GetEntry());
                }
            }

            // clear all rows
            foreach (var row in m_shiftEditRows)
            {
                row.IsActive = false;
                row.Panel.isVisible = false;
            }

            // re-fill in order
            for (int i = 0; i < remaining.Count; i++)
            {
                m_shiftEditRows[i].SetEntry(remaining[i]);
                m_shiftEditRows[i].Panel.isVisible = true;
                int row_index = i + 1;
                m_shiftEditRows[i].IndexLabel.text = t_shiftLabelIndex + " " + row_index;
            }

            m_addShiftBtn.isEnabled = remaining.Count < m_shiftEditRows.Length;
            RefreshShiftsSummary();
        }

        private void CopySettings(UIComponent c, UIMouseEventParameter eventParameter)
        {
            m_isCopied = false;

            var days = GetActiveDays();
            var shifts = GetActiveShifts();

            if(days == null || shifts == null)
            {
                return;
            }

            if (days.Length > 0)
            {
                m_daysOfWeekClipboard.Clear();
                m_daysOfWeekClipboard = [.. days];
            }

            if (shifts.Length > 0)
            {
                m_shiftsClipboard.Clear();
                m_shiftsClipboard = [.. shifts];
            }

            m_ignorePolicyClipboard = m_ignorePolicy.isChecked;

            m_isLockedClipboard = m_lockUnlockChangesBtn.normalFgSprite == "Lock";

            m_isCopied = true;
        }

        private void PasteSettings(UIComponent c, UIMouseEventParameter eventParameter)
        {
            if (!m_isCopied)
            {
                return;
            }

            ushort buildingID = WorldInfoPanel.GetCurrentInstanceID().Building;

            var newBuildingSettings = new BuildingWorkTimeManager.WorkTime
            {
                WorkDays = [.. m_daysOfWeekClipboard],
                WorkShifts = [.. m_shiftsClipboard],
                IgnorePolicy = m_ignorePolicyClipboard,
                IsLocked = m_isLockedClipboard
            };

            UpdateBuildingSettings.SaveNewSettings(buildingID, newBuildingSettings);

            RefreshData(buildingID, newBuildingSettings);
        }

        private bool AreAllShiftsValid()
        {
            foreach (var row in m_shiftEditRows)
            {
                if (row.IsActive && !row.GetEntry().IsValid)
                {
                    return false;
                }
            }

            return true;
        }

        private void SetAllControlsToDisabled()
        {
            foreach (var btn in m_dayButtons)
            {
                btn.Disable();
            }

            // action/danger buttons follow their own conditional logic
            m_shiftsEditBtn.Disable();
            m_accessAdvancedSettingsBtn.Disable();
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
                btn.hoveredBgSprite = "ButtonMenuFocused";
                btn.pressedBgSprite = "ButtonMenuFocused";
                btn.focusedBgSprite = "ButtonMenuFocused";
                btn.disabledBgSprite = "ButtonMenuFocused";
                btn.color = new Color32(255, 255, 255, 255);
                btn.textColor = new Color32(110, 203, 216, 255); // teal
            }
            else
            {
                btn.normalBgSprite = "ButtonMenu";
                btn.hoveredBgSprite = "ButtonMenuHovered";
                btn.pressedBgSprite = "ButtonMenuFocused";
                btn.focusedBgSprite = "ButtonMenu";
                btn.disabledBgSprite = "ButtonMenuDisabled";
                btn.color = new Color32(100, 110, 140, 255);
                btn.textColor = new Color32(80, 88, 120, 255);
            }
        }

        private void SaveBuildingSettings(UIComponent c, UIMouseEventParameter eventParameter)
        {
            if (!AreAllShiftsValid())
            {
                UIView.library.ShowModal<ExceptionPanel>("ExceptionPanel").SetMessage(t_invalidShiftsTitle, t_invalidShiftsText, true);
                return;
            }

            ushort buildingID = WorldInfoPanel.GetCurrentInstanceID().Building;

            var newBuildingSettings = new BuildingWorkTimeManager.WorkTime
            {
                WorkDays = GetActiveDays(),
                WorkShifts = GetActiveShifts(),
                IgnorePolicy = m_ignorePolicy.isChecked,
                IsLocked = m_lockUnlockChangesBtn.normalFgSprite == "Lock"
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

                SetActiveDays(prefabRecord.WorkDays);
                SetActiveShifts(prefabRecord.WorkShifts);
                m_ignorePolicy.isChecked = prefabRecord.IgnorePolicy;
                m_settingsStatus.text = t_prefabSettingsStatus;

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

                SetActiveDays(buildingWorkTimeGlobal.WorkDays);
                SetActiveShifts(UpdateBuildingSettings.ConvertXMLToWorkShifts(buildingWorkTimeGlobal.WorkShifts));
                m_ignorePolicy.isChecked = buildingWorkTimeGlobal.IgnorePolicy;
                m_settingsStatus.text = t_globalSettingsStatus;

                UpdateBuildingSettings.SetBuildingToGlobal(buildingID, buildingWorkTimeGlobal);
            }
        }

        private void SetPrefabSettings(UIComponent c, UIMouseEventParameter eventParameter)
        {
            if (!AreAllShiftsValid())
            {
                UIView.library.ShowModal<ExceptionPanel>("ExceptionPanel").SetMessage(t_invalidShiftsTitle, t_invalidShiftsText, true);
                return;
            }

            ConfirmPanel.ShowModal(t_confirmPanelSetPrefabTitle, t_confirmPanelSetPrefabText, (comp, ret) => PrefabSettingsConfirmPanel(ret));
        }

        private void PrefabSettingsConfirmPanel(int ret)
        {
            if (ret != 1)
            {
                return;
            }
            ushort buildingID = WorldInfoPanel.GetCurrentInstanceID().Building;

            var buildingWorkTime = BuildingWorkTimeManager.GetBuildingWorkTime(buildingID);

            var newPrefabSettings = new BuildingWorkTimeManager.WorkTime
            {
                WorkDays = GetActiveDays(),
                WorkShifts = GetActiveShifts(),
                IgnorePolicy = m_ignorePolicy.isChecked
            };

            if (!buildingWorkTime.IsLocked)
            {
                m_settingsStatus.text = t_prefabSettingsStatus;
            }

            UpdateBuildingSettings.CreatePrefabSettings(buildingID, newPrefabSettings);
        }

        private void SetGlobalSettings(UIComponent c, UIMouseEventParameter eventParameter)
        {
            if (!AreAllShiftsValid())
            {
                UIView.library.ShowModal<ExceptionPanel>("ExceptionPanel").SetMessage(t_invalidShiftsTitle, t_invalidShiftsText, true);
                return;
            }

            ConfirmPanel.ShowModal(t_confirmPanelSetGlobalTitle, t_confirmPanelSetGlobalText, (comp, ret) => PrefabSettingsConfirmPanel(ret));
        }

        private void GlobalSettingsConfirmPanel(int ret)
        {
            if (ret != 1)
            {
                return;
            }
            ushort buildingID = WorldInfoPanel.GetCurrentInstanceID().Building;

            var buildingWorkTime = BuildingWorkTimeManager.GetBuildingWorkTime(buildingID);

            var newGlobalSettings = new BuildingWorkTimeManager.WorkTime
            {
                WorkDays = GetActiveDays(),
                WorkShifts = GetActiveShifts(),
                IgnorePolicy = m_ignorePolicy.isChecked
            };

            if (!buildingWorkTime.IsLocked)
            {
                m_settingsStatus.text = t_globalSettingsStatus;
            }

            UpdateBuildingSettings.CreateGlobalSettings(buildingID, newGlobalSettings);
        }

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

            SetActiveDays(buildingWorkTimeDefault.WorkDays);
            SetActiveShifts(buildingWorkTimeDefault.WorkShifts);
            m_ignorePolicy.isChecked = buildingWorkTimeDefault.IgnorePolicy;

            UpdateBuildingSettings.UpdateBuildingToDefaultSettings(buildingID, buildingWorkTimeDefault);

            RefreshData(buildingID, buildingWorkTimeDefault);
        }

    }
}
