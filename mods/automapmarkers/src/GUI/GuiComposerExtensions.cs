using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Egocarib.AutoMapMarkers.GUI
{
    using Color = System.Drawing.Color;

    public static class GuiComposerExtensions
    {
        /// <summary>
        /// Adds a color icon button to an existing GUI composer
        /// </summary>
        public static GuiComposer AddColorIconButton(this GuiComposer composer, string icon, string iconColor, ActionConsumable onClick, ElementBounds bounds, EnumButtonStyle style = EnumButtonStyle.Normal, string key = null)
        {
            if (composer.Composed)
                return composer;

            Color color = Color.FromName(iconColor);
            int? parsedColor = (color.A == byte.MaxValue) ? color.ToArgb() : null;
            double[] colorVals = parsedColor.HasValue ? ColorUtil.ToRGBADoubles(parsedColor.Value) : GuiStyle.DialogDefaultTextColor;
            return AddColorIconButton(composer, icon, colorVals, onClick, bounds, style, key);
        }


        /// <summary>
        /// Adds a color icon button to an existing GUI composer
        /// </summary>
        public static GuiComposer AddColorIconButton(this GuiComposer composer, string icon, double[] iconColor, ActionConsumable onClick, ElementBounds bounds, EnumButtonStyle style = EnumButtonStyle.Normal, string key = null)
        {
            if (!composer.Composed && iconColor != null)
            {
                GuiElementColorIconButton elem = new GuiElementColorIconButton(
                    capi: composer.Api,
                    icon: icon,
                    iconColor: iconColor,
                    onClick: onClick,
                    bounds: bounds,
                    style: style
                );
                composer.AddInteractiveElement(elem, key);
            }
            return composer;
        }
    }
}
