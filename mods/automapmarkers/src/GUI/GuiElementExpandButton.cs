using System;
using Cairo;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace Egocarib.AutoMapMarkers.GUI
{
    /// <summary>
    /// A button that draws a plus (expand) or minus (collapse) icon, styled to match
    /// GuiElementColorIconButton. Renders in the interactive pass so it scrolls correctly
    /// inside clip regions.
    /// </summary>
    public class GuiElementExpandButton : GuiElementControl
    {
        private readonly Action handler;
        private readonly bool isExpand;
        private LoadedTexture normalTexture;
        private LoadedTexture activeTexture;
        private LoadedTexture hoverTexture;
        private bool isOver;
        private bool currentlyMouseDownOnElement;

        public override bool Focusable => true;

        public GuiElementExpandButton(ICoreClientAPI capi, Action onClick, bool expand, ElementBounds bounds)
            : base(capi, bounds)
        {
            handler = onClick;
            isExpand = expand;
            normalTexture = new LoadedTexture(capi);
            activeTexture = new LoadedTexture(capi);
            hoverTexture = new LoadedTexture(capi);
        }

        public override void ComposeElements(Context ctxStatic, ImageSurface surfaceStatic)
        {
            Bounds.CalcWorldBounds();

            ImageSurface surface = new ImageSurface(Format.Argb32, (int)Bounds.OuterWidth, (int)Bounds.OuterHeight);
            Context ctx = genContext(surface);
            ComposeButton(ctx, surface);
            generateTexture(surface, ref normalTexture);

            // Active overlay (dark)
            ContextUtils.Clear(ctx);
            ctx.SetSourceRGBA(0.0, 0.0, 0.0, 0.4);
            ctx.Rectangle(0.0, 0.0, Bounds.OuterWidth, Bounds.OuterHeight);
            ctx.Fill();
            generateTexture(surface, ref activeTexture);

            // Hover overlay (light)
            ContextUtils.Clear(ctx);
            ctx.SetSourceRGBA(1.0, 1.0, 1.0, 0.1);
            ctx.Rectangle(0.0, 0.0, Bounds.OuterWidth, Bounds.OuterHeight);
            ctx.Fill();
            generateTexture(surface, ref hoverTexture);

            ctx.Dispose();
            surface.Dispose();
        }

        private void ComposeButton(Context ctx, ImageSurface surface)
        {
            double embossHeight = scaled(1.5);

            // Brown background matching ColorIconButton
            Rectangle(ctx, 0.0, 0.0, Bounds.OuterWidth, Bounds.OuterHeight);
            ctx.SetSourceRGBA(69.0 / 255.0, 52.0 / 255.0, 36.0 / 255.0, 0.8);
            ctx.Fill();

            // Top + left emboss (light)
            Rectangle(ctx, 0.0, 0.0, Bounds.OuterWidth - embossHeight, embossHeight);
            ctx.SetSourceRGBA(1.0, 1.0, 1.0, 0.15);
            ctx.Fill();
            Rectangle(ctx, 0.0, embossHeight, embossHeight, Bounds.OuterHeight - embossHeight);
            ctx.SetSourceRGBA(1.0, 1.0, 1.0, 0.15);
            ctx.Fill();

            SurfaceTransformBlur.BlurPartial(surface, 2.0, 5);
            Bounds.CalcWorldBounds();

            // Draw plus or minus icon
            double cx = Bounds.OuterWidth / 2;
            double cy = Bounds.OuterHeight / 2;
            double armLen = Bounds.InnerHeight * 0.25;
            double lineWidth = scaled(2);

            ctx.SetSourceRGBA(GuiStyle.DialogDefaultTextColor);
            ctx.LineWidth = lineWidth;
            ctx.LineCap = LineCap.Round;

            // Horizontal bar (both expand and collapse)
            ctx.MoveTo(cx - armLen, cy);
            ctx.LineTo(cx + armLen, cy);
            ctx.Stroke();

            if (isExpand)
            {
                // Vertical bar (expand only)
                ctx.MoveTo(cx, cy - armLen);
                ctx.LineTo(cx, cy + armLen);
                ctx.Stroke();
            }

            // Bottom + right emboss (dark)
            Rectangle(ctx, embossHeight, Bounds.OuterHeight - embossHeight, Bounds.OuterWidth - 2.0 * embossHeight, embossHeight);
            ctx.SetSourceRGBA(0.0, 0.0, 0.0, 0.2);
            ctx.Fill();
            Rectangle(ctx, Bounds.OuterWidth - embossHeight, 0.0, embossHeight, Bounds.OuterHeight);
            ctx.SetSourceRGBA(0.0, 0.0, 0.0, 0.2);
            ctx.Fill();
        }

        public override void RenderInteractiveElements(float deltaTime)
        {
            api.Render.Render2DTexturePremultipliedAlpha(normalTexture.TextureId, Bounds);
            if (currentlyMouseDownOnElement)
            {
                api.Render.Render2DTexturePremultipliedAlpha(activeTexture.TextureId, Bounds);
            }
            else if (isOver)
            {
                api.Render.Render2DTexturePremultipliedAlpha(hoverTexture.TextureId, Bounds);
            }
        }

        public override void OnMouseMove(ICoreClientAPI api, MouseEvent args)
        {
            isOver = enabled && Bounds.PointInside(api.Input.MouseX, api.Input.MouseY);
        }

        public override void OnMouseDownOnElement(ICoreClientAPI api, MouseEvent args)
        {
            if (enabled)
            {
                base.OnMouseDownOnElement(api, args);
                currentlyMouseDownOnElement = true;
                api.Gui.PlaySound("menubutton_down");
            }
        }

        public override void OnMouseUp(ICoreClientAPI api, MouseEvent args)
        {
            base.OnMouseUp(api, args);
            currentlyMouseDownOnElement = false;
        }

        public override void OnMouseUpOnElement(ICoreClientAPI api, MouseEvent args)
        {
            if (enabled && currentlyMouseDownOnElement && Bounds.PointInside(args.X, args.Y) && args.Button == EnumMouseButton.Left)
            {
                handler?.Invoke();
            }
            currentlyMouseDownOnElement = false;
        }

        public override void Dispose()
        {
            base.Dispose();
            normalTexture?.Dispose();
            activeTexture?.Dispose();
            hoverTexture?.Dispose();
        }
    }

    public static class GuiComposerExpandButtonExtensions
    {
        public static GuiComposer AddExpandButton(this GuiComposer composer, Action onClick, bool expand, ElementBounds bounds, string key = null)
        {
            if (!composer.Composed)
            {
                composer.AddInteractiveElement(new GuiElementExpandButton(composer.Api, onClick, expand, bounds), key);
            }
            return composer;
        }
    }
}
