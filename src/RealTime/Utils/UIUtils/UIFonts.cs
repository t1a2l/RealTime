namespace RealTime.Utils.UIUtils
{
    using System.Linq;
    using ColossalFramework.UI;
    using UnityEngine;

    public static class UIFonts
    {

        /// <summary>
        /// Gets the regular sans-serif font.
        /// </summary>
        public static UIFont Regular
        {
            get
            {
                if (field == null)
                {
                    field = Resources.FindObjectsOfTypeAll<UIFont>().FirstOrDefault((UIFont f) => f.name == "OpenSans-Regular");
                }

                return field;
            }
        }

        /// <summary>
        /// Gets the semi-bold sans-serif font.
        /// </summary>
        public static UIFont SemiBold
        {
            get
            {
                if (field == null)
                {
                    field = Resources.FindObjectsOfTypeAll<UIFont>().FirstOrDefault((UIFont f) => f.name == "OpenSans-Semibold");
                }

                return field;
            }
        }

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
