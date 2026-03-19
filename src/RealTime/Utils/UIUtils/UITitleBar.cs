namespace RealTime.Utils.UIUtils
{
    using ColossalFramework.UI;
    using UnityEngine;

    public class UITitleBar : UIPanel
    {
        private UISprite m_icon;
        private UILabel m_title;
        private UIDragHandle m_drag;

        public string iconSprite
        {
            get => m_icon.spriteName;
            set
            {
                if (m_icon != null)
                {
                    m_icon.spriteName = value;

                    if (m_icon.atlas == null)
                    {
                        m_icon.atlas = TextureUtils.GetAtlas("Ingame");
                    }

                    if (m_icon.spriteInfo != null)
                    {
                        m_icon.size = m_icon.spriteInfo.pixelSize;
                        ResizeIcon(m_icon, new Vector2(32, 32));
                        m_icon.relativePosition = new Vector3(10, 5);
                    }
                }
            }
        }

        public UITextureAtlas iconAtlas
        {
            get => m_icon.atlas;
            set => m_icon.atlas = value;
        }

        public UIButton closeButton { get; private set; }

        public string title
        {
            get => m_title.text;
            set => m_title.text = value;
        }


        public override void Awake()
        {
            base.Awake();

            m_icon = AddUIComponent<UISprite>();
            m_title = AddUIComponent<UILabel>();
            closeButton = AddUIComponent<UIButton>();
            m_drag = AddUIComponent<UIDragHandle>();

            height = 40;
            width = 450;
            title = "(None)";
            iconSprite = "";
        }

        public override void Start()
        {
            base.Start();

            width = parent.width;
            relativePosition = Vector3.zero;
            isVisible = true;
            canFocus = true;
            isInteractive = true;

            m_drag.width = width - 50;
            m_drag.height = height;
            m_drag.relativePosition = Vector3.zero;
            m_drag.target = parent;

            m_icon.spriteName = iconSprite;
            m_icon.relativePosition = new Vector3(10, 5);

            m_title.relativePosition = new Vector3(50, 13);
            m_title.text = title;
            m_title.autoSize = false;
            m_title.textAlignment = UIHorizontalAlignment.Center;
            m_title.verticalAlignment = UIVerticalAlignment.Middle;

            closeButton.atlas = TextureUtils.GetAtlas("Ingame");
            closeButton.relativePosition = new Vector3(width - 35, 2);
            closeButton.normalBgSprite = "buttonclose";
            closeButton.hoveredBgSprite = "buttonclosehover";
            closeButton.pressedBgSprite = "buttonclosepressed";
            closeButton.eventClick += (component, param) => parent.Hide();

            m_title.width = parent.width - m_title.relativePosition.x - closeButton.width - 10;
        }

        public void ResizeIcon(UISprite icon, Vector2 maxSize)
        {
            if (icon.height == 0)
            {
                return;
            }

            float ratio = icon.width / icon.height;

            if (icon.width > maxSize.x)
            {
                icon.width = maxSize.x;
                icon.height = maxSize.x / ratio;
            }

            if (icon.height > maxSize.y)
            {
                icon.height = maxSize.y;
                icon.width = maxSize.y * ratio;
            }
        }
    }
}
