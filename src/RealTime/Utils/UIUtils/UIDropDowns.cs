namespace RealTime.Utils.UIUtils
{
    using ColossalFramework.UI;
    using UnityEngine;

    public static class UIDropDowns
    {
        /// <summary>
        /// Creates a dropdown menu with an attached text label.
        /// </summary>
        /// <param name="parent">Parent component.</param>
        /// <param name="xPos">Relative x position.</param>
        /// <param name="yPos">Relative y position.</param>
        /// <param name="name">Dropdown name.</param>
        /// <param name="text">Text label.</param>
        /// <param name="width">Dropdown menu width, excluding label (default 220f).</param>
        /// <param name="height">Dropdown button height (default 25f).</param>
        /// <param name="labelXpos">Label relative x position.</param>
        /// <param name="labelYpos">Label relative y position.</param>
        /// <param name="itemTextScale">Text scaling (default 0.7f).</param>
        /// <param name="itemHeight">Dropdown menu item height (default 20).</param>
        /// <param name="itemVertPadding">Dropdown menu item vertical text padding (default 8).</param>
        /// <param name="tooltip">Tooltip, if any.</param>
        /// <returns>New dropdown menu with an attached text label and enclosing panel.</returns>
        public static UIDropDown AddLabelledDropDown(UIComponent parent, float xPos, float yPos, string name, string text, string tooltip = null, float width = 220f, float height = 24f,float labelXpos = 0f, float labelYpos = -20f, float itemTextScale = 0.875f, int itemHeight = 24, int itemVertPadding = 7)
        {
            // Create dropdown.
            var dropDown = AddDropDown(parent, xPos, yPos, name, tooltip, width, height, itemTextScale, itemHeight, itemVertPadding);

            // Add label.
            var label = dropDown.AddUIComponent<UILabel>();
            label.textScale = 0.8125f;
            label.text = text;
            label.textAlignment = UIHorizontalAlignment.Left;
            label.relativePosition = new Vector2(labelXpos, labelYpos);

            return dropDown;
        }

        /// <summary>
        /// Creates a dropdown menu without text label or enclosing panel.
        /// </summary>
        /// <param name="parent">Parent component.</param>
        /// <param name="xPos">Relative x position (default 20).</param>
        /// <param name="yPos">Relative y position (default 0).</param>
        /// <param name="name">Dropdown name.</param>
        /// <param name="width">Dropdown menu width, excluding label (default 220f).</param>
        /// <param name="height">Dropdown button height (default 25f).</param>
        /// <param name="itemTextScale">Text scaling (default 0.7f).</param>
        /// <param name="itemHeight">Dropdown menu item height (default 20).</param>
        /// <param name="itemVertPadding">Dropdown menu item vertical text padding (default 8).</param>
        /// <param name="tooltip">Tooltip, if any.</param>
        /// <returns>New dropdown menu *without* an attached text label or enclosing panel.</returns>
        public static UIDropDown AddDropDown(UIComponent parent, float xPos, float yPos, string name, string tooltip = null, float width = 220f, float height = 25f, float itemTextScale = 0.875f, int itemHeight = 24, int itemVertPadding = 7)
        {
            // Create dropdown menu.
            var dropDown = parent.AddUIComponent<UIDropDown>();
            dropDown.atlas = TextureUtils.GetAtlas("Ingame");
            dropDown.name = name;
            dropDown.normalBgSprite = "OptionsDropbox";
            dropDown.disabledBgSprite = "OptionsDropboxDisabled";
            dropDown.hoveredBgSprite = "OptionsDropboxHovered";
            dropDown.focusedBgSprite = "OptionsDropboxHovered";
            dropDown.foregroundSpriteMode = UIForegroundSpriteMode.Stretch;
            dropDown.listBackground = "OptionsDropboxListbox";
            dropDown.itemHover = "ListItemHover";
            dropDown.itemHighlight = "ListItemHighlight";
            dropDown.color = Color.white;
            dropDown.popupColor = Color.white;
            dropDown.textColor = Color.white;
            dropDown.popupTextColor = Color.gray;
            dropDown.disabledColor = Color.white;
            dropDown.font = UIFonts.GetUIFont("OpenSans-Regular");
            dropDown.zOrder = 2;
            dropDown.verticalAlignment = UIVerticalAlignment.Middle;
            dropDown.horizontalAlignment = UIHorizontalAlignment.Center;
            dropDown.textFieldPadding = new RectOffset(14, 40, itemVertPadding, 0);
            dropDown.itemPadding = new RectOffset(14, 14, 0, 0);

            dropDown.relativePosition = new Vector2(xPos, yPos);

            // Dropdown size parameters.
            dropDown.size = new Vector2(width, height);
            dropDown.listWidth = (int)width;
            dropDown.listHeight = 500;
            dropDown.itemHeight = itemHeight;
            dropDown.textScale = itemTextScale;
            dropDown.triggerButton = dropDown;

            // Add tooltip.
            if (tooltip != null)
            {
                dropDown.tooltip = tooltip;
            }

            return dropDown;
        }

        /// <summary>
        /// Creates a plain dropdown using the game's option panel dropdown template.
        /// </summary>
        /// <param name="parent">Parent component.</param>
        /// <param name="xPos">Relative x position.</param>
        /// <param name="yPos">Relative y position.</param>
        /// <param name="name">Dropdown name.</param>
        /// <param name="text">Descriptive label text.</param>
        /// <param name="items">Dropdown menu item list.</param>
        /// <param name="selectedIndex">Initially selected index (default 0).</param>
        /// <param name="width">Width of dropdown (default 60).</param>
        /// <returns>New dropdown menu using game's option panel template.</returns>
        public static UIDropDown AddPlainDropDown(UIComponent parent, float xPos, float yPos, string name, string text, string[] items, int selectedIndex = 0, float width = 270f)
        {
            var panel = parent.AttachUIComponent(UITemplateManager.GetAsGameObject("OptionsDropdownTemplate")) as UIPanel;
            var dropDown = panel.Find<UIDropDown>("Dropdown");

            // Set text.
            panel.Find<UILabel>("Label").text = text;

            // Slightly increase width.
            dropDown.autoSize = false;
            dropDown.width = width;

            // Add items.
            dropDown.items = items;
            dropDown.selectedIndex = selectedIndex;

            // Set position.
            dropDown.parent.relativePosition = new Vector2(xPos, yPos);

            // Set name.
            dropDown.name = name;

            return dropDown;
        }
    }
}
