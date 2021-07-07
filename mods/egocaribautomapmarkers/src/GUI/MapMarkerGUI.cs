using Cairo;
using System;
using System.Collections.Generic;
using System.Globalization;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Datastructures;
using System.Linq;
using Egocarib.AutoMapMarkers.Settings;

namespace Egocarib.AutoMapMarkers.GUI
{
    using Color = System.Drawing.Color;
    using AutoMapMarkerSetting = MapMarkerConfig.Settings.AutoMapMarkerSetting;

    public class MapMarkerGUISystem : ModSystem
    {
        ICoreClientAPI capi;
        GuiDialog mapMarkerDialog;

        public override bool ShouldLoad(EnumAppSide forSide)
        {
            return forSide == EnumAppSide.Client;
        }

        public override void StartClientSide(ICoreClientAPI api)
        {
            base.StartClientSide(api);

            mapMarkerDialog = new MapMarkerGUI(api);

            capi = api;
            capi.Input.RegisterHotKey(
                hotkeyCode: MapMarkerGUI.HotkeyCode,
                name: Lang.Get("egocarib-mapmarkers:config-keybind-name"),
                key: GlKeys.M,
                type: HotkeyType.GUIOrOtherControls,
                altPressed: false,
                ctrlPressed: true,
                shiftPressed: true);
            capi.Input.SetHotKeyHandler(
                hotkeyCode: MapMarkerGUI.HotkeyCode,
                handler: ToggleGUI);
        }

        private bool ToggleGUI(KeyCombination comb)
        {
            if (mapMarkerDialog.IsOpened())
            {
                mapMarkerDialog.TryClose();
            }
            else
            {
                mapMarkerDialog.TryOpen();
            }
            return true;
        }
    }

    public class MapMarkerGUI : GuiDialog
    {
        public const string HotkeyCode = "egocarib_MapMarkerGUI";
        public const string DialogID = "egocarib-mapmarkers-config-menu";
        public int DynamicDrawColor;
        public override string ToggleKeyCombinationCode { get { return HotkeyCode; } }
        public override bool DisableMouseGrab { get { return true; } }

        public MapMarkerConfig.Settings ModSettings;
        public OrderedDictionary<string, OrderedDictionary<string, AutoMapMarkerSetting>> AutoMapMarkerSettings;
        public readonly string ExtraSettingsTabName = Lang.Get("egocarib-mapmarkers:ui");
        public string CurrentTab;

        public MapMarkerGUI(ICoreClientAPI capi) : base(capi)
        {
            //SetupDialog();
        }

        public override void OnGuiOpened()
        {
            try
            {
                ModSettings = MapMarkerConfig.GetSettings(capi);
                AutoMapMarkerSettings = ModSettings.GetMapMarkerSettingCollection();
                SetupDialog();
            }
            catch (Exception e)
            {
                MapMarkerMod.CoreAPI.Logger.Error("Map Marker Mod: Failed to initialize settings GUI. (" + e.ToString() + ")");
            }
            base.OnGuiOpened();
        }

        private void SetupDialog()
        {
            // Icon info
            string[] icons = MapMarkerConfig.Settings.Icons.Split(',');
            string[] iconsVTML = MapMarkerConfig.Settings.IconsVTML.Split(',');

            CairoFont headerFont = CairoFont.WhiteSmallishText().Clone().WithWeight(Cairo.FontWeight.Bold);
            headerFont.Color[3] = 0.6; // Adjust transparency
            CairoFont disabledFont = CairoFont.WhiteSmallishText().Clone().WithSlant(FontSlant.Italic);
            disabledFont.Color[3] = 0.2;

            /*
             *  General dialog structure (not including clipping/inset/scrolling elements):
             *  
             * dialogBounds
             *   bgBounds
             *     markerHeaderBounds
             *     markerOptionAreaBounds
             *       markerOptionRowBounds [multiple copies]
             *         markerOption*
             *         markerOption*
             *         ...etc...
             *     dialogButtonBounds
             */

            // Auto-sized dialog at the center of the screen
            ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.CenterMiddle);

            // Fixed position values
            double dialogPadding = 40;
            double xpos = 0;
            double ypos = 2.5;
            double yStart = 0; // 25;
            double dgWidth = 960 + dialogPadding * 2;
            double opAreaHeight = 575;
            double opAreaWidth = 960 + dialogPadding * 2;
            double toggleBarHeight = 76;
            double headerHeight = 48;
            double rowIndent = 20;
            double rowHeight = 42;
            double uiElementHeight = 25;
            double opPromptWidth = 200;
            double toggleWidth = 100; //Toggle switch, plus empty space after
            double disabledMsgWidth = 200;
            double iconLabelWidth = 52;
            double iconDropdownWidth = 60;
            double iconDropdownAfter = 28;
            double colorLabelWidth = 62;
            double colorInputWidth = 140;
            double colorInputAfter = 10;
            double colorPreviewWidth = 55; //Color preview box, plus empty space after
            double nameLabelWidth = 62;
            double nameInputWidth = 140;
            double titleBarThickness = 31;

            ElementBounds toggleButtonBarBounds = ElementBounds
                .Fixed(0, titleBarThickness, dgWidth, toggleBarHeight)
                .WithFixedPadding(GuiStyle.ElementToDialogPadding)
                .WithFixedWidth(dgWidth - 2.0 * GuiStyle.ElementToDialogPadding);
            ElementBounds toggleButtonBounds = ElementBounds.Fixed(0, 0, 160, 40).WithFixedPadding(0, 3);

            ElementBounds markerHeaderBounds = ElementBounds.Fixed(0, yStart, dgWidth, headerHeight);
            ElementBounds markerOptionAreaBounds = ElementBounds.Fixed(0, (int)(yStart /*+ headerHeight*/))
                .WithFixedPadding(rowIndent, 0)
                .WithSizing(ElementSizing.FitToChildren);


            ElementBounds markerOptionRowBounds = ElementBounds.Fixed(0, 0)
                .WithSizing(horizontalSizing: ElementSizing.FitToChildren, verticalSizing: ElementSizing.Fixed)
                .WithFixedHeight(rowHeight); // Single option row

            ElementBounds markerOptionPromptBounds = ElementBounds.Fixed(xpos, ypos, opPromptWidth, rowHeight); 
            ElementBounds markerOptionToggleBounds = ElementBounds.Fixed(xpos += opPromptWidth, ypos - 2.5, toggleWidth, rowHeight); // bigger than dropdown/textinput, so we minus 2.5 pixels to vertically align better with rest of row
            ElementBounds markerOptionDisabledMessage = ElementBounds.Fixed(xpos += toggleWidth, ypos, disabledMsgWidth, rowHeight);
            ElementBounds markerOptionIconLabelBounds = ElementBounds.Fixed(xpos, ypos, iconLabelWidth, rowHeight); 
            ElementBounds markerOptionIconDropdownBounds = ElementBounds.Fixed(xpos += iconLabelWidth, ypos, iconDropdownWidth, uiElementHeight);
            ElementBounds markerOptionColorLabelBounds = ElementBounds.Fixed(xpos += iconDropdownWidth + iconDropdownAfter, ypos, colorLabelWidth, rowHeight);
            ElementBounds markerOptionColorInputBounds = ElementBounds.Fixed(xpos += colorLabelWidth, ypos, colorInputWidth, uiElementHeight);
            ElementBounds markerOptionColorPreviewBounds = ElementBounds.Fixed(xpos += colorInputWidth + colorInputAfter, ypos, colorPreviewWidth, uiElementHeight);
            ElementBounds markerOptionNameLabelBounds = ElementBounds.Fixed(xpos += colorPreviewWidth, ypos, nameLabelWidth, rowHeight);
            ElementBounds markerOptionNameInputBounds = ElementBounds.Fixed(xpos += nameLabelWidth, ypos, nameInputWidth, uiElementHeight);

            ElementBounds mainContentBounds = ElementBounds.Fixed(0, titleBarThickness + toggleBarHeight, dgWidth - dialogPadding * 2, /*headerHeight +*/ opAreaHeight)
                .WithFixedPadding(dialogPadding)
                // Uncomment the following line to make each tab resize the GUI to fit its contents (decided I didn't like that)
                //.WithSizing(horizontalSizing: ElementSizing.Fixed, verticalSizing: ElementSizing.FitToChildren)
                .WithChildren(/*markerHeaderBounds,*/ markerOptionAreaBounds);

            ElementBounds bgBounds = ElementBounds.Fill
                .WithChildren(mainContentBounds, toggleButtonBarBounds)
                .WithSizing(ElementSizing.FitToChildren);


            SingleComposer = capi.Gui.CreateCompo(DialogID, dialogBounds)


                .AddShadedDialogBG(bgBounds)
                .AddDialogTitleBar(Lang.Get("egocarib-mapmarkers:config-menu-title"), OnTitleBarCloseClicked)
                .BeginChildElements(bgBounds)

                //.AddShadedDialogBG(toggleButtonBarBounds, withTitleBar: false)
                .AddStaticCustomDraw(toggleButtonBarBounds, delegate (Context ctx, ImageSurface surface, ElementBounds bounds)
                {
                    //This header design copied from Vintagestory.Client.NoObf.GuiCompositeSettings.ComposerHeader
                    ctx.SetSourceRGBA(1.0, 1.0, 1.0, 0.1);
                    GuiElement.RoundRectangle(ctx, GuiElement.scaled(5.0) + bounds.bgDrawX, GuiElement.scaled(5.0) + bounds.bgDrawY, bounds.OuterWidth - GuiElement.scaled(10.0), GuiElement.scaled(75.0), 1.0);
                    ctx.Fill();
                })
                .BeginChildElements();

            foreach (var settingGroupName in AutoMapMarkerSettings.Keys.Concat(new [] { ExtraSettingsTabName }))
            {
                if (string.IsNullOrEmpty(CurrentTab))
                {
                    CurrentTab = settingGroupName;
                }
                CairoFont buttonFont = CairoFont.ButtonText();
                TextExtents textExtents = buttonFont.GetTextExtents(settingGroupName);
                double width = textExtents.Width / (double)RuntimeEnv.GUIScale + 15.0;
                SingleComposer.AddToggleButton(
                    text: settingGroupName,
                    font: buttonFont,
                    onToggle: isSelected => OnTabToggle(settingGroupName),
                    bounds: toggleButtonBounds.WithFixedWidth(width),
                    key: settingGroupName + "-toggle-tab");
                toggleButtonBounds = toggleButtonBounds.RightCopy(15, 0);
            }

            SingleComposer.GetToggleButton(CurrentTab + "-toggle-tab").SetValue(true);

            SingleComposer
                .AddButton(text: Lang.Get("general-save"),
                    onClick: OnSaveButton,
                    bounds: ElementBounds.Fixed(0.0, 0.0, 80.0, 40.0).WithFixedPadding(4.0, 3.0).WithAlignment(EnumDialogArea.RightTop))
                .EndChildElements()

                .BeginChildElements(mainContentBounds)

                //.AddStaticText(Lang.Get("egocarib-mapmarkers:config-menu-section-header1"), headerFont, markerHeaderBounds)
                .BeginChildElements(markerOptionAreaBounds);


            foreach (var settingGroup in AutoMapMarkerSettings)
            {
                if (settingGroup.Key != CurrentTab)
                {
                    continue;
                }
                //TODO: add title text for group? (settingGroup.Key)
                foreach (var setting in settingGroup.Value)
                {
                    string markerSettingTitle = setting.Key;
                    AutoMapMarkerSetting markerSetting = setting.Value;

                    SingleComposer.BeginChildElements(markerOptionRowBounds)

                    // Option name + toggle switch to enable
                    .AddStaticText(
                        text: markerSettingTitle,
                        font: CairoFont.WhiteSmallishText(),
                        bounds: markerOptionPromptBounds.FlatCopy().WithParent(markerOptionRowBounds))
                    .AddSwitch(
                        onToggle: OnMarkerToggleEnabled,
                        bounds: markerOptionToggleBounds.FlatCopy().WithParent(markerOptionRowBounds),
                        key: markerSettingTitle + "-auto-markers-enabled")

                    .AddIf(markerSetting.Enabled == true)

                        // Map marker icon
                        .AddStaticText(
                            text: Lang.Get("egocarib-mapmarkers:icon"),
                            font: CairoFont.WhiteSmallishText(),
                            bounds: markerOptionIconLabelBounds.FlatCopy().WithParent(markerOptionRowBounds))
                        .AddDropDown(
                            values: icons,
                            names: iconsVTML,
                            selectedIndex: GetIconIndex(icons, markerSetting),
                            onSelectionChanged: OnMarkerIconChanged,
                            bounds: markerOptionIconDropdownBounds.FlatCopy().WithParent(markerOptionRowBounds),
                            key: markerSettingTitle + "-auto-markers-icon")

                        // Map marker color
                        .AddStaticText(
                            text: Lang.Get("egocarib-mapmarkers:color"),
                            font: CairoFont.WhiteSmallishText(),
                            bounds: markerOptionColorLabelBounds.FlatCopy().WithParent(markerOptionRowBounds))
                        .AddTextInput(
                            bounds: markerOptionColorInputBounds.FlatCopy().WithParent(markerOptionRowBounds),
                            OnTextChanged: OnMarkerColorChanged,
                            font: CairoFont.TextInput(),
                            key: markerSettingTitle + "-auto-markers-color")
                        .AddDynamicCustomDraw(
                            bounds: markerOptionColorPreviewBounds.FlatCopy().WithParent(markerOptionRowBounds),
                            OnDraw: OnDrawColorRect,
                            key: markerSettingTitle + "-auto-markers-color-rect")

                        // Map marker name
                        .AddStaticText(
                            text: Lang.Get("egocarib-mapmarkers:name"),
                            font: CairoFont.WhiteSmallishText(),
                            bounds: markerOptionNameLabelBounds.FlatCopy().WithParent(markerOptionRowBounds))
                        .AddTextInput(
                            bounds: markerOptionNameInputBounds.FlatCopy().WithParent(markerOptionRowBounds),
                            OnTextChanged: OnMarkerNameChanged,
                            font: CairoFont.TextInput(),
                            key: markerSettingTitle + "-auto-markers-name")

                    .EndIf()
                    .AddIf(markerSetting.Enabled == false)

                        //"Disabled" message
                        .AddStaticText(
                            text: Lang.Get("egocarib-mapmarkers:disabled"),
                            font: disabledFont,
                            bounds: markerOptionDisabledMessage.FlatCopy().WithParent(markerOptionRowBounds))

                    .EndIf()

                    .EndChildElements(); // markerOptionRowBounds

                    // Set initial option values for this row
                    SingleComposer.GetSwitch(markerSettingTitle + "-auto-markers-enabled").SetValue(markerSetting.Enabled);
                    if (markerSetting.Enabled)
                    {
                        SingleComposer.GetTextInput(markerSettingTitle + "-auto-markers-color").SetValue(markerSetting.MarkerColor);
                        SingleComposer.GetTextInput(markerSettingTitle + "-auto-markers-name").SetValue(markerSetting.MarkerTitle);
                    }

                    markerOptionRowBounds = markerOptionRowBounds.BelowCopy(); // Create next row, immediately below current row
                }
            }
            if (CurrentTab == ExtraSettingsTabName)
            {
                ElementBounds uiToggleBounds = ElementBounds.Fixed(0, 0, 60, rowHeight);
                ElementBounds uiToggleLabelBounds = ElementBounds.Fixed(60, 2.5, 800, rowHeight);

                SingleComposer.BeginChildElements(markerOptionRowBounds)

                    .AddSwitch(
                        onToggle: isSelected => { ModSettings.ChatNotifyOnWaypointCreation = isSelected; },
                        bounds: uiToggleBounds.FlatCopy().WithParent(markerOptionRowBounds),
                        key: "toggle-show-chat-message")
                    .AddStaticText(
                        text: Lang.Get("egocarib-mapmarkers:show-chat-message"),
                        font: CairoFont.WhiteSmallishText(),
                        bounds: uiToggleLabelBounds.FlatCopy().WithParent(markerOptionRowBounds))

                .EndChildElements();
                SingleComposer.GetSwitch("toggle-show-chat-message").SetValue(ModSettings.ChatNotifyOnWaypointCreation);
            }
            
            SingleComposer
                .EndChildElements() // markerOptionAreaBounds
                .EndChildElements() // mainContentBounds
                .EndChildElements() // bgBounds
                .Compose();

            OnMarkerColorChanged(); // Force color tiles to be redrawn with correct colors.
        }

        private int GetIconIndex(string[] icons, AutoMapMarkerSetting setting)
        {
            int index = Array.FindIndex(icons, i => i.Equals(setting.MarkerIcon, StringComparison.OrdinalIgnoreCase));
            if (index < 0)
            {
                // Reset icon setting if invalid
                index = 0;
                setting.MarkerIcon = icons[0];
            }
            return index;
        }

        public void OnTabToggle(string tabName)
        {
            CurrentTab = tabName;
            SetupDialog();
        }

        public void OnMarkerToggleEnabled(bool isSelected)
        {
            foreach (var settingGroup in AutoMapMarkerSettings)
            {
                foreach (KeyValuePair<string, AutoMapMarkerSetting> settings in settingGroup.Value)
                {
                    GuiElementSwitch enabledSwitch = SingleComposer.GetSwitch(settings.Key + "-auto-markers-enabled");
                    if (enabledSwitch != null)
                    {
                        settings.Value.Enabled = enabledSwitch.On;
                    }
                }
            }
            SetupDialog(); // Redraw GUI
        }

        private void OnMarkerIconChanged(string iconName, bool selected)
        {
            foreach (var settingGroup in AutoMapMarkerSettings)
            {
                foreach (KeyValuePair<string, AutoMapMarkerSetting> settings in settingGroup.Value)
                {
                    GuiElementSwitch enabledSwitch = SingleComposer.GetSwitch(settings.Key + "-auto-markers-enabled");
                    if (enabledSwitch == null || !enabledSwitch.On)
                    {
                        continue; //this marker option not enabled & won't have icon fields.
                    }
                    settings.Value.MarkerIcon = SingleComposer.GetDropDown(settings.Key + "-auto-markers-icon").SelectedValue;
                }
            }
        }

        private void OnMarkerColorChanged(string colorstring = "")
        {
            int colorTransparent = Color.Transparent.ToArgb();
            int colorBlack = Color.Black.ToArgb();
            bool saveEnabled = true;

            foreach (var settingGroup in AutoMapMarkerSettings)
            {
                foreach (KeyValuePair<string, AutoMapMarkerSetting> settings in settingGroup.Value)
                {
                    GuiElementSwitch enabledSwitch = SingleComposer.GetSwitch(settings.Key + "-auto-markers-enabled");
                    if (enabledSwitch == null || !enabledSwitch.On)
                    {
                        continue; //this marker option not enabled & won't have color fields.
                    }
                    GuiElementTextInput colorInput = SingleComposer.GetTextInput(settings.Key + "-auto-markers-color");
                    GuiElementTextInput nameInput = SingleComposer.GetTextInput(settings.Key + "-auto-markers-name");
                    GuiElementCustomDraw colorTile = SingleComposer.GetCustomDraw(settings.Key + "-auto-markers-color-rect");
                    colorstring = colorInput.GetText();
                    int? parsedColor = null;
                    if (colorstring.StartsWith("#"))
                    {
                        if (colorstring.Length == 7)
                        {
                            string s = colorstring.Substring(1);
                            try
                            {
                                parsedColor = (int.Parse(s, NumberStyles.HexNumber) | colorBlack);
                            }
                            catch (Exception)
                            {
                            }
                        }
                    }
                    else
                    {
                        Color color = Color.FromName(colorstring);
                        if (color.A == byte.MaxValue)
                        {
                            parsedColor = color.ToArgb();
                        }
                    }
                    DynamicDrawColor = (parsedColor ?? colorTransparent);
                    colorInput.Font.Color = (parsedColor.HasValue ? GuiStyle.DialogDefaultTextColor : GuiStyle.ErrorTextColor);
                    bool nameValid = nameInput.GetText().Trim() != "";
                    bool colorValid = parsedColor.HasValue;
                    if (!nameValid || !colorValid)
                    {
                        saveEnabled = false;
                    }
                    colorTile.Redraw();
                    if (colorValid)
                    {
                        settings.Value.MarkerColor = colorstring;
                    }
                }
            }

            SetSaveButtonState(saveEnabled);
        }

        private void OnMarkerNameChanged(string name)
        {
            bool saveEnabled = true;

            foreach (var settingGroup in AutoMapMarkerSettings)
            {
                foreach (KeyValuePair<string, AutoMapMarkerSetting> settings in settingGroup.Value)
                {
                    GuiElementSwitch enabledSwitch = SingleComposer.GetSwitch(settings.Key + "-auto-markers-enabled");
                    if (enabledSwitch == null || !enabledSwitch.On)
                    {
                        continue; //this marker option not enabled & won't have name fields.
                    }
                    bool colorValid = SingleComposer.GetTextInput(settings.Key + "-auto-markers-color").Font.Color != GuiStyle.ErrorTextColor;
                    name = SingleComposer.GetTextInput(settings.Key + "-auto-markers-name").GetText();
                    bool nameValid = name.Trim() != "";
                    if (!colorValid || !nameValid)
                    {
                        saveEnabled = false;
                    }
                    if (nameValid)
                    {
                        settings.Value.MarkerTitle = name;
                    }
                }
            }

            SetSaveButtonState(saveEnabled);
        }

        private void OnDrawColorRect(Context ctx, ImageSurface surface, ElementBounds currentBounds)
        {
            ctx.Rectangle(0.0, 0.0, 25.0, 25.0);
            ctx.SetSourceRGBA(ColorUtil.ToRGBADoubles(DynamicDrawColor));
            ctx.FillPreserve();
            ctx.SetSourceRGBA(GuiStyle.DialogBorderColor);
            ctx.Stroke();
        }

        private void SetSaveButtonState(bool enabled)
        {
            GuiElementTextButton saveButton = SingleComposer.GetButton("auto-markers-saveButton");
            if (saveButton != null)
            {
                saveButton.Enabled = enabled;
            }
        }

        public bool OnSaveButton()
        {
            TryClose();
            return true;
        }

        public override void OnGuiClosed()
        {
            GuiElementTextButton saveButton = SingleComposer.GetButton("auto-markers-saveButton");
            if (saveButton != null && saveButton.Enabled == false)
            {
                //"Auto Map Marker settings not saved: Settings included invalid colors or names."
                MapMarkerMod.CoreClientAPI.SendChatMessage(Lang.Get("egocarib-mapmarkers:not-saved-warning"));
            }
            else
            {
                MapMarkerConfig.SaveSettings(capi, ModSettings);
            }
            base.OnGuiClosed();
        }

        public override bool CaptureAllInputs()
        {
            return IsOpened();
        }

        private void OnTitleBarCloseClicked()
        {
            TryClose();
        }
    }
}
