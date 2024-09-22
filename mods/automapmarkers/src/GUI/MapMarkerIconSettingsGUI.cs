using Egocarib.AutoMapMarkers.Settings;
using Vintagestory.API.Client;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Egocarib.AutoMapMarkers.GUI
{
    using AutoMapMarkerSetting = MapMarkerConfig.Settings.AutoMapMarkerSetting;

    public class MapMarkerIconSettingsGUI : GuiDialogGeneric
    {
        private int[] colors;
        private readonly string[] icons;
        private readonly string iconTitle;
        private readonly AutoMapMarkerSetting settings;
        private const string GuiPrefix = "auto-markers-single-icon";
        private int selectedIconIndex = -1;
        private int selectedColorIndex = -1;
        public override double DrawOrder => 0.2;
        public override bool DisableMouseGrab => true;

        public MapMarkerIconSettingsGUI(ICoreClientAPI capi, string iconTitle, AutoMapMarkerSetting iconSettings, string[] icons, int[] colors)
            : base("", capi)
        {
            this.icons = icons;
            this.colors = colors;
            this.iconTitle = iconTitle;
            this.settings = iconSettings;
            ComposeDialog();
        }

        public override bool TryOpen()
        {
            ComposeDialog();
            return base.TryOpen();
        }

        private void ComposeDialog()
        {
            ElementBounds leftColumn = ElementBounds.Fixed(0.0, 28.0, 120.0, 25.0);
            ElementBounds rightColumn = leftColumn.RightCopy();
            ElementBounds buttonRow = ElementBounds.Fixed(0.0, 28.0, 360.0, 25.0);
            ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
            bgBounds.BothSizing = ElementSizing.FitToChildren;
            bgBounds.WithChildren(leftColumn, rightColumn);
            ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.CenterMiddle).WithFixedAlignmentOffset(0.0 - GuiStyle.DialogToScreenPadding, 0.0);
            if (SingleComposer != null)
                SingleComposer.Dispose();
            int colorIconSize = 22;
            selectedIconIndex = icons.IndexOf(settings.MarkerIcon);
            if (selectedIconIndex < 0)
                selectedIconIndex = 0;
            int? currentColor = settings.MarkerColorIntegerNoAlpha;
            selectedColorIndex = currentColor == null ? 0 : colors.IndexOf(currentColor.Value);
            if (selectedColorIndex < 0)
            {
                colors = ArrayExtensions.Append(colors, currentColor.Value);
                selectedColorIndex = colors.Length - 1;
            }
            SingleComposer = capi.Gui.CreateCompo($"{GuiPrefix}-config", dialogBounds)
                .AddShadedDialogBG(bgBounds, withTitleBar: false)
                .AddDialogTitleBar(iconTitle, delegate { TryClose(); })
                .BeginChildElements(bgBounds)
                    .AddStaticText(Lang.Get("Name"), CairoFont.WhiteSmallText(), leftColumn = leftColumn.FlatCopy())
                    .AddTextInput(rightColumn = rightColumn.FlatCopy().WithFixedWidth(200.0), OnNameChanged, CairoFont.TextInput(), $"{GuiPrefix}-nameInput")
                    .AddStaticText(Lang.Get("egocarib-mapmarkers:should-pin"), CairoFont.WhiteSmallText(), leftColumn = leftColumn.BelowCopy(0.0, 9.0))
                    .AddSwitch(OnPinnedToggled, rightColumn = rightColumn.BelowCopy(0.0, 5.0).WithFixedWidth(200.0), $"{GuiPrefix}-pinnedSwitch")
                    .AddRichtext(Lang.Get("waypoint-color"), CairoFont.WhiteSmallText(), leftColumn = leftColumn.BelowCopy(0.0, 5.0))
                    .AddColorListPicker(colors, OnColorSelected, leftColumn = leftColumn.BelowCopy(0.0, 5.0).WithFixedSize(colorIconSize, colorIconSize), 270, $"{GuiPrefix}-colorPicker")
                    .AddStaticText(Lang.Get("Icon"), CairoFont.WhiteSmallText(), leftColumn = leftColumn.WithFixedPosition(0.0, leftColumn.fixedY + leftColumn.fixedHeight).WithFixedWidth(100.0).BelowCopy())
                    .AddIconListPicker(icons, OnIconSelected, leftColumn = leftColumn.BelowCopy(0.0, 5.0).WithFixedSize(colorIconSize + 5, colorIconSize + 5), 270, $"{GuiPrefix}-iconPicker")
                    .AddSmallButton(Lang.Get("Cancel"), OnCancel, buttonRow.FlatCopy().FixedUnder(leftColumn).WithFixedWidth(100.0))
                    .AddSmallButton(Lang.Get("Save"), OnSave, buttonRow.FlatCopy().FixedUnder(leftColumn).WithFixedWidth(100.0).WithAlignment(EnumDialogArea.RightFixed), EnumButtonStyle.Normal, $"{GuiPrefix}-saveButton")
                .EndChildElements()
                .Compose();
            SingleComposer.ColorListPickerSetValue($"{GuiPrefix}-colorPicker", selectedColorIndex);
            SingleComposer.IconListPickerSetValue($"{GuiPrefix}-iconPicker", selectedIconIndex);
            SingleComposer.GetTextInput($"{GuiPrefix}-nameInput").SetValue(settings.MarkerTitle);
            SingleComposer.GetSwitch($"{GuiPrefix}-pinnedSwitch").SetValue(settings.MarkerPinned);
        }

        private void OnIconSelected(int index)
        {
            selectedIconIndex = index;
        }

        private void OnColorSelected(int index)
        {
            selectedColorIndex = index;
        }

        private void OnPinnedToggled(bool t1)
        {
        }

        private bool OnSave()
        {
            settings.MarkerIcon = icons[selectedIconIndex];
            settings.MarkerColor = ColorUtil.Int2Hex(colors[selectedColorIndex]);
            settings.MarkerTitle = SingleComposer.GetTextInput($"{GuiPrefix}-nameInput").GetText();
            settings.MarkerPinned = SingleComposer.GetSwitch($"{GuiPrefix}-pinnedSwitch").On;
            TryClose();
            return true;
        }

        private bool OnCancel()
        {
            TryClose();
            return true;
        }

        private void OnNameChanged(string t1)
        {
            SingleComposer.GetButton($"{GuiPrefix}-saveButton").Enabled = t1.Trim() != "";
        }

        public override bool CaptureAllInputs()
        {
            return IsOpened();
        }

        public override void OnMouseDown(MouseEvent args)
        {
            base.OnMouseDown(args);
            args.Handled = true;
        }

        public override void OnMouseUp(MouseEvent args)
        {
            base.OnMouseUp(args);
            args.Handled = true;
        }

        public override void OnMouseMove(MouseEvent args)
        {
            base.OnMouseMove(args);
            args.Handled = true;
        }

        public override void OnMouseWheel(MouseWheelEventArgs args)
        {
            base.OnMouseWheel(args);
            args.SetHandled();
        }
    }
}
