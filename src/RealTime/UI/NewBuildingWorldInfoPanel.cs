namespace RealTime.UI
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using ColossalFramework;
    using ColossalFramework.UI;
    using HarmonyLib;
    using Newtonsoft.Json;
    using RealTime.Events.Containers;
    using RealTime.Events.Storage;  // CityEventsLoader
    using RealTime.Utils.UIUtils;
    using UnityEngine;

    public static class NewBuildingWorldInfoPanel
    {
        private static UIPanel eventSelectionPanel;
        private static UIDropDown eventSelectionDropDown;
        private static UIButton eventSelectionButton;

        private static UserEventCreationWindow eventCreationWindow;

        private static InstanceID? lastInstanceID;

        private static readonly JsonSerializerSettings settings = new()
        {
            TypeNameHandling = TypeNameHandling.Objects,  // Preserves exact types on deserialize
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,  // Handles game object cycles
            Formatting = Formatting.None  // Compact for UIDropdown
        };

        public static void OnSetTarget()
        {
            if (eventSelectionPanel == null)
            {
                Debug.Log("Adding event UI to service panel.");
                CreateEventSelectionPanel();
            }
            CheckAndSetupEvents();
        }

        private static void CreateEventSelectionPanel()
        {
            var _cityServiceWorldInfoPanel = UIView.library.Get<CityServiceWorldInfoPanel>(typeof(CityServiceWorldInfoPanel).Name);
            if (!(_cityServiceWorldInfoPanel != null))
            {
                return;
            }

            var m_InstanceIDInfo = typeof(CityServiceWorldInfoPanel).GetField("m_InstanceID", BindingFlags.NonPublic | BindingFlags.Instance);
            var m_InstanceID = m_InstanceIDInfo.GetValue(_cityServiceWorldInfoPanel) as InstanceID?;

            lastInstanceID = m_InstanceID;

            var wrapper = _cityServiceWorldInfoPanel?.Find("Wrapper");
            var mainSectionPanel = wrapper?.Find("MainSectionPanel");
            var mainBottom = mainSectionPanel?.Find("MainBottom");
            var buttonPanels = mainBottom?.Find("ButtonPanels");
            if (buttonPanels != null)
            {
                eventSelectionPanel = buttonPanels.AddUIComponent<UIPanel>();
                eventSelectionPanel.name = "EventSelectionPanel";
                eventSelectionPanel.width = 230f;
                eventSelectionPanel.height = 40f;
                eventSelectionPanel.autoLayoutDirection = LayoutDirection.Vertical;
                eventSelectionPanel.autoLayoutStart = LayoutStart.TopLeft;
                eventSelectionPanel.autoLayoutPadding = new RectOffset(0, 0, 0, 5);
                eventSelectionPanel.autoLayout = true;
                eventSelectionPanel.relativePosition = new Vector3(150f, -40f);
                eventSelectionDropDown = UIDropDowns.AddLabelledDropDown(eventSelectionPanel, eventSelectionPanel.width, 160f, "Events:");
                
                eventSelectionButton = eventSelectionPanel.AddUIComponent<UIButton>();
                eventSelectionButton = UIButtons.CreateButton(eventSelectionPanel, 260f, 320f, "EventSelectionButton", "select", "");
                eventSelectionButton.eventClicked += OnEventSelectionButtonClicked;

                eventSelectionPanel.isVisible = false;

                buttonPanels.AttachUIComponent(eventSelectionPanel.gameObject);

            }

            // Create event window (slides in)
            eventCreationWindow = _cityServiceWorldInfoPanel.Find<UserEventCreationWindow>("EventCreator");
            if (eventCreationWindow == null)
            {
                eventCreationWindow = _cityServiceWorldInfoPanel.component.AddUIComponent<UserEventCreationWindow>();
                eventCreationWindow.name = "EventCreator";
                eventCreationWindow.Hide();
            }


        }


        private static void CheckAndSetupEvents()
        {
            if (lastInstanceID == null || lastInstanceID.Value.Building == 0)
            {
                return;
            }

            ushort buildingId = lastInstanceID.Value.Building;
            bool hasTemplates = GetTemplatesForBuilding(buildingId).Count > 0;

            if (hasTemplates)
            {
                eventSelectionPanel.isVisible = true;
                eventSelectionPanel.Show();
                BuildDropdownList();
            }
            else
            {
                eventSelectionPanel.isVisible = false;
                eventCreationWindow?.Hide();
            }
        }

        private static List<CityEventTemplate> GetTemplatesForBuilding(ushort buildingId)
        {
            var buildingMgr = Singleton<BuildingManager>.instance;
            var building = buildingMgr.m_buildings.m_buffer[buildingId];
            string buildingName = building.Info.name;

            // Public method (Events private)
            return CityEventsLoader.Instance.GetEventTemplates(buildingName);
        }

        private static void OnEventSelectionButtonClicked(UIComponent c, UIMouseEventParameter p)
        {
            int selected_index = eventSelectionDropDown.selectedIndex;
            if (selected_index == -1)
            {
                return;
            }

            string json = eventSelectionDropDown.items[selected_index];

            var selectedOption = JsonConvert.DeserializeObject<LabelOptionItem>(json, settings);

            eventCreationWindow.Show();
            eventCreationWindow.SetUp(selectedOption, lastInstanceID.Value.Building);
            eventCreationWindow.relativePosition = eventSelectionPanel.relativePosition + new Vector3(-(eventSelectionPanel.width / 2f), eventSelectionPanel.height);
        }

        private static void BuildDropdownList()
        {
            if (eventSelectionDropDown == null)
            {
                return;
            }

            Array.Clear(eventSelectionDropDown.items, 0, eventSelectionDropDown.items.Length);

            ushort buildingId = lastInstanceID.Value.Building;
            var templates = GetTemplatesForBuilding(buildingId);

            foreach (var template in templates)
            {
                var item = new LabelOptionItem
                {
                    linkedTemplate = template,
                    readableLabel = template.EventName
                };
                string json = JsonConvert.SerializeObject(item, settings);
                eventSelectionDropDown.items.AddItem(json);
            }
            eventSelectionDropDown.selectedIndex = 0;
        }

    }
}
