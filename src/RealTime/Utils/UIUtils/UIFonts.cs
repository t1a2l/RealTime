namespace RealTime.Utils.UIUtils
{
    using ColossalFramework.UI;
    using UnityEngine;

    public static class UIFonts
    {
        public static UIFont GetUIFont(string name)
        {
            var fonts = Resources.FindObjectsOfTypeAll<UIFont>();

            foreach (var font in fonts)
            {
                if (font.name.CompareTo(name) == 0)
                {
                    return font;
                }
            }

            return null;
        }
    }
}
