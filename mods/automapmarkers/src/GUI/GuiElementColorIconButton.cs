using Cairo;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace Egocarib.AutoMapMarkers.GUI
{
    public class GuiElementColorIconButton : GuiElementControl
    {
        private LoadedTexture normalTexture;
        private LoadedTexture activeTexture;
        private LoadedTexture hoverTexture;
        private LoadedTexture disabledTexture;
        private ActionConsumable onClick;
        private bool isOver;
        private EnumButtonStyle buttonStyle;
        private bool active;
        private bool currentlyMouseDownOnElement;
        public bool PlaySound = true;
        //public static double Padding = 2.0; //TODO: is this used?
        public bool Visible = true;
        private string icon;
        private double[] iconColor;

        public override bool Focusable => true;

        /// <summary>
        /// Creates a button with icon and color indicators
        /// </summary>
        public GuiElementColorIconButton(ICoreClientAPI capi, string icon, double[] iconColor, ActionConsumable onClick, ElementBounds bounds, EnumButtonStyle style = EnumButtonStyle.Normal)
            : base(capi, bounds)
        {
            hoverTexture = new LoadedTexture(capi);
            activeTexture = new LoadedTexture(capi);
            normalTexture = new LoadedTexture(capi);
            disabledTexture = new LoadedTexture(capi);
            buttonStyle = style;
            this.icon = icon;
            this.iconColor = iconColor ?? GuiStyle.DialogDefaultTextColor;
            this.onClick = onClick;
        }

        public override void ComposeElements(Context ctxStatic, ImageSurface surfaceStatic)
        {
            Bounds.CalcWorldBounds();
            ImageSurface surface = new ImageSurface(Format.Argb32, (int)Bounds.OuterWidth, (int)Bounds.OuterHeight);
            Context ctx = genContext(surface);
            ComposeButton(ctx, surface);
            generateTexture(surface, ref normalTexture);
            ContextUtils.Clear(ctx);
            if (buttonStyle != EnumButtonStyle.None)
            {
                ctx.SetSourceRGBA(0.0, 0.0, 0.0, 0.4);
                ctx.Rectangle(0.0, 0.0, Bounds.OuterWidth, Bounds.OuterHeight);
                ctx.Fill();
            }
            generateTexture(surface, ref activeTexture);
            ContextUtils.Clear(ctx);
            if (buttonStyle != EnumButtonStyle.None)
            {
                ctx.SetSourceRGBA(1.0, 1.0, 1.0, 0.1);
                ctx.Rectangle(0.0, 0.0, Bounds.OuterWidth, Bounds.OuterHeight);
                ctx.Fill();
            }
            generateTexture(surface, ref hoverTexture);
            ctx.Dispose();
            surface.Dispose();
            surface = new ImageSurface(Format.Argb32, 2, 2);
            ctx = genContext(surface);
            if (buttonStyle != EnumButtonStyle.None)
            {
                ctx.SetSourceRGBA(0.0, 0.0, 0.0, 0.4);
                ctx.Rectangle(0.0, 0.0, 2.0, 2.0);
                ctx.Fill();
            }
            generateTexture(surface, ref disabledTexture);
            ctx.Dispose();
            surface.Dispose();
        }

        private void ComposeButton(Context ctx, ImageSurface surface)
        {
            double embossHeight = GuiElement.scaled(2.5);
            if (buttonStyle == EnumButtonStyle.Normal || buttonStyle == EnumButtonStyle.Small)
            {
                embossHeight = GuiElement.scaled(1.5);
            }
            if (buttonStyle != EnumButtonStyle.None)
            {
                GuiElement.Rectangle(ctx, 0.0, 0.0, Bounds.OuterWidth, Bounds.OuterHeight);
                ctx.SetSourceRGBA(69.0 / 255.0, 52.0 / 255.0, 36.0 / 255.0, 0.8);
                ctx.Fill();
            }
            if (buttonStyle == EnumButtonStyle.MainMenu)
            {
                GuiElement.Rectangle(ctx, 0.0, 0.0, Bounds.OuterWidth, embossHeight);
                ctx.SetSourceRGBA(1.0, 1.0, 1.0, 0.15);
                ctx.Fill();
            }
            if (buttonStyle == EnumButtonStyle.Normal || buttonStyle == EnumButtonStyle.Small)
            {
                GuiElement.Rectangle(ctx, 0.0, 0.0, Bounds.OuterWidth - embossHeight, embossHeight);
                ctx.SetSourceRGBA(1.0, 1.0, 1.0, 0.15);
                ctx.Fill();
                GuiElement.Rectangle(ctx, 0.0, 0.0 + embossHeight, embossHeight, Bounds.OuterHeight - embossHeight);
                ctx.SetSourceRGBA(1.0, 1.0, 1.0, 0.15);
                ctx.Fill();
            }
            SurfaceTransformBlur.BlurPartial(surface, 2.0, 5);
            Bounds.CalcWorldBounds();
            if (icon != null && icon.Length > 0)
            {
                double x = Bounds.absPaddingX + GuiElement.scaled(8.0);
                double y = Bounds.absPaddingY + GuiElement.scaled(5.0);
                double h = Bounds.InnerHeight - GuiElement.scaled(11.0);
                double w = h;

                //MessageUtil.Log($"drawing icon: {icon} [x:{Bounds.absPaddingX + GuiElement.scaled(4.0)} y:{Bounds.absPaddingY + GuiElement.scaled(4.0)} w:{Bounds.InnerWidth - GuiElement.scaled(9.0)} h:{Bounds.InnerHeight - GuiElement.scaled(9.0)} argb: {iconColor.ToString()}");
                api.Gui.Icons.DrawIcon(ctx, icon, x, y, w, h, GuiStyle.DialogDefaultTextColor);

                //Draw color swatch
                x += w + GuiElement.scaled(10.0);
                ctx.Rectangle(x, y, w, h);
                ctx.SetSourceRGBA(iconColor);
                ctx.FillPreserve();
                ctx.SetSourceRGBA(GuiStyle.DialogBorderColor);
                ctx.Stroke();
            }
            if (buttonStyle == EnumButtonStyle.MainMenu)
            {
                GuiElement.Rectangle(ctx, 0.0, 0.0 + Bounds.OuterHeight - embossHeight, Bounds.OuterWidth, embossHeight);
                ctx.SetSourceRGBA(0.0, 0.0, 0.0, 0.2);
                ctx.Fill();
            }
            if (buttonStyle == EnumButtonStyle.Normal || buttonStyle == EnumButtonStyle.Small)
            {
                GuiElement.Rectangle(ctx, 0.0 + embossHeight, 0.0 + Bounds.OuterHeight - embossHeight, Bounds.OuterWidth - 2.0 * embossHeight, embossHeight);
                ctx.SetSourceRGBA(0.0, 0.0, 0.0, 0.2);
                ctx.Fill();
                GuiElement.Rectangle(ctx, 0.0 + Bounds.OuterWidth - embossHeight, 0.0, embossHeight, Bounds.OuterHeight);
                ctx.SetSourceRGBA(0.0, 0.0, 0.0, 0.2);
                ctx.Fill();
            }
        }

        public override void RenderInteractiveElements(float deltaTime)
        {
            if (Visible)
            {
                api.Render.Render2DTexturePremultipliedAlpha(normalTexture.TextureId, Bounds);
                if (!enabled)
                {
                    api.Render.Render2DTexturePremultipliedAlpha(disabledTexture.TextureId, Bounds);
                }
                else if (active || currentlyMouseDownOnElement)
                {
                    api.Render.Render2DTexturePremultipliedAlpha(activeTexture.TextureId, Bounds);
                }
                else if (isOver)
                {
                    api.Render.Render2DTexturePremultipliedAlpha(hoverTexture.TextureId, Bounds);
                }
            }
        }

        public override void OnKeyDown(ICoreClientAPI api, KeyEvent args)
        {
            if (!Visible || !base.HasFocus || args.KeyCode != 49)
            {
                return;
            }
            args.Handled = true;
            if (enabled)
            {
                if (PlaySound)
                {
                    api.Gui.PlaySound("menubutton_press");
                }
                args.Handled = onClick();
            }
        }

        public override void OnMouseMove(ICoreClientAPI api, MouseEvent args)
        {
            bool flag = isOver;
            setIsOver();
            if (!flag && isOver && PlaySound)
            {
                api.Gui.PlaySound("menubutton");
            }
        }

        protected void setIsOver()
        {
            isOver = Visible && enabled && Bounds.PointInside(api.Input.MouseX, api.Input.MouseY);
        }

        public override void OnMouseDownOnElement(ICoreClientAPI api, MouseEvent args)
        {
            if (Visible && enabled)
            {
                base.OnMouseDownOnElement(api, args);
                currentlyMouseDownOnElement = true;
                if (PlaySound)
                {
                    api.Gui.PlaySound("menubutton_down");
                }
                setIsOver();
            }
        }

        public override void OnMouseUp(ICoreClientAPI api, MouseEvent args)
        {
            if (Visible)
            {
                if (currentlyMouseDownOnElement && !Bounds.PointInside(args.X, args.Y) && !active && PlaySound)
                {
                    api.Gui.PlaySound("menubutton_up");
                }
                base.OnMouseUp(api, args);
                currentlyMouseDownOnElement = false;
            }
        }

        public override void OnMouseUpOnElement(ICoreClientAPI api, MouseEvent args)
        {
            if (enabled && currentlyMouseDownOnElement && Bounds.PointInside(args.X, args.Y) && args.Button == EnumMouseButton.Left)
            {
                args.Handled = onClick();
            }
            currentlyMouseDownOnElement = false;
        }

        /// <summary>
        /// Sets the button as active or inactive.
        /// </summary>
        /// <param name="active">Active == clickable</param>
        public void SetActive(bool active)
        {
            this.active = active;
        }

        public override void Dispose()
        {
            base.Dispose();
            hoverTexture?.Dispose();
            activeTexture?.Dispose();
            disabledTexture?.Dispose();
            normalTexture?.Dispose();
        }
    }
}
