using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Cairo;
using Vintagestory.API.Client;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Egocarib.AutoMapMarkers.Settings;
using Egocarib.AutoMapMarkers.Utilities;

namespace Egocarib.AutoMapMarkers.GUI
{
    using AutoMapMarkerSetting = MapMarkerConfig.Settings.AutoMapMarkerSetting;
    using Color = System.Drawing.Color;

    /// <summary>
    /// The Auto Map Marker configuration menu
    /// </summary>
    public class MapMarkerGUI : GuiDialog
    {
        public const string HotkeyCode = "egocarib_MapMarkerGUI";
        public const string DialogID = "egocarib-mapmarkers-config-menu";
        public MapMarkerConfig.Settings ModSettings;
        public OrderedDictionary<string, OrderedDictionary<string, AutoMapMarkerSetting>> AutoMapMarkerSettings;
        public readonly string ExtraSettingsTabName = Lang.Get("egocarib-mapmarkers:ui");
        public string CurrentTab;
        public int DynamicDrawColor;
        private Action RegisterCustomHotkeys;
        private Action RegisterDeleteHotkey;

        public override string ToggleKeyCombinationCode { get { return HotkeyCode; } }

        public override bool DisableMouseGrab { get { return true; } }

        public MapMarkerGUI(ICoreClientAPI capi, Action onRegisterCustomHotkeys, Action onRegisterDeleteHotkey) : base(capi)
        {
            RegisterCustomHotkeys = onRegisterCustomHotkeys;
            RegisterDeleteHotkey = onRegisterDeleteHotkey;
        }

        public override bool CaptureAllInputs()
        {
            return IsOpened();
        }

        /// <summary>
        /// Called when the GUI opens. Loads the player's current map marker settings and then calls SetupDialog() to compose the GUI.
        /// </summary>
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

        /// <summary>
        /// Composes or re-composes the GUI. Called each time the menu tab changes or an option is toggled off/on
        /// </summary>
        private void SetupDialog()
        {
            // Icon info
            string[] icons = MapMarkerConfig.Settings.Icons.Split(',');
            string[] iconsVTML = MapMarkerConfig.Settings.IconsVTML.Split(',');

            CairoFont headerFont = CairoFont.WhiteSmallishText().Clone().WithWeight(Cairo.FontWeight.Bold);
            headerFont.Color[3] = 0.6; // Adjust transparency
            CairoFont disabledFont = CairoFont.WhiteSmallishText().Clone().WithSlant(FontSlant.Italic);
            disabledFont.Color[3] = 0.2;

            string customTabName = Lang.Get("egocarib-mapmarkers:custom");

            /*
             *  Dialog bounds nesting structure:
             *  
             *  dialogBounds
             *    bgBounds
             *      toggleButtonBarBounds
             *        toggleButtonBounds [multiple]
             *      mainContentBounds
             *        markerOptionAreaBounds
             *          markerOptionRowBounds [multiple]
             *            markerOption* [multiple]
             */

            // Auto-sized dialog at the center of the screen
            ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.CenterMiddle);

            // Fixed position values
            double dialogPadding = 40;
            double xpos = 0;
            double ypos = 2.5;
            double yStart = 0;
            double opAreaHeight = 575;
            double toggleBarHeight = 76;
            double headerHeight = 48;
            double rowIndent = 20;
            double rowHeight = 42;
            double uiElementHeight = 25;
            double opPromptWidth = 240;
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
            double dgInnerWidth = opPromptWidth + toggleWidth + iconLabelWidth + iconDropdownWidth + iconDropdownAfter + colorLabelWidth + colorInputWidth
                + colorInputAfter + colorPreviewWidth + nameLabelWidth + nameInputWidth;
            double dgWidth = dgInnerWidth + rowIndent * 2 + dialogPadding * 2;

            ElementBounds toggleButtonBarBounds = ElementBounds
                .Fixed(0, titleBarThickness, dgWidth, toggleBarHeight)
                .WithFixedPadding(GuiStyle.ElementToDialogPadding)
                .WithFixedWidth(dgWidth - 2.0 * GuiStyle.ElementToDialogPadding);
            ElementBounds toggleButtonBounds = ElementBounds.Fixed(0, 0, 160, 40).WithFixedPadding(0, 3);

            ElementBounds markerHeaderBounds = ElementBounds.Fixed(0, yStart, dgWidth, headerHeight);
            ElementBounds markerOptionAreaBounds = ElementBounds.Fixed(0, (int)(yStart))
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

            ElementBounds mainContentBounds = ElementBounds.Fixed(0, titleBarThickness + toggleBarHeight, dgWidth - dialogPadding * 2, opAreaHeight)
                .WithFixedPadding(dialogPadding);
                // Uncomment the following line to make each tab resize the GUI to fit its contents (decided I didn't like that)
                //.WithSizing(horizontalSizing: ElementSizing.Fixed, verticalSizing: ElementSizing.FitToChildren)
                //.WithChildren(/*markerHeaderBounds,*/ markerOptionAreaBounds);

            ElementBounds bgBounds = ElementBounds.Fill
                .WithChildren(mainContentBounds, toggleButtonBarBounds)
                .WithSizing(ElementSizing.FitToChildren);

            SingleComposer = capi.Gui.CreateCompo(DialogID, dialogBounds)
                .AddShadedDialogBG(bgBounds)
                .AddDialogTitleBar(Lang.Get("egocarib-mapmarkers:config-menu-title"), OnTitleBarCloseClicked)
                .BeginChildElements(bgBounds)
                .AddStaticCustomDraw(toggleButtonBarBounds, delegate (Context ctx, ImageSurface surface, ElementBounds bounds)
                {
                    //This header design copied from Vintagestory.Client.NoObf.GuiCompositeSettings.ComposerHeader
                    ctx.SetSourceRGBA(1.0, 1.0, 1.0, 0.1);
                    GuiElement.RoundRectangle(ctx, GuiElement.scaled(5.0) + bounds.bgDrawX, GuiElement.scaled(5.0) + bounds.bgDrawY, bounds.OuterWidth - GuiElement.scaled(10.0), GuiElement.scaled(75.0), 1.0);
                    ctx.Fill();
                })
                .BeginChildElements(); // start header bar toggle buttons

            foreach (var settingGroupName in AutoMapMarkerSettings.Keys.Concat(new [] { ExtraSettingsTabName }))
            {
                if (settingGroupName == customTabName && !ModSettings.EnableCustomHotkeys)
                {
                    continue;
                }
                if (string.IsNullOrEmpty(CurrentTab))
                {
                    CurrentTab = settingGroupName;
                }
                CairoFont buttonFont = CairoFont.ButtonText();
                SingleComposer.AddToggleButton(
                    text: settingGroupName,
                    font: buttonFont,
                    onToggle: isSelected => OnTabToggle(settingGroupName),
                    bounds: toggleButtonBounds.WithFixedWidth(GetFontTextWidth(buttonFont, settingGroupName)),
                    key: settingGroupName + "-toggle-tab");
                toggleButtonBounds = toggleButtonBounds.RightCopy(15, 0);
            }

            SingleComposer.GetToggleButton(CurrentTab + "-toggle-tab").SetValue(true);

            SingleComposer
                .AddButton(text: Lang.Get("general-save"),
                    onClick: OnSaveButton,
                    bounds: ElementBounds.Fixed(0.0, 0.0, 80.0, 40.0).WithFixedPadding(4.0, 3.0).WithAlignment(EnumDialogArea.RightTop))
                .EndChildElements() // end header bar toggle buttons

                .BeginChildElements(mainContentBounds);

            if (ModSettings.DisableAllModFeatures)
            {
                string enableLabel = Lang.Get("egocarib-mapmarkers:re-enable-mod");
                SingleComposer
                    .AddButton(
                        text: enableLabel,
                        onClick: () => 
                        {
                            ModSettings.DisableAllModFeatures = false;
                            SetupDialog();
                            return true;
                        },
                        bounds: ElementBounds
                            .Fixed(0, 0, GetFontTextWidth(CairoFont.ButtonText(), enableLabel), 40)
                            .WithFixedPadding(20, 5)
                            .WithAlignment(EnumDialogArea.CenterMiddle)
                            .WithFixedAlignmentOffset(0, -100))
                    .EndChildElements() // mainContentBounds
                    .EndChildElements() // bgBounds
                    .Compose();
                return;
            }

            SingleComposer.BeginChildElements(markerOptionAreaBounds);

            foreach (var settingGroup in AutoMapMarkerSettings)
            {
                if (settingGroup.Key != CurrentTab)
                {
                    continue;
                }
                if (CurrentTab == customTabName)
                {
                    if (!ModSettings.EnableCustomHotkeys)
                    {
                        continue;
                    }
                    SingleComposer.BeginChildElements(markerOptionRowBounds = markerOptionRowBounds.FlatCopy().WithFixedHeight(rowHeight * 1.5))
                    .AddStaticText(
                        text: Lang.Get("egocarib-mapmarkers:custom-gui-description"),
                        font: CairoFont.WhiteSmallishText(),
                        bounds: ElementBounds.Fixed(0, 0, dgInnerWidth, rowHeight * 1.5))
                    .EndChildElements();
                    markerOptionRowBounds = markerOptionRowBounds.BelowCopy().WithFixedHeight(rowHeight);
                }
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
                if (CurrentTab == Lang.Get("egocarib-mapmarkers:traders"))
                {
                    // Add special button for trader settings
                    string firstTraderSettingName = settingGroup.Value.Cast<KeyValuePair<string, AutoMapMarkerSetting>>().ElementAt(0).Key.ToString();
                    string buttonLabel = Lang.Get("egocarib-mapmarkers:copy-setting-to-all-traders", firstTraderSettingName);
                    SingleComposer.AddSmallButton(
                        text: buttonLabel,
                        onClick: () =>
                        {
                            AutoMapMarkerSetting firstSetting = null;
                            foreach (var traderSettingInfo in settingGroup.Value)
                            {
                                if (firstSetting == null)
                                {
                                    firstSetting = traderSettingInfo.Value;
                                }
                                else
                                {
                                    traderSettingInfo.Value.CopyIconAndColorFrom(firstSetting);
                                }
                            }
                            SetupDialog();
                            return true;
                        },
                        bounds: markerOptionRowBounds
                            .WithSizing(ElementSizing.Fixed)
                            .WithParent(markerOptionAreaBounds)
                            .WithFixedPadding(12, 0)
                            .WithFixedAlignmentOffset(1, 10)
                            .WithAlignment(EnumDialogArea.RightFixed)
                    );
                }
            }
            if (CurrentTab == ExtraSettingsTabName)
            {
                // Build "UI" tab with additional mod options
                ElementBounds uiToggleBounds = ElementBounds.Fixed(0, 0, 60, rowHeight);
                ElementBounds uiToggleLabelBounds = ElementBounds.Fixed(60, 2.5, 800, rowHeight);
                int hotkeyTooltipWidth = (int)(GetFontTextWidth(CairoFont.WhiteSmallText(), Lang.Get("egocarib-mapmarkers:show-chat-message-hotkey-tooltip")) / 2 + 25);
                string hotkeyTooltipText = Lang.Get("egocarib-mapmarkers:show-chat-message-hotkey-tooltip").Replace(">", "&gt;");

                SingleComposer.BeginChildElements(markerOptionRowBounds)

                    // Chat messages for waypoint creation enabled/disabled
                    .AddSwitch(
                        onToggle: isSelected => { ModSettings.ChatNotifyOnWaypointCreation = isSelected; },
                        bounds: uiToggleBounds.FlatCopy().WithParent(markerOptionRowBounds),
                        key: "toggle-show-create-chat-message")
                    .AddStaticText(
                        text: Lang.Get("egocarib-mapmarkers:show-chat-message"),
                        font: CairoFont.WhiteSmallishText(),
                        bounds: uiToggleLabelBounds.FlatCopy().WithParent(markerOptionRowBounds))

                .EndChildElements()
                .BeginChildElements(markerOptionRowBounds = markerOptionRowBounds.BelowCopy())

                    // Custom waypoint hotkeys enabled/disabled
                    .AddSwitch(
                        onToggle: isSelected =>
                        {
                            ModSettings.EnableCustomHotkeys = isSelected;
                            RegisterCustomHotkeys();
                            SetupDialog();
                        },
                        bounds: uiToggleBounds = uiToggleBounds.FlatCopy().WithParent(markerOptionRowBounds),
                        key: "toggle-enable-custom-hotkeys")
                    .AddStaticText(
                        text: Lang.Get("egocarib-mapmarkers:enable-custom-hotkeys"),
                        font: CairoFont.WhiteSmallishText(),
                        bounds: uiToggleLabelBounds = uiToggleLabelBounds.FlatCopy().WithParent(markerOptionRowBounds))
                    .AddHoverText(
                        text: hotkeyTooltipText,
                        font: CairoFont.WhiteSmallText(),
                        width: hotkeyTooltipWidth,
                        bounds: uiToggleLabelBounds.FlatCopy())

                .EndChildElements()
                .BeginChildElements(markerOptionRowBounds = markerOptionRowBounds.BelowCopy())

                    // Waypoint deletion enabled/disabled
                    .AddSwitch(
                        onToggle: isSelected =>
                        {
                            ModSettings.EnableWaypointDeletionHotkey = isSelected;
                            RegisterDeleteHotkey();
                            SetupDialog();
                        },
                        bounds: uiToggleBounds = uiToggleBounds.FlatCopy().WithParent(markerOptionRowBounds),
                        key: "toggle-enable-delete-hotkey")
                    .AddStaticText(
                        text: Lang.Get("egocarib-mapmarkers:enable-delete-hotkey"),
                        font: CairoFont.WhiteSmallishText(),
                        bounds: uiToggleLabelBounds = uiToggleLabelBounds.FlatCopy().WithParent(markerOptionRowBounds))
                    .AddHoverText(
                        text: hotkeyTooltipText,
                        font: CairoFont.WhiteSmallText(),
                        width: hotkeyTooltipWidth,
                        bounds: uiToggleLabelBounds.FlatCopy())

                .EndChildElements();

                if (ModSettings.EnableWaypointDeletionHotkey)
                {
                    SingleComposer
                    .BeginChildElements(markerOptionRowBounds = markerOptionRowBounds.BelowCopy().WithFixedOffset(60, 0))

                        // Chat messages for waypoint deletion enabled/disabled
                        .AddSwitch(
                            onToggle: isSelected => { ModSettings.ChatNotifyOnWaypointDeletion = isSelected; },
                            bounds: uiToggleBounds = uiToggleBounds.FlatCopy().WithParent(markerOptionRowBounds),
                            key: "toggle-show-delete-chat-message")
                        .AddStaticText(
                            text: Lang.Get("egocarib-mapmarkers:show-chat-message-on-delete"),
                            font: CairoFont.WhiteSmallishText(),
                            bounds: uiToggleLabelBounds = uiToggleLabelBounds.FlatCopy().WithParent(markerOptionRowBounds))

                    .EndChildElements();
                    markerOptionRowBounds = markerOptionRowBounds.FlatCopy().WithFixedOffset(-60, 0);
                }

                SingleComposer
                .BeginChildElements(markerOptionRowBounds = markerOptionRowBounds.BelowCopy())

                    // Master toggle to disable the mod
                    .AddSwitch(
                        onToggle: isSelected =>
                        {
                            ModSettings.DisableAllModFeatures = isSelected;
                            SetupDialog();
                        },
                        bounds: uiToggleBounds = uiToggleBounds.FlatCopy().WithParent(markerOptionRowBounds),
                        key: "toggle-disable-mod")
                    .AddStaticText(
                        text: Lang.Get("egocarib-mapmarkers:disable-mod"),
                        font: CairoFont.WhiteSmallishText(),
                        bounds: uiToggleLabelBounds = uiToggleLabelBounds.FlatCopy().WithParent(markerOptionRowBounds))

                .EndChildElements();

                SingleComposer.GetSwitch("toggle-show-create-chat-message").SetValue(ModSettings.ChatNotifyOnWaypointCreation);
                SingleComposer.GetSwitch("toggle-enable-custom-hotkeys").SetValue(ModSettings.EnableCustomHotkeys);
                SingleComposer.GetSwitch("toggle-enable-delete-hotkey").SetValue(ModSettings.EnableWaypointDeletionHotkey);
                SingleComposer.GetSwitch("toggle-show-delete-chat-message")?.SetValue(ModSettings.ChatNotifyOnWaypointDeletion);
                SingleComposer.GetSwitch("toggle-disable-mod").SetValue(ModSettings.DisableAllModFeatures);
            }
            
            SingleComposer
                .EndChildElements() // markerOptionAreaBounds
                .EndChildElements() // mainContentBounds
                .EndChildElements() // bgBounds
                .Compose();

            OnMarkerColorChanged(); // Force color tiles to be redrawn with correct colors.
        }

        /// <summary>
        /// Calculates the dropdown index of an icon associated with a given setting.
        /// </summary>
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

        /// <summary>
        /// Called when the selected menu tab changes. Recomposes the GUI to draw the new menu screen.
        /// </summary>
        private void OnTabToggle(string tabName)
        {
            CurrentTab = tabName;
            SetupDialog();
        }

        /// <summary>
        /// Called when a map marker option is toggled off/on. Enables or disables that option and recomposes the GUI.
        /// </summary>
        private void OnMarkerToggleEnabled(bool isSelected)
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

        /// <summary>
        /// Called when an icon dropdown choice changes. Updates the related map marker settings.
        /// </summary>
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

        /// <summary>
        /// Called when the color input text changes. Updates the related map marker settings.
        /// </summary>
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

        /// <summary>
        /// Called when the name input text changes. Updates the related map marker settings.
        /// </summary>
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

        /// <summary>
        /// Draws the color preview tile.
        /// </summary>
        private void OnDrawColorRect(Context ctx, ImageSurface surface, ElementBounds currentBounds)
        {
            ctx.Rectangle(0.0, 0.0, 25.0, 25.0);
            ctx.SetSourceRGBA(ColorUtil.ToRGBADoubles(DynamicDrawColor));
            ctx.FillPreserve();
            ctx.SetSourceRGBA(GuiStyle.DialogBorderColor);
            ctx.Stroke();
        }

        /// <summary>
        /// Enables or disables the Save button. Disabled when a color input is invalid or if no Name is specified for an option.
        /// </summary>
        private void SetSaveButtonState(bool enabled)
        {
            GuiElementTextButton saveButton = SingleComposer.GetButton("auto-markers-saveButton");
            if (saveButton != null)
            {
                saveButton.Enabled = enabled;
            }
        }

        /// <summary>
        /// Closes the GUI, which will also trigger an attempt to save the settings.
        /// </summary>
        private bool OnSaveButton()
        {
            TryClose();
            return true;
        }

        /// <summary>
        /// Called when the title bar "X" button is clicked. Closes the GUI and attempts to save the settings.
        /// </summary>
        private void OnTitleBarCloseClicked()
        {
            TryClose();
        }

        /// <summary>
        /// Called when the GUI is closed for any reason. Attempts to save the user's settings if they are valid.
        /// </summary>
        public override void OnGuiClosed()
        {
            GuiElementTextButton saveButton = SingleComposer?.GetButton("auto-markers-saveButton");
            if (saveButton != null && saveButton.Enabled == false)
            {
                //"Auto Map Marker settings not saved: Settings included invalid colors or names."
                MessageUtil.Chat(Lang.Get("egocarib-mapmarkers:not-saved-warning"));
            }
            else
            {
                MapMarkerConfig.SaveSettings(capi, ModSettings);
            }
            base.OnGuiClosed();
        }

        /// <summary>
        /// Measures the GUI width that will be consumed by a string of text in the specified font.
        /// Can be used to help pre-determine the width needed for the text's enclosing GUI element.
        /// </summary>
        private double GetFontTextWidth(CairoFont font, string text)
        {
            TextExtents textExtents = font.GetTextExtents(text);
            return textExtents.Width / (double)RuntimeEnv.GUIScale + 15.0;
        }
    }
}
