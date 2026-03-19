namespace RealTime.Utils.UIUtils
{
    using ColossalFramework.UI;
    using UnityEngine;

    public static class UIPanels
    {
        public static UIPanel CreatePanel(UIComponent parent, string name)
        {
            var panel = parent.AddUIComponent<UIPanel>();
            panel.name = name;

            return panel;
        }

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
            var m_uiTextLabel = UILabels.CreateLabel(m_uiPanel, label_name, text, prefix);
            m_uiTextLabel.textAlignment = UIHorizontalAlignment.Left;
            m_uiTextLabel.relativePosition = new Vector3(0, 0);
            m_uiTextLabel.textScale = DEFAULT_SCALE;

            // value
            string text_name = name + "TextField";
            var m_uiValueLabel = UITextFields.CreateTextField(m_uiPanel, text_name, tooltip);
            m_uiValueLabel.relativePosition = new Vector3(180f, -6f);

            return m_uiPanel;
        }
    }
}
