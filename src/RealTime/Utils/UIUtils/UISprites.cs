namespace RealTime.Utils.UIUtils
{
    using ColossalFramework.UI;
    using UnityEngine;

    public static class UISprites
    {
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
