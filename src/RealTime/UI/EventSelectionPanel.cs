namespace RealTime.UI
{
    using System;
    using System.Collections.Generic;
    using ColossalFramework;
    using ColossalFramework.UI;
    using RealTime.Events.Storage;
    using RealTime.Utils.UIUtils;
    using UnityEngine;

    internal class EventSelectionPanel : UIPanel
    {
        private UIDropDown eventSelectionDropDown;
        private UIButton eventSelectionButton;
        private UserEventCreationPanel eventCreationPanel;

        public override void Awake()
        {
            base.Awake();

            name = "EventSelectionPanel";
            width = 230f;
            height = 40f;
            autoLayout = false;
            relativePosition = new Vector3(120f, 15f);

            UILabels.CreatePositionedLabel(this, 0f, -15f, "EventSelectionLabel", "Events:");

            eventSelectionDropDown = UIDropDowns.AddDropDown(this, 0f, 0f, "EventSelectionDropDown", 120f);

            eventSelectionButton = AddUIComponent<UIButton>();
            eventSelectionButton = UIButtons.CreateButton(this, 130f, 0f, "EventSelectionButton", "select", "", 80f);
            eventSelectionButton.eventClicked += OnEventSelectionButtonClicked;

            isVisible = false;
        }

        public void UpdateData(UserEventCreationPanel EventCreationPanel) => eventCreationPanel = EventCreationPanel;

        public void CheckAndSetupEvents()
        {
            ushort buildingID = WorldInfoPanel.GetCurrentInstanceID().Building;
            bool hasTemplates = GetTemplatesForBuilding(buildingID).Count > 0;
            if (hasTemplates)
            {
                isVisible = true;
                Show();
                BuildDropdownList(buildingID);
            }
            else
            {
                isVisible = false;
                Hide();
            }
        }

        private void OnEventSelectionButtonClicked(UIComponent c, UIMouseEventParameter p)
        {
            int selected_index = eventSelectionDropDown.selectedIndex;
            if (selected_index == -1)
            {
                return;
            }

            string event_name = eventSelectionDropDown.items[selected_index];

            ushort buildingID = WorldInfoPanel.GetCurrentInstanceID().Building;

            var buildingMgr = Singleton<BuildingManager>.instance;
            var building = buildingMgr.m_buildings.m_buffer[buildingID];
            string buildingName = building.Info.name;

            var template = CityEventsLoader.Instance.GetEventTemplate(event_name, buildingName);

            eventCreationPanel.Show();
            eventCreationPanel.SetUp(template, buildingID);
            eventCreationPanel.relativePosition = relativePosition + new Vector3(-(width / 2f), height);
        }

        private void BuildDropdownList(ushort buildingId)
        {
            if (eventSelectionDropDown == null)
            {
                return;
            }

            Array.Clear(eventSelectionDropDown.items, 0, eventSelectionDropDown.items.Length);

            var templates = GetTemplatesForBuilding(buildingId);

            var list = new List<string>();
            foreach (var template in templates)
            {
                list.Add(template.EventName);
            }

            eventSelectionDropDown.items = [.. list];
            eventSelectionDropDown.selectedIndex = 0;
        }

        private List<CityEventTemplate> GetTemplatesForBuilding(ushort buildingId)
        {
            var buildingMgr = Singleton<BuildingManager>.instance;
            var building = buildingMgr.m_buildings.m_buffer[buildingId];
            string buildingName = building.Info.name;

            // Public method (Events private)
            return CityEventsLoader.Instance.GetEventTemplates(buildingName);
        }

    }
}
