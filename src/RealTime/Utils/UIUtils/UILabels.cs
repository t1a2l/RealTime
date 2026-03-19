namespace RealTime.Utils.UIUtils
{
    using ColossalFramework.UI;
    using UnityEngine;

    public static class UILabels
    {
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
    }
}
