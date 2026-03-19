namespace RealTime.Utils.UIUtils
{
    using ColossalFramework.UI;
    using RealTime.Events.Containers;
    using UnityEngine;

    internal class UIFastListLabel : UIPanel, IUIFastListRow
    {
        private UIPanel background;
        private UILabel itemLabel;

        public override void Start()
        {
            base.Start();

            isVisible = true;
            canFocus = true;
            isInteractive = true;
            width = parent.width;
            height = 20;

            background = AddUIComponent<UIPanel>();
            background.width = width;
            background.height = 20;
            background.relativePosition = Vector2.zero;
            background.zOrder = 0;

            itemLabel = AddUIComponent<UILabel>();
            itemLabel.relativePosition = new Vector2(2, 2);
            itemLabel.textScale = 0.7f;
        }

        protected override void OnClick(UIMouseEventParameter p) => base.OnClick(p);

        public void Display(object data, bool isRowOdd)
        {
            if (data != null)
            {
                if (data is LabelOptionItem option && option.readableLabel != null && option.readableLabel != "" && background != null)
                {
                    itemLabel.name = option.readableLabel;
                    itemLabel.text = option.readableLabel;
                    itemLabel.autoSize = false;
                    itemLabel.autoHeight = false;
                    itemLabel.width = width;
                    itemLabel.height = 20;
                    itemLabel.textAlignment = UIHorizontalAlignment.Left;
                    itemLabel.verticalAlignment = UIVerticalAlignment.Middle;

                    if (isRowOdd)
                    {
                        background.backgroundSprite = "UnlockingItemBackground";
                        background.color = new Color32(0, 0, 0, 128);
                    }
                    else
                    {
                        background.backgroundSprite = null;
                    }
                }
            }
        }

        public void Select(bool isRowOdd)
        {
            if (itemLabel != null && background != null)
            {
                background.backgroundSprite = "ListItemHighlight";
                background.color = new Color32(255, 255, 255, 255);
            }
        }

        public void Deselect(bool isRowOdd)
        {
            if (itemLabel != null && background != null)
            {
                if (isRowOdd)
                {
                    background.backgroundSprite = "UnlockingItemBackground";
                    background.color = new Color32(0, 0, 0, 128);
                }
                else
                {
                    background.backgroundSprite = null;
                }
            }
        }

    }
}
