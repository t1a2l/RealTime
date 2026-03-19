namespace RealTime.UI
{
    using System.Collections.Generic;
    using System.Reflection;
    using ColossalFramework;
    using ColossalFramework.UI;
    using RealTime.Events.Containers;
    using RealTime.Events.Storage;  // CityEventsLoader
    using RealTime.Utils.UIUtils;
    using UnityEngine;

    public static class NewBuildingWorldInfoPanel
    {
        private static UITextField mNameField;
        private static UIButton createEventButton;
        private static UIFastList eventSelection;
        private static UserEventCreationWindow eventCreationWindow;
        private static float originalNameWidth = -1f;
        private static InstanceID? lastInstanceID;

        public static void OnSetTarget(BuildingWorldInfoPanel thisPanel)
        {
            var m_TimeInfo = typeof(BuildingWorldInfoPanel).GetField("m_Time", BindingFlags.NonPublic | BindingFlags.Instance);
            float? m_Time = m_TimeInfo.GetValue(thisPanel) as float?;

            if (m_Time != null)
            {
                mNameField = thisPanel.Find<UITextField>("BuildingName");

                if (mNameField != null)
                {
                    if (originalNameWidth == -1)
                    {
                        originalNameWidth = mNameField.width;
                    }

                    
                    m_Time = 0.0f;

                    var servicePanel = thisPanel as CityServiceWorldInfoPanel;

                    if (servicePanel != null)
                    {
                        Debug.Log("Adding event UI to service panel.");
                        AddEventUI(servicePanel);
                        mNameField.text = GetName();
                    }
                }
                else
                {
                    Debug.LogError("Couldn't set the m_NameField parameter of the BuildingWorldInfoPanel");
                }
            }
            else
            {
                Debug.LogError("Couldn't set the m_Time parameter of the BuildingWorldInfoPanel");
            }
        }

        private static string GetName()
        {
            if (lastInstanceID.Value != null && lastInstanceID.Value.Type == InstanceType.Building && lastInstanceID.Value.Building != 0)
            {
                return Singleton<BuildingManager>.instance.GetBuildingName(lastInstanceID.Value.Building, InstanceID.Empty);
            }
            return string.Empty;
        }

        private static void AddEventUI(CityServiceWorldInfoPanel parent)
        {
            var m_InstanceIDInfo = typeof(CityServiceWorldInfoPanel).GetField("m_InstanceID", BindingFlags.NonPublic | BindingFlags.Instance);
            var m_InstanceID = m_InstanceIDInfo.GetValue(parent) as InstanceID?;

            lastInstanceID = m_InstanceID;

            // Create "Create Event" button (next to Location button)
            createEventButton = parent.Find<UIButton>("CreateEventButton");
            if (createEventButton == null)
            {
                var locationBtn = parent.Find<UIMultiStateButton>("LocationMarker");
                if (locationBtn == null)
                {
                    return;
                }

                createEventButton = parent.component.AddUIComponent<UIButton>();
                createEventButton.name = "CreateEventButton";
                createEventButton.atlas = parent.GetComponent<UIPanel>().atlas;  // Reuse panel atlas
                createEventButton.normalFgSprite = "InfoIconLevel";
                createEventButton.width = locationBtn.width;
                createEventButton.height = locationBtn.height;
                createEventButton.relativePosition = locationBtn.relativePosition + new Vector3(-(locationBtn.width + 5f), 0);
                createEventButton.eventClicked += OnCreateEventButtonClicked;
            }

            // Create event dropdown
            eventSelection = parent.Find<UIFastList>("EventSelectionList");
            if (eventSelection == null)
            {
                eventSelection = UIFastList.Create<UIFastListLabel>(parent.component);
                eventSelection.name = "EventSelectionList";
                eventSelection.backgroundSprite = "UnlockingPanel";
                eventSelection.size = new Vector2(120, 60);
                eventSelection.rowHeight = 20f;
                eventSelection.canSelect = true;
                eventSelection.selectedIndex = -1;
                eventSelection.eventSelectedIndexChanged += OnEventSelectionChanged;
                eventSelection.Hide();
            }

            // Create event window (slides in)
            eventCreationWindow = parent.Find<UserEventCreationWindow>("EventCreator");
            if (eventCreationWindow == null)
            {
                eventCreationWindow = parent.component.AddUIComponent<UserEventCreationWindow>();
                eventCreationWindow.name = "EventCreator";
                eventCreationWindow.Hide();
            }
        }

        private static void OnCreateEventButtonClicked(UIComponent c, UIMouseEventParameter p)
        {
            if (lastInstanceID == null || lastInstanceID.Value.Building == 0)
            {
                return;
            }

            ushort buildingId = lastInstanceID.Value.Building;
            bool hasTemplates = GetTemplatesForBuilding(buildingId).Count > 0;

            createEventButton.enabled = hasTemplates;

            if (eventSelection.isVisible)
            {
                eventSelection.Hide();
                eventCreationWindow?.Hide();
            }
            else
            {
                BuildDropdownList();
                eventSelection.Show();
            }
        }

        private static void OnEventSelectionChanged(UIComponent c, int index)
        {
            if (index < 0 || eventCreationWindow == null)
            {
                return;
            }

            ushort buildingId = lastInstanceID.Value.Building;

            // Get selected item (not raw template)
            var selectedItem = eventSelection.rowsData[index] as LabelOptionItem;
            if (selectedItem?.linkedTemplate == null)
            {
                return;
            }

            eventCreationWindow.Show();
            eventCreationWindow.SetUp(selectedItem, buildingId);  // ← LabelOptionItem!
            eventCreationWindow.relativePosition = eventSelection.relativePosition + new Vector3(-eventSelection.width - 2f, eventSelection.height);
        }

        // Real Time replacements
        private static List<CityEventTemplate> GetTemplatesForBuilding(ushort buildingId)
        {
            var buildingMgr = Singleton<BuildingManager>.instance;
            var building = buildingMgr.m_buildings.m_buffer[buildingId];
            string buildingName = building.Info.name;

            // Public method (Events private)
            return CityEventsLoader.Instance.GetEventTemplates(buildingName);
        }

        private static void UpdateEventSelection(UIComponent component)
        {
            var list = component as UIFastList;

            if (list != null)
            {
                if (list.selectedItem is LabelOptionItem selectedOption && eventCreationWindow != null)
                {
                    eventCreationWindow.Show();
                    eventCreationWindow.SetUp(selectedOption, lastInstanceID.Value.Building);
                    eventCreationWindow.relativePosition = list.relativePosition + new Vector3(-(list.width / 2f), list.height);

                    Debug.Log("Selected " + list.selectedIndex);
                }
                else
                {
                    Debug.LogError("Couldn't find the option that has been selected for an event!");
                }
            }
            else
            {
                Debug.LogError("Couldn't find the list that the selection was made on!");
            }
        }

        private static void BuildDropdownList()
        {
            if (eventSelection == null)
            {
                return;
            }

            eventSelection.rowsData.Clear();
            ushort buildingId = lastInstanceID.Value.Building;
            var templates = GetTemplatesForBuilding(buildingId);

            foreach (var template in templates)
            {
                var item = new LabelOptionItem
                {
                    linkedTemplate = template,
                    readableLabel = template.EventName
                };
                eventSelection.rowsData.Add(item);
            }
            eventSelection.DisplayAt(0);
        }

        private static void UpdateButtonState()
        {
            if (createEventButton == null || lastInstanceID == null)
            {
                return;
            }

            ushort buildingId = lastInstanceID.Value.Building;
            bool hasTemplates = GetTemplatesForBuilding(buildingId).Count > 0;
            createEventButton.Show();
            createEventButton.enabled = hasTemplates;

            mNameField.width = hasTemplates ? originalNameWidth - 45f : originalNameWidth;
        }
    }
}
