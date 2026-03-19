namespace RealTime.Utils.UIUtils
{
    using ColossalFramework.UI;
    using UnityEngine;

    public static class UISliders
    {
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
    }
}
