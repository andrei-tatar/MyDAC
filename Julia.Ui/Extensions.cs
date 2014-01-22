using System;
using System.ComponentModel;
using System.Linq;

namespace Julia.Ui
{
    public static class Extensions
    {
        public static string GetDescription(this Enum obj)
        {
            try
            {
                var fieldInfo = obj.GetType().GetField(obj.ToString());
                var descriptionAttribute = fieldInfo.GetCustomAttributes(false).OfType<DescriptionAttribute>().FirstOrDefault();
                return descriptionAttribute != null ? descriptionAttribute.Description : obj.ToString();
            }
            catch (NullReferenceException)
            {
                return "Unknown";
            }
        }

        public static SlideDirection Reverse(this SlideDirection slide)
        {
            switch (slide)
            {
                case SlideDirection.Left: return SlideDirection.Right;
                case SlideDirection.Right: return SlideDirection.Left;
                case SlideDirection.Top: return SlideDirection.Bottom;
                case SlideDirection.Bottom: return SlideDirection.Top;
                default: return SlideDirection.None;
            }
        }
    }
}
