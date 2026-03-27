namespace RealTime.Events.Containers
{
    using ColossalFramework.UI;
    using RealTime.Utils.UIUtils;
    using UnityEngine;

    internal class UpcomingEventRow : UIPanel, IUIFastListRow
    {
        private UIPanel background;
        private UILabel nameLabel;
        private UILabel timeLabel;
        private UIButton deleteBtn;

        public override void Start()
        {
            base.Start();
            isVisible = true;
            canFocus = true;
            isInteractive = true;
            width = parent.width;
            height = 30f;  // Matches rowHeight

            // Background
            background = AddUIComponent<UIPanel>();
            background.width = width;
            background.height = height;
            background.relativePosition = Vector2.zero;
            background.zOrder = 0;
            background.backgroundSprite = "GenericPanel";
            background.color = new Color32(91, 97, 106, 128);

            // Name label
            nameLabel = AddUIComponent<UILabel>();
            nameLabel.autoSize = false;
            nameLabel.width = 180;
            nameLabel.height = height;
            nameLabel.relativePosition = new Vector2(8, 0);
            nameLabel.textScale = 0.85f;
            nameLabel.verticalAlignment = UIVerticalAlignment.Middle;
            nameLabel.textAlignment = UIHorizontalAlignment.Left;

            // Time label
            timeLabel = AddUIComponent<UILabel>();
            timeLabel.autoSize = false;
            timeLabel.width = 100;
            timeLabel.height = height;
            timeLabel.relativePosition = new Vector2(195, 0);
            timeLabel.textScale = 0.85f;
            timeLabel.verticalAlignment = UIVerticalAlignment.Middle;
            timeLabel.textAlignment = UIHorizontalAlignment.Left;

            // Delete X button
            deleteBtn = AddUIComponent<UIButton>();
            deleteBtn.text = "✕";
            deleteBtn.size = new Vector2(25, 25);
            deleteBtn.relativePosition = new Vector2(width - 35, (height - 25) / 2);
            deleteBtn.textScale = 1.1f;
            deleteBtn.normalBgSprite = "SmallButton";
            deleteBtn.hoveredBgSprite = "SmallButtonHovered";
            deleteBtn.pressedBgSprite = "SmallButtonPressed";
            deleteBtn.textColor = new Color32(255, 100, 100, 255);
        }

        public void Display(object data, bool isRowOdd)
        {
            if (data is UpcomingEventItem item)
            {
                nameLabel.text = item.eventName;
                timeLabel.text = item.timeStr;
                deleteBtn.eventClick -= OnDeleteClick;  // Clear previous
                deleteBtn.eventClick += OnDeleteClick;
                deleteBtn.objectUserData = item;  // Pass data

                if (isRowOdd)
                {
                    background.color = new Color32(70, 77, 86, 180);
                }
                else
                {
                    background.color = new Color32(91, 97, 106, 128);
                }
            }
        }

        public void Select(bool isRowOdd) => background.color = new Color32(100, 150, 255, 200);

        public void Deselect(bool isRowOdd) =>
            // Revert to odd/even colors
            background.color = isRowOdd ? new Color32(70, 77, 86, 180) : new Color32(91, 97, 106, 128);

        private void OnDeleteClick(UIComponent component, UIMouseEventParameter eventParam)
        {
            if (component.objectUserData is UpcomingEventItem item && item.deleteAction != null)
            {
                item.deleteAction();
            }
        }
    }
}
