namespace RealTime.Utils.UIUtils
{
    using ColossalFramework.UI;
    using UnityEngine;

    public static class UIPositions
    {
        public static Vector3 PositionUnder(UIComponent uIComponent, float margin = 8f, float horizontalOffset = 0f) => new(uIComponent.relativePosition.x + horizontalOffset, uIComponent.relativePosition.y + uIComponent.height + margin);

        public static Vector3 PositionRightOf(UIComponent uIComponent, float margin = 8f, float verticalOffset = 0f) => new(uIComponent.relativePosition.x + uIComponent.width + margin, uIComponent.relativePosition.y + verticalOffset);

    }
}
