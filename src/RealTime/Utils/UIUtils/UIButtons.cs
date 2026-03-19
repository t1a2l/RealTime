namespace RealTime.Utils.UIUtils
{
    using ColossalFramework.UI;
    using UnityEngine;

    public static class UIButtons
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
    }
}
