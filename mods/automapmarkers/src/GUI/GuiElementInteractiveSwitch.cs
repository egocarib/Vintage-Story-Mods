using System;
using Cairo;
using Vintagestory.API.Client;

namespace Egocarib.AutoMapMarkers.GUI
{
    /// <summary>
    /// A switch/checkbox element that renders entirely in the interactive pass,
    /// so it scrolls correctly inside clip regions. Visually matches the stock
    /// GuiElementSwitch appearance.
    /// </summary>
    public class GuiElementInteractiveSwitch : GuiElementControl
    {
        public bool On;

        private Action<bool> handler;
        private LoadedTexture bgTexture;
        private LoadedTexture onTexture;

        private const double unscaledSize = 30;
        private const double unscaledPadding = 4;

        public override bool Focusable => true;

        public GuiElementInteractiveSwitch(ICoreClientAPI capi, Action<bool> onToggle, ElementBounds bounds)
            : base(capi, bounds)
        {
            handler = onToggle;
            bgTexture = new LoadedTexture(capi);
            onTexture = new LoadedTexture(capi);
        }

        public override void ComposeElements(Context ctxStatic, ImageSurface surfaceStatic)
        {
            Bounds.CalcWorldBounds();

            // Generate background texture (dark inset rectangle)
            double size = scaled(unscaledSize);
            ImageSurface surface = new ImageSurface(Format.Argb32, (int)size, (int)size);
            Context ctx = genContext(surface);

            RoundRectangle(ctx, 0, 0, size, size, 1);
            ctx.SetSourceRGBA(0, 0, 0, 0.2);
            ctx.Fill();
            // Use EmbossRoundRectangle directly with higher intensity and no alpha offset
            // to compensate for drawing on a transparent surface instead of the opaque dialog bg.
            EmbossRoundRectangle(ctx, 0, 0, size, size, 1, 2, 0.9f, 0.8f, true, 0f);

            generateTexture(surface, ref bgTexture);
            ctx.Dispose();
            surface.Dispose();

            // Generate "on" indicator texture
            GenOnTexture();
        }

        private void GenOnTexture()
        {
            double padding = scaled(unscaledPadding);
            double innerSize = scaled(unscaledSize - 2 * unscaledPadding);
            ImageSurface surface = new ImageSurface(Format.Argb32, (int)innerSize, (int)innerSize);
            Context ctx = genContext(surface);

            RoundRectangle(ctx, 0, 0, innerSize, innerSize, 1);
            ctx.SetSourceRGBA(0, 0, 0, 1);
            ctx.FillPreserve();
            fillWithPattern(api, ctx, waterTextureName, false, true, 255, 0.5f);

            generateTexture(surface, ref onTexture);
            ctx.Dispose();
            surface.Dispose();
        }

        public override void RenderInteractiveElements(float deltaTime)
        {
            // Render background at current scrolled position
            double size = scaled(unscaledSize);
            api.Render.Render2DTexturePremultipliedAlpha(bgTexture.TextureId,
                (int)Bounds.renderX, (int)Bounds.renderY, (int)size, (int)size);

            // Render "on" indicator if toggled on
            if (On)
            {
                double padding = scaled(unscaledPadding);
                api.Render.Render2DTexturePremultipliedAlpha(onTexture.TextureId,
                    (int)(Bounds.renderX + padding), (int)(Bounds.renderY + padding),
                    (int)scaled(unscaledSize - 2 * unscaledPadding),
                    (int)scaled(unscaledSize - 2 * unscaledPadding));
            }
        }

        public override void OnMouseDownOnElement(ICoreClientAPI api, MouseEvent args)
        {
            base.OnMouseDownOnElement(api, args);
            if (!enabled) return;
            On = !On;
            handler?.Invoke(On);
            api.Gui.PlaySound("toggleswitch");
        }

        public void SetValue(bool on)
        {
            On = on;
        }

        public override void Dispose()
        {
            base.Dispose();
            bgTexture?.Dispose();
            onTexture?.Dispose();
        }
    }

    public static class GuiComposerInteractiveSwitchExtensions
    {
        /// <summary>
        /// Adds an interactive switch that scrolls correctly inside clip regions.
        /// </summary>
        public static GuiComposer AddInteractiveSwitch(this GuiComposer composer, Action<bool> onToggle, ElementBounds bounds, string key = null)
        {
            if (!composer.Composed)
            {
                GuiElementInteractiveSwitch elem = new GuiElementInteractiveSwitch(composer.Api, onToggle, bounds);
                composer.AddInteractiveElement(elem, key);
            }
            return composer;
        }

        /// <summary>
        /// Gets an interactive switch by key.
        /// </summary>
        public static GuiElementInteractiveSwitch GetInteractiveSwitch(this GuiComposer composer, string key)
        {
            return (GuiElementInteractiveSwitch)composer.GetElement(key);
        }
    }
}
