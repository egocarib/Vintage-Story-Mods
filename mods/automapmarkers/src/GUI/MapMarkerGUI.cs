using System;
using System.Collections.Generic;
using System.Linq;
using Cairo;
using Vintagestory.API.Client;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Egocarib.AutoMapMarkers.Settings;
using Egocarib.AutoMapMarkers.Utilities;
using Vintagestory.API.Common;
using System.Text.RegularExpressions;
using Vintagestory.API.Util;
using Vintagestory.GameContent;
using Vintagestory.API.MathTools;

namespace Egocarib.AutoMapMarkers.GUI
{
    using AutoMapMarkerSetting = MapMarkerConfig.Settings.AutoMapMarkerSetting;

    /// <summary>
    /// The Auto Map Marker configuration menu
    /// </summary>
    public class MapMarkerGUI : GuiDialog
    {
        public const string HotkeyCode = "egocarib_MapMarkerGUI";
        public const string DialogID = "egocarib-mapmarkers-config-menu";
        public MapMarkerConfig.Settings ModSettings;
        public Vintagestory.API.Datastructures.OrderedDictionary<string, Vintagestory.API.Datastructures.OrderedDictionary<string, AutoMapMarkerSetting>> AutoMapMarkerSettings;
        public readonly string ExtraSettingsTabName = Lang.Get("egocarib-mapmarkers:ui");
        public string CurrentTab;

        private ElementBounds scrollContentBounds;
        private float currentScrollY;
        private Action RegisterCustomHotkeys;
        private Action RegisterDeleteHotkey;
        private Action RegisterDetectHotkey;
        public string[] icons;
        public string[] iconsVTML;
        public int[] colors;
        private List<IAsset> loadedIconAssets;
        MapMarkerIconSettingsGUI iconConfigPopup;

        public override string ToggleKeyCombinationCode { get { return HotkeyCode; } }

        public override bool DisableMouseGrab { get { return true; } }

        public MapMarkerGUI(ICoreClientAPI capi, Action onRegisterCustomHotkeys, Action onRegisterDeleteHotkey, Action onRegisterDetectHotkey) : base(capi)
        {
            RegisterCustomHotkeys = onRegisterCustomHotkeys;
            RegisterDeleteHotkey = onRegisterDeleteHotkey;
            RegisterDetectHotkey = onRegisterDetectHotkey;
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
                LoadIconTextures();
                LoadColorOptions();

                SetupDialog();
            }
            catch (Exception e)
            {
                MapMarkerMod.CoreAPI.Logger.Error("Map Marker Mod: Failed to initialize settings GUI. (" + e.ToString() + ")");
            }
            base.OnGuiOpened();
        }

        private string GetWaypointIconName(string iconName)
        {
            return $"wp{iconName.UcFirst()}";
        }

        private void LoadColorOptions()
        {
            List<MapLayer> mapLayers = capi.ModLoader?.GetModSystem<WorldMapManager>()?.MapLayers;
            WaypointMapLayer wml = mapLayers?.FirstOrDefault((MapLayer l) => l is WaypointMapLayer) as WaypointMapLayer;
            if (wml != null)
            {
                colors = wml.WaypointColors.ToArray();
            }
            else
            {
                MessageUtil.Log("Unable to load map color options from the game. Falling back to hard-coded defaults...");
                colors = WaypointUtil.GetDefaultWaypointColorOptions();
            }
        }

        /// <summary>
        /// Loads all the world map waypoint icons, which typically are loaded from the following directory:
        /// %appdata%\Vintagestory\assets\survival\textures\icons\worldmap
        /// Also loads modded icons from the same asset directory of each mod
        /// </summary>
        private void LoadIconTextures()
        {
            // Load (or retrieve cached) icon assets from the asset manager
            loadedIconAssets = capi.Assets.GetMany("textures/icons/worldmap/", null, loadAsset: true);

            List<string> listIcons = new List<string>();
            List<string> listIconsVTML = new List<string>();
            foreach (var icon in loadedIconAssets)
            {
                // Replicate logic the game uses when loading map icons in WaypointMapLayer constructor
                string name = icon.Name.Substring(0, icon.Name.IndexOf("."));
                name = Regex.Replace(name, "\\d+\\-", "");
                listIcons.Add(name);                                               //example:  bee
                listIconsVTML.Add($"<icon name=\"{GetWaypointIconName(name)}\">"); //example:  <icon name=\"wpBee\">
            }
            icons = listIcons.ToArray();
            iconsVTML = listIconsVTML.ToArray();
        }

        /// <summary>
        /// Release references to the icon assets so that the AssetManager can later dispose of them.
        /// </summary>
        private void UnloadIconTextures()
        {
            loadedIconAssets = null;
        }

        /// <summary>
        /// Composes or re-composes the GUI. Called each time the menu tab changes or an option is toggled off/on
        /// </summary>
        private void SetupDialog()
        {
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
             *        clipBounds (visible viewport, marker tabs only)
             *          markerOptionAreaBounds (scrollable content)
             *            markerOptionRowBounds [multiple]
             *              markerOption* [multiple]
             *        scrollbarBounds (marker tabs only)
             */

            // Determine current tab and whether it needs to be paged
            if (string.IsNullOrEmpty(CurrentTab))
                CurrentTab = AutoMapMarkerSettings.Keys.First();
            bool isMarkerTab = CurrentTab != ExtraSettingsTabName;

            // Auto-sized dialog at the center of the screen
            ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.CenterMiddle);

            // Fixed position values
            double dialogPadding = 40;
            double xpos = 0;
            double ypos = 2.5;
            double yStart = 0;
            double opAreaHeight = 575;
            double dgMinWidth = 920;
            double dgMaxWidth = 1080;
            double toggleBarHeight = 76;

            double rowIndent = 20;
            double rowHeight = 42;

            double opPromptWidth = 280;
            double toggleWidth = 64; // 100; //Toggle switch, plus empty space after
            double disabledMsgWidth = 200;
            double iconColorButtonWidth = 70;
            double iconColorButtonHeight = 32;
            double titleBarThickness = 31;
            double dgInnerWidth = opPromptWidth + toggleWidth + iconColorButtonWidth;
            if (dgInnerWidth < dgMinWidth)
                dgInnerWidth = dgMinWidth;
            else if (dgInnerWidth > dgMaxWidth)
                dgInnerWidth = dgMaxWidth;
            double dgWidth = dgInnerWidth + rowIndent * 2 + dialogPadding * 2;

            ElementBounds toggleButtonBarBounds = ElementBounds
                .Fixed(0, titleBarThickness, dgWidth, toggleBarHeight)
                .WithFixedPadding(GuiStyle.ElementToDialogPadding)
                .WithFixedWidth(dgWidth - 2.0 * GuiStyle.ElementToDialogPadding);
            ElementBounds toggleButtonBounds = ElementBounds.Fixed(0, 0, 160, 40).WithFixedPadding(0, 3);


            ElementBounds markerOptionAreaBounds = ElementBounds.Fixed(0, (int)(yStart))
                .WithFixedPadding(rowIndent, 0)
                .WithSizing(ElementSizing.FitToChildren);

            ElementBounds markerOptionRowBounds = ElementBounds.Fixed(0, 0)
                .WithSizing(horizontalSizing: ElementSizing.FitToChildren, verticalSizing: ElementSizing.Fixed)
                .WithFixedHeight(rowHeight); // Single option row

            ElementBounds markerOptionPromptBounds = ElementBounds.Fixed(xpos, ypos, opPromptWidth, rowHeight); 
            ElementBounds markerOptionToggleBounds = ElementBounds.Fixed(xpos += opPromptWidth, ypos - 2.5, toggleWidth, rowHeight); // bigger than dropdown/textinput, so we minus 2.5 pixels to vertically align better with rest of row
            ElementBounds markerOptionDisabledMessage = ElementBounds.Fixed(xpos += toggleWidth, ypos, disabledMsgWidth, rowHeight);
            ElementBounds markerOptionIconColorButtonBounds = ElementBounds.Fixed(xpos, ypos - 3.5, iconColorButtonWidth, iconColorButtonHeight);

            ElementBounds mainContentBounds = ElementBounds.Fixed(0, titleBarThickness + toggleBarHeight, dgWidth - dialogPadding * 2, opAreaHeight)
                .WithFixedPadding(dialogPadding);

            // Scrollable content area bounds
            double clipWidth = dgInnerWidth + rowIndent * 2;
            double clipHeight = opAreaHeight - 10;
            ElementBounds clipBounds = ElementBounds.Fixed(0, 0, clipWidth, clipHeight);
            ElementBounds scrollbarBounds = ElementBounds.Fixed(clipWidth + 5, 0, 20, clipHeight);

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
                    continue;

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
                .EndChildElements(); // end header bar toggle buttons

            if (ModSettings.DisableAllModFeatures)
            {
                string enableLabel = Lang.Get("egocarib-mapmarkers:re-enable-mod");
                SingleComposer
                    .BeginChildElements(mainContentBounds)
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

            SingleComposer
                .BeginChildElements(mainContentBounds);

            if (isMarkerTab)
            {
                scrollContentBounds = markerOptionAreaBounds;
                SingleComposer.BeginClip(clipBounds);
            }
            SingleComposer.BeginChildElements(markerOptionAreaBounds);

            ComposeMarkerOptions(ref markerOptionRowBounds, rowHeight, dgInnerWidth, customTabName, disabledFont,
                markerOptionPromptBounds, markerOptionToggleBounds, markerOptionDisabledMessage,
                markerOptionIconColorButtonBounds, markerOptionAreaBounds);

            if (CurrentTab == ExtraSettingsTabName)
            {
                ComposeExtraSettingsTab(ref markerOptionRowBounds, rowHeight);
            }
            
            SingleComposer
                .EndChildElements(); // markerOptionAreaBounds
            if (isMarkerTab)
            {
                SingleComposer
                    .EndClip()
                    .AddVerticalScrollbar(OnNewScrollbarValue, scrollbarBounds, "scrollbar");
            }
            SingleComposer
                .EndChildElements() // mainContentBounds
                .EndChildElements() // bgBounds
                .Compose();

            if (isMarkerTab)
            {
                int itemCount = AutoMapMarkerSettings.ContainsKey(CurrentTab) ? AutoMapMarkerSettings[CurrentTab].Count : 0;
                float visibleHeight = (float)clipHeight;
                float totalContentHeight = (float)(itemCount * rowHeight);
                if (CurrentTab == customTabName)
                    totalContentHeight += (float)(rowHeight * 0.5); // Account for description row
                float savedScrollY = currentScrollY;
                var scrollbar = SingleComposer.GetScrollbar("scrollbar");
                scrollbar?.SetHeights(visibleHeight, totalContentHeight);
                if (savedScrollY > 0)
                {
                    scrollbar.CurrentYPosition = savedScrollY;
                    scrollbar.TriggerChanged();
                }
            }
        }

        /// <summary>
        /// Composes the marker option rows for the currently selected marker tab.
        /// Handles: custom tab description, marker row loop (label + toggle + icon button + disabled text), traders "copy settings" button.
        /// </summary>
        private void ComposeMarkerOptions(ref ElementBounds markerOptionRowBounds, double rowHeight, double dgInnerWidth,
            string customTabName, CairoFont disabledFont,
            ElementBounds markerOptionPromptBounds, ElementBounds markerOptionToggleBounds,
            ElementBounds markerOptionDisabledMessage, ElementBounds markerOptionIconColorButtonBounds,
            ElementBounds markerOptionAreaBounds)
        {
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
                    .AddDynamicText(
                        text: Lang.Get("egocarib-mapmarkers:custom-gui-description"),
                        font: CairoFont.WhiteSmallishText(),
                        bounds: ElementBounds.Fixed(0, 0, dgInnerWidth, rowHeight * 1.5),
                        key: "custom-tab-description")
                    .EndChildElements();
                    markerOptionRowBounds = markerOptionRowBounds.BelowCopy().WithFixedHeight(rowHeight);
                }

                foreach (var setting in settingGroup.Value)
                {
                    string markerSettingTitle = setting.Key;
                    AutoMapMarkerSetting markerSetting = setting.Value;

                    double[] markerColorRGBADoubles = markerSetting.MarkerColorInteger.HasValue
                        ? ColorUtil.ToRGBADoubles(markerSetting.MarkerColorInteger.Value)
                        : new double[] { 1.0, 1.0, 1.0, 1.0};

                    SingleComposer.BeginChildElements(markerOptionRowBounds)

                    // Option name + toggle button to enable
                    .AddDynamicText(
                        text: markerSettingTitle,
                        font: CairoFont.WhiteSmallishText(),
                        bounds: markerOptionPromptBounds.FlatCopy().WithParent(markerOptionRowBounds),
                        key: markerSettingTitle + "-auto-markers-label")
                    .AddInteractiveSwitch(
                        onToggle: OnMarkerToggleEnabled,
                        bounds: markerOptionToggleBounds.FlatCopy().WithParent(markerOptionRowBounds),
                        key: markerSettingTitle + "-auto-markers-enabled")

                    .AddIf(markerSetting.Enabled == true)

                        .AddColorIconButton(
                            icon: GetWaypointIconName(markerSetting.MarkerIcon),
                            iconColor: markerColorRGBADoubles,
                            onClick: () => { return OnIconColorButtonClick(CurrentTab, markerSettingTitle, markerSetting); },
                            bounds: markerOptionIconColorButtonBounds.FlatCopy().WithParent(markerOptionRowBounds),
                            style: EnumButtonStyle.Normal,
                            key: markerSettingTitle + "-auto-markers-icon")

                    .EndIf()
                    .AddIf(markerSetting.Enabled == false)

                        .AddDynamicText(
                            text: Lang.Get("egocarib-mapmarkers:disabled"),
                            font: disabledFont,
                            bounds: markerOptionDisabledMessage.FlatCopy().WithParent(markerOptionRowBounds),
                            key: markerSettingTitle + "-auto-markers-disabled")

                    .EndIf()

                    .EndChildElements(); // markerOptionRowBounds

                    // Set initial option values for this row
                    SingleComposer.GetInteractiveSwitch(markerSettingTitle + "-auto-markers-enabled").SetValue(markerSetting.Enabled);

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
        }

        /// <summary>
        /// Composes the "UI" extra settings tab with toggle switches for mod options.
        /// </summary>
        private void ComposeExtraSettingsTab(ref ElementBounds markerOptionRowBounds, double rowHeight)
        {
            ElementBounds uiToggleBounds = ElementBounds.Fixed(0, 0, 60, rowHeight);
            ElementBounds uiToggleLabelBounds = ElementBounds.Fixed(60, 2.5, 800, rowHeight);
            int hotkeyTooltipWidth = (int)(GetFontTextWidth(CairoFont.WhiteSmallText(), Lang.Get("egocarib-mapmarkers:show-chat-message-hotkey-tooltip")) / 2 + 25);
            string hotkeyTooltipText = Lang.Get("egocarib-mapmarkers:show-chat-message-hotkey-tooltip").Replace(">", "&gt;");

            SingleComposer.BeginChildElements(markerOptionRowBounds)

                // Interact with object to trigger creation of a map marker
                .AddSwitch(
                    onToggle: isSelected => {
                        ModSettings.EnableMarkOnInteract = isSelected;
                        if (!ModSettings.EnableMarkOnInteract && !ModSettings.EnableMarkOnSneak)
                        {
                            ModSettings.EnableMarkOnSneak = true; // Force one of these options to be chosen
                            SetupDialog();
                        }
                    },
                    bounds: uiToggleBounds.FlatCopy().WithParent(markerOptionRowBounds),
                    key: "toggle-mark-style-interact")
                .AddStaticText(
                    text: Lang.Get("egocarib-mapmarkers:mark-style-interact"),
                    font: CairoFont.WhiteSmallishText(),
                    bounds: uiToggleLabelBounds.FlatCopy().WithParent(markerOptionRowBounds))

            .EndChildElements()
            .BeginChildElements(markerOptionRowBounds = markerOptionRowBounds.BelowCopy())

                // Sneak while looking at object to trigger creation of a map marker
                .AddSwitch(
                    onToggle: isSelected => {
                        ModSettings.EnableMarkOnSneak = isSelected;
                        if (!ModSettings.EnableMarkOnSneak && !ModSettings.EnableMarkOnInteract)
                        {
                            ModSettings.EnableMarkOnInteract = true; // Force one of these options to be chosen
                            SetupDialog();
                        }
                    },
                    bounds: uiToggleBounds.FlatCopy().WithParent(markerOptionRowBounds),
                    key: "toggle-mark-style-sneak")
                .AddStaticText(
                    text: Lang.Get("egocarib-mapmarkers:mark-style-sneak"),
                    font: CairoFont.WhiteSmallishText(),
                    bounds: uiToggleLabelBounds.FlatCopy().WithParent(markerOptionRowBounds))

            .EndChildElements()
            .BeginChildElements(markerOptionRowBounds = markerOptionRowBounds.BelowCopy())

                // Mark map by pressing a hotkey while looking at object
                .AddSwitch(
                    onToggle: isSelected =>
                    {
                        ModSettings.EnableDetectHotkey = isSelected;
                        RegisterDetectHotkey();
                        SetupDialog();
                    },
                    bounds: uiToggleBounds = uiToggleBounds.FlatCopy().WithParent(markerOptionRowBounds),
                    key: "toggle-enable-detect-hotkey")
                .AddStaticText(
                    text: Lang.Get("egocarib-mapmarkers:enable-detect-hotkey"),
                    font: CairoFont.WhiteSmallishText(),
                    bounds: uiToggleLabelBounds = uiToggleLabelBounds.FlatCopy().WithParent(markerOptionRowBounds))
                .AddHoverText(
                    text: hotkeyTooltipText,
                    font: CairoFont.WhiteSmallText(),
                    width: hotkeyTooltipWidth,
                    bounds: uiToggleLabelBounds.FlatCopy())

            .EndChildElements()
            .BeginChildElements(markerOptionRowBounds = markerOptionRowBounds.BelowCopy())

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

                // Include coordinates in marker labels enabled/disabled
                .AddSwitch(
                    onToggle: isSelected => { ModSettings.LabelCoordinates = isSelected; },
                    bounds: uiToggleBounds.FlatCopy().WithParent(markerOptionRowBounds),
                    key: "toggle-label-coordinates")
                .AddStaticText(
                    text: Lang.Get("egocarib-mapmarkers:include-coordinates"),
                    font: CairoFont.WhiteSmallishText(),
                    bounds: uiToggleLabelBounds.FlatCopy().WithParent(markerOptionRowBounds))

            .EndChildElements()
            .BeginChildElements(markerOptionRowBounds = markerOptionRowBounds.BelowCopy())

                // Suppress markers on farmland enabled/disabled
                .AddSwitch(
                    onToggle: isSelected => { ModSettings.SuppressMarkerOnFarmland = isSelected; },
                    bounds: uiToggleBounds.FlatCopy().WithParent(markerOptionRowBounds),
                    key: "toggle-suppress-farmland")
                .AddStaticText(
                    text: Lang.Get("egocarib-mapmarkers:suppress-farmland"),
                    font: CairoFont.WhiteSmallishText(),
                    bounds: uiToggleLabelBounds.FlatCopy().WithParent(markerOptionRowBounds))

            .EndChildElements()
            .BeginChildElements(markerOptionRowBounds = markerOptionRowBounds.BelowCopy())

                // Chat messages for boat marker creation/deletion enabled/disabled
                .AddSwitch(
                    onToggle: isSelected => { ModSettings.ChatNotifyOnBoatMarker = isSelected; },
                    bounds: uiToggleBounds.FlatCopy().WithParent(markerOptionRowBounds),
                    key: "toggle-show-boat-chat-message")
                .AddStaticText(
                    text: Lang.Get("egocarib-mapmarkers:boat-chat-message"),
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

            SingleComposer.GetSwitch("toggle-mark-style-interact").SetValue(ModSettings.EnableMarkOnInteract);
            SingleComposer.GetSwitch("toggle-mark-style-sneak").SetValue(ModSettings.EnableMarkOnSneak);
            SingleComposer.GetSwitch("toggle-show-create-chat-message").SetValue(ModSettings.ChatNotifyOnWaypointCreation);
            SingleComposer.GetSwitch("toggle-label-coordinates").SetValue(ModSettings.LabelCoordinates);
            SingleComposer.GetSwitch("toggle-suppress-farmland").SetValue(ModSettings.SuppressMarkerOnFarmland);
            SingleComposer.GetSwitch("toggle-show-boat-chat-message").SetValue(ModSettings.ChatNotifyOnBoatMarker);
            SingleComposer.GetSwitch("toggle-enable-detect-hotkey").SetValue(ModSettings.EnableDetectHotkey);
            SingleComposer.GetSwitch("toggle-enable-custom-hotkeys").SetValue(ModSettings.EnableCustomHotkeys);
            SingleComposer.GetSwitch("toggle-enable-delete-hotkey").SetValue(ModSettings.EnableWaypointDeletionHotkey);
            SingleComposer.GetSwitch("toggle-show-delete-chat-message")?.SetValue(ModSettings.ChatNotifyOnWaypointDeletion);
            SingleComposer.GetSwitch("toggle-disable-mod").SetValue(ModSettings.DisableAllModFeatures);
        }

        /// <summary>
        /// Called when the selected menu tab changes. Recomposes the GUI to draw the new menu screen.
        /// </summary>
        private void OnTabToggle(string tabName)
        {
            CurrentTab = tabName;
            currentScrollY = 0;
            SetupDialog();
        }

        /// <summary>
        /// Called when the scrollbar value changes. Scrolls the marker options content.
        /// </summary>
        private void OnNewScrollbarValue(float value)
        {
            currentScrollY = value;
            if (scrollContentBounds != null)
            {
                scrollContentBounds.fixedY = 0 - value;
                scrollContentBounds.CalcWorldBounds();
            }
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
                    GuiElementInteractiveSwitch enabledSwitch = SingleComposer.GetInteractiveSwitch(settings.Key + "-auto-markers-enabled");
                    if (enabledSwitch != null)
                    {
                        settings.Value.Enabled = enabledSwitch.On;
                    }
                }
            }
            SetupDialog(); // Redraw GUI
        }

        private bool OnIconColorButtonClick(string settingGroup, string settingName, AutoMapMarkerSetting settings)
        {
            iconConfigPopup = new MapMarkerIconSettingsGUI(capi, settingName, settings, icons, colors);
            iconConfigPopup.TryOpen();
            iconConfigPopup.OnClosed += delegate
            {
                capi.Gui.RequestFocus(this);
                SetupDialog();  // Redraw GUI
            };
            return true;
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
            MapMarkerConfig.SaveSettings(capi, ModSettings);
            UnloadIconTextures();
            base.OnGuiClosed();
        }

        /// <summary>
        /// Measures the GUI width that will be consumed by a string of text in the specified font.
        /// Can be used to help pre-determine the width needed for the text's enclosing GUI element.
        /// </summary>
        private double GetFontTextWidth(CairoFont font, string text, double padding = 15.0)
        {
            TextExtents textExtents = font.GetTextExtents(text);
            return textExtents.Width / (double)RuntimeEnv.GUIScale + padding;
        }
    }
}
