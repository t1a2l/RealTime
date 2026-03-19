namespace RealTime.Utils.UIUtils
{
    using ColossalFramework.UI;
    using UnityEngine;

    public static class UITextFields
    {
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
    }
}
