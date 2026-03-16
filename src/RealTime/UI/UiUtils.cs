namespace RealTime.UI
{
    using ColossalFramework.UI;
    using UnityEngine;

    public static class UiUtils
    {
        public static UIButton CreateButton(UIComponent parent, float posX, float posY, string name, string text, string tooltip = null, float width = 230f, float height = 30f, float scale = 0.9f, int vertPad = 4)
        {
            var button = parent.AddUIComponent<UIButton>();

            // Size and position.
            button.size = new Vector2(width, height);
            button.relativePosition = new Vector2(posX, posY);

            // Appearance.
            button.textScale = scale;
            button.normalBgSprite = "ButtonWhite";
            button.hoveredBgSprite = "ButtonWhite";
            button.focusedBgSprite = "ButtonWhite";
            button.pressedBgSprite = "ButtonWhitePressed";
            button.disabledBgSprite = "ButtonWhiteDisabled";
            button.color = Color.white;
            button.focusedColor = Color.white;
            button.hoveredColor = Color.white;
            button.pressedColor = Color.white;
            button.textColor = Color.black;
            button.pressedTextColor = Color.black;
            button.focusedTextColor = Color.black;
            button.hoveredTextColor = Color.blue;
            button.disabledTextColor = Color.grey;
            button.canFocus = false;

            // Add tooltip.
            if (tooltip != null)
            {
                button.tooltip = tooltip;
            }

            // Text.
            button.textScale = scale;
            button.textPadding = new RectOffset(0, 0, vertPad, 0);
            button.textVerticalAlignment = UIVerticalAlignment.Middle;
            button.textHorizontalAlignment = UIHorizontalAlignment.Center;
            button.text = text;
            button.name = name;

            return button;
        }

        public static UICheckBox CreateCheckBox(UIComponent parent, string name, string text, string tooltip, bool state)
        {
            var checkBox = parent.AddUIComponent<UICheckBox>();
            checkBox.name = name;

            checkBox.height = 16f;
            checkBox.width = parent.width - 10f;

            var uncheckedSprite = checkBox.AddUIComponent<UISprite>();
            uncheckedSprite.spriteName = "check-unchecked";
            uncheckedSprite.size = new Vector2(16f, 16f);
            uncheckedSprite.relativePosition = Vector3.zero;

            var checkedSprite = checkBox.AddUIComponent<UISprite>();
            checkedSprite.spriteName = "check-checked";
            checkedSprite.size = new Vector2(16f, 16f);
            checkedSprite.relativePosition = Vector3.zero;
            checkBox.checkedBoxObject = checkedSprite;

            checkBox.label = checkBox.AddUIComponent<UILabel>();
            checkBox.label.text = text;
            checkBox.label.font = GetUIFont("OpenSans-Regular");
            checkBox.label.autoSize = false;
            checkBox.label.height = 20f;
            checkBox.label.verticalAlignment = UIVerticalAlignment.Middle;
            checkBox.label.relativePosition = new Vector3(20f, 0f);
            checkBox.tooltip = tooltip;

            checkBox.isChecked = state;

            return checkBox;
        }

        public static UIFont GetUIFont(string name)
        {
            var fonts = Resources.FindObjectsOfTypeAll<UIFont>();

            foreach (var font in fonts)
            {
                if (font.name.CompareTo(name) == 0)
                {
                    return font;
                }
            }

            return null;
        }

        public static UILabel CreateLabel(UIComponent parent, string name, string text, string prefix)
        {
            var label = parent.AddUIComponent<UILabel>();
            label.name = name;
            label.text = text;
            label.prefix = prefix;

            return label;
        }

        public static UILabel CreateLabel(UIComponent parent, float xPos, float yPos, string text, float width = -1f, float textScale = 1.0f, UIHorizontalAlignment alignment = UIHorizontalAlignment.Left)
        {
            // Add label.
            var label = parent.AddUIComponent<UILabel>();

            // Set sizing options.
            if (width > 0f)
            {
                // Fixed width.
                label.autoSize = false;
                label.width = width;
                label.autoHeight = true;
                label.wordWrap = true;
            }
            else
            {
                // Autosize.
                label.autoSize = true;
                label.autoHeight = false;
                label.wordWrap = false;
            }

            // Alignment.
            label.textAlignment = alignment;

            // Text.
            label.textScale = textScale;
            label.text = text;

            // Position (aligned to right if text alignment is set to right).
            label.relativePosition = new Vector2(alignment == UIHorizontalAlignment.Right ? xPos - label.width : xPos, yPos);

            return label;
        }

        public static UIPanel CreatePanel(UIComponent parent, string name)
        {
            var panel = parent.AddUIComponent<UIPanel>();
            panel.name = name;

            return panel;
        }

        public static UITextField CreateTextField(UIComponent parent, string name, string tooltip)
        {
            var textField = parent.AddUIComponent<UITextField>();
            textField.name = name;
            textField.padding = new RectOffset(0, 0, 9, 3);
            textField.builtinKeyNavigation = true;
            textField.isInteractive = true;
            textField.readOnly = false;
            textField.horizontalAlignment = UIHorizontalAlignment.Center;
            textField.verticalAlignment = UIVerticalAlignment.Middle;
            textField.selectionSprite = "EmptySprite";
            textField.selectionBackgroundColor = new Color32(233, 201, 148, 255);
            textField.normalBgSprite = "TextFieldPanelHovered";
            textField.disabledBgSprite = "TextFieldPanel";
            textField.textColor = new Color32(0, 0, 0, 255);
            textField.disabledTextColor = new Color32(0, 0, 0, 128);
            textField.color = new Color32(185, 221, 254, 255);
            textField.tooltip = tooltip;
            textField.size = new Vector2(50f, 27f);
            textField.padding.top = 6;
            textField.numericalOnly = true;
            textField.allowNegative = false;
            textField.allowFloats = false;
            textField.multiline = false;
            textField.textScale = 1.0f;

            return textField;
        }

        public static Vector3 PositionUnder(UIComponent uIComponent, float margin = 8f, float horizontalOffset = 0f) => new Vector3(uIComponent.relativePosition.x + horizontalOffset, uIComponent.relativePosition.y + uIComponent.height + margin);

        public static Vector3 PositionRightOf(UIComponent uIComponent, float margin = 8f, float verticalOffset = 0f) => new Vector3(uIComponent.relativePosition.x + uIComponent.width + margin, uIComponent.relativePosition.y + verticalOffset);

        public static UIPanel UIServiceBar(UIComponent parent, string name, string text, string prefix, string tooltip)
        {
            float DEFAULT_SCALE = 0.8f;
            // panel
            var m_uiPanel = parent.AddUIComponent<UIPanel>();
            m_uiPanel.name = name;
            m_uiPanel.height = 20f;
            m_uiPanel.width = 440f;

            // text
            string label_name = name + "Label";
            var m_uiTextLabel = CreateLabel(m_uiPanel, label_name, text, prefix);
            m_uiTextLabel.textAlignment = UIHorizontalAlignment.Left;
            m_uiTextLabel.relativePosition = new Vector3(0, 0);
            m_uiTextLabel.textScale = DEFAULT_SCALE;

            // value
            string text_name = name + "TextField";
            var m_uiValueLabel = CreateTextField(m_uiPanel, text_name, tooltip);
            m_uiValueLabel.relativePosition = new Vector3(180f, -6f);

            return m_uiPanel;
        }

        public static UISlider CreateSlider(UIComponent parent, string name, string tooltip, float min, float max, float step, float initial)
        {
            var slider = parent.AddUIComponent<UISlider>();
            slider.name = name;
            slider.maxValue = max;
            slider.minValue = min;
            slider.stepSize = step;

            var slicedSprite = slider.AddUIComponent<UISlicedSprite>();
            slicedSprite.spriteName = "BudgetSlider";
            slicedSprite.relativePosition = Vector3.zero;

            var thumbSprite = slider.AddUIComponent<UISprite>();
            thumbSprite.spriteName = "SliderFill";
            thumbSprite.relativePosition = Vector3.zero;
            slider.thumbObject = thumbSprite;

            slider.value = initial;
            slider.tooltip = tooltip;

            slider.eventSizeChanged += (component, value) => slicedSprite.width = slicedSprite.parent.width;

            return slider;
        }

        public static UISprite CreateSprite(UIComponent parent, float posX, float posY, string name, UITextureAtlas atlas, string spriteName)
        {
            var sprite = parent.AddUIComponent<UISprite>();
            sprite.name = name;
            sprite.relativePosition = new Vector2(posX, posY);
            sprite.atlas = atlas;
            sprite.spriteName = spriteName;
            sprite.size = new Vector2(32f, 32f);

            return sprite;
        }
    }
}
