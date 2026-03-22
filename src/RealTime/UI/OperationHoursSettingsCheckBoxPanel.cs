namespace RealTime.UI
{
    using ColossalFramework.UI;
    using RealTime.Localization;
    using RealTime.Utils.UIUtils;
    using SkyTools.Localization;
    using UnityEngine;

    internal class OperationHoursSettingsCheckBoxPanel : UIPanel
    {
        private UICheckBox OperationHoursSettingsCheckBox;

        private BuildingOperationHoursPanel operationHoursPanel;

        public override void Awake()
        {
            base.Awake();

            name = "OperationHoursSettingsCheckBoxPanel";
            width = 230f;
            height = 40f;
            autoLayout = false;

            OperationHoursSettingsCheckBox = UICheckBoxes.CreateCheckBox(this, "OperationHoursSettingsCheckBox", "", "", false);
            OperationHoursSettingsCheckBox.width = 80f;
            OperationHoursSettingsCheckBox.label.textColor = new Color32(185, 221, 254, 255);
            OperationHoursSettingsCheckBox.label.textScale = 0.8125f;
            OperationHoursSettingsCheckBox.relativePosition = new Vector3(0, 0);
            OperationHoursSettingsCheckBox.eventCheckChanged += EventCheckChanged;
            OperationHoursSettingsCheckBox.label.width = 70f;

            isVisible = false;
        }

        public void UpdateData(float checkBoxXposition, float checkBoxYposition, ILocalizationProvider localizationProvider, BuildingOperationHoursPanel buildingOperationHoursPanel)
        {
            operationHoursPanel = buildingOperationHoursPanel;

            relativePosition = new Vector3(checkBoxXposition, checkBoxYposition);

            OperationHoursSettingsCheckBox.text = localizationProvider.Translate(TranslationKeys.OperationHoursSettingsCheckBox);
            OperationHoursSettingsCheckBox.tooltip = localizationProvider.Translate(TranslationKeys.OperationHoursSettingsCheckBoxTooltip);
        }

        private void EventCheckChanged(UIComponent c, bool value)
        {
            operationHoursPanel.isVisible = value;
            if (operationHoursPanel.isVisible)
            {
                operationHoursPanel.height = 470f;
            }
            else
            {
                operationHoursPanel.m_workAtNight.Disable();
                operationHoursPanel.m_workAtWeekands.Disable();
                operationHoursPanel.m_hasExtendedWorkShift.Disable();
                operationHoursPanel.m_hasContinuousWorkShift.Disable();
                operationHoursPanel.m_workShifts.Disable();

                operationHoursPanel.m_saveBuildingSettingsBtn.Disable();
                operationHoursPanel.m_returnToDefaultBtn.Disable();
                operationHoursPanel.m_applyPrefabSettingsBtn.Disable();
                operationHoursPanel.m_applyGlobalSettingsBtn.Disable();
                operationHoursPanel.m_setPrefabSettingsBtn.Disable();
                operationHoursPanel.m_setGlobalSettingsBtn.Disable();
                operationHoursPanel.m_deletePrefabSettingsBtn.Disable();
                operationHoursPanel.m_deleteGlobalSettingsBtn.Disable();
                operationHoursPanel.m_unlockSettingsBtn.Show();
            }
        }

        public void RefreshData(float checkBoxXposition, float checkBoxYposition, BuildingOperationHoursPanel buildingOperationHoursPanel)
        {
            Show();
            relativePosition = new Vector3(checkBoxXposition, checkBoxYposition);

            if (OperationHoursSettingsCheckBox.isChecked)
            {
                buildingOperationHoursPanel.height = 470f;
                buildingOperationHoursPanel.Show();
            }
        }

    }
}
