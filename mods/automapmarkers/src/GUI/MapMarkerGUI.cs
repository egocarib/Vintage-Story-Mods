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
        private const double ScrollAreaHeight = 565;
        private const double ScrollAreaPadding = 10;
        public MapMarkerConfig.Settings ModSettings;
        public MarkerSettingLayout Layout;
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
                Layout = ModSettings.GetMapMarkerSettingLayout();
                if (Layout == null)
                {
                    MapMarkerMod.CoreAPI.Logger.Error("Map Marker Mod: Marker definitions not loaded. Cannot open settings GUI.");
                    return;
                }
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
                CurrentTab = Layout.Tabs.Count > 0 ? Layout.Tabs[0].TabKey : "UI";
            bool isMarkerTab = CurrentTab != "UI";

            // Auto-sized dialog at the center of the screen
            ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.CenterMiddle);

            // Fixed position values
            double dialogPadding = 40;
            double xpos = 0;
            double ypos = 2.5;
            double yStart = 0;
            double opAreaHeight = ScrollAreaHeight + ScrollAreaPadding;
            double dgMinWidth = 920;
            double dgMaxWidth = 1080;
            double toggleBarHeight = 76;

            double rowIndent = 20;
            double rowHeight = 42;

            double opPromptWidth = 320;
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
            double toggleClickSize = 34; // Tightly wrap the 30x30 visual switch
            ElementBounds markerOptionToggleBounds = ElementBounds.Fixed(xpos += opPromptWidth, ypos - 2.5, toggleClickSize, toggleClickSize);
            ElementBounds markerOptionDisabledMessage = ElementBounds.Fixed(xpos += toggleWidth, ypos, disabledMsgWidth, rowHeight);
            ElementBounds markerOptionIconColorButtonBounds = ElementBounds.Fixed(xpos, ypos - 3.5, iconColorButtonWidth, iconColorButtonHeight);

            ElementBounds mainContentBounds = ElementBounds.Fixed(0, titleBarThickness + toggleBarHeight, dgWidth - dialogPadding * 2, opAreaHeight)
                .WithFixedPadding(dialogPadding);

            // Scrollable content area bounds
            double clipWidth = dgInnerWidth + rowIndent * 2;
            double clipHeight = ScrollAreaHeight;
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

            // Marker tabs from layout
            foreach (var tab in Layout.Tabs)
            {
                string tabDisplayName = Lang.Get(tab.LangKey);
                string tabKey = tab.TabKey;
                CairoFont buttonFont = CairoFont.ButtonText();
                SingleComposer.AddToggleButton(
                    text: tabDisplayName,
                    font: buttonFont,
                    onToggle: isSelected => OnTabToggle(tabKey),
                    bounds: toggleButtonBounds.WithFixedWidth(GetFontTextWidth(buttonFont, tabDisplayName)),
                    key: tabKey + "-toggle-tab");
                toggleButtonBounds = toggleButtonBounds.RightCopy(15, 0);
            }
            // Custom tab (conditional)
            if (ModSettings.EnableCustomHotkeys)
            {
                string customDisplayName = Lang.Get("egocarib-mapmarkers:custom");
                CairoFont buttonFont = CairoFont.ButtonText();
                SingleComposer.AddToggleButton(
                    text: customDisplayName,
                    font: buttonFont,
                    onToggle: isSelected => OnTabToggle("Custom"),
                    bounds: toggleButtonBounds.WithFixedWidth(GetFontTextWidth(buttonFont, customDisplayName)),
                    key: "Custom-toggle-tab");
                toggleButtonBounds = toggleButtonBounds.RightCopy(15, 0);
            }
            // UI tab (always)
            {
                CairoFont buttonFont = CairoFont.ButtonText();
                SingleComposer.AddToggleButton(
                    text: ExtraSettingsTabName,
                    font: buttonFont,
                    onToggle: isSelected => OnTabToggle("UI"),
                    bounds: toggleButtonBounds.WithFixedWidth(GetFontTextWidth(buttonFont, ExtraSettingsTabName)),
                    key: "UI-toggle-tab");
            }

            SingleComposer.GetToggleButton(CurrentTab + "-toggle-tab").SetValue(true);

            SingleComposer
                .AddButton(text: Lang.Get("general-close"),
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
                SingleComposer
                    .AddInset(clipBounds, 3)
                    .BeginClip(clipBounds);
            }
            SingleComposer.BeginChildElements(markerOptionAreaBounds);

            ComposeMarkerOptions(ref markerOptionRowBounds, rowHeight, dgInnerWidth, disabledFont,
                markerOptionPromptBounds, markerOptionToggleBounds, markerOptionDisabledMessage,
                markerOptionIconColorButtonBounds, markerOptionAreaBounds);

            if (CurrentTab == "UI")
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
                float visibleHeight = (float)clipHeight;
                float totalContentHeight = CalculateTabContentHeight(rowHeight);
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
        /// Calculates total content height for the current marker tab.
        /// </summary>
        private float CalculateTabContentHeight(double rowHeight)
        {
            if (CurrentTab == "Custom")
            {
                return (float)((Layout.CustomEntries.Count * rowHeight) + (rowHeight * 0.5));
            }

            var tab = Layout.Tabs.FirstOrDefault(t => t.TabKey == CurrentTab);
            if (tab == null) return 0;

            int totalRows = 0;
            foreach (var sg in tab.Subgroups)
            {
                foreach (var entry in sg.Entries)
                {
                    totalRows++; // parent/header row
                    if (entry.IsExpandable && entry.IsExpanded)
                        totalRows += entry.SubEntries?.Count ?? 0;
                }
            }
            double extraHeight = 0;
            if (tab.HasSubheadings)
            {
                totalRows += tab.Subgroups.Count; // one row per subheading
                extraHeight = (tab.Subgroups.Count - 1) * rowHeight * 0.4; // spacing between subgroups
            }
            return (float)(totalRows * rowHeight + extraHeight + 9); // +9 for inset top padding
        }

        /// <summary>
        /// Composes the marker option rows for the currently selected marker tab.
        /// Handles: layout tabs with subheadings, custom tab, expandable entries.
        /// </summary>
        private void ComposeMarkerOptions(ref ElementBounds markerOptionRowBounds, double rowHeight, double dgInnerWidth,
            CairoFont disabledFont,
            ElementBounds markerOptionPromptBounds, ElementBounds markerOptionToggleBounds,
            ElementBounds markerOptionDisabledMessage, ElementBounds markerOptionIconColorButtonBounds,
            ElementBounds markerOptionAreaBounds)
        {
            // Custom tab
            if (CurrentTab == "Custom")
            {
                if (!ModSettings.EnableCustomHotkeys)
                    return;

                SingleComposer.BeginChildElements(markerOptionRowBounds = markerOptionRowBounds.FlatCopy().WithFixedHeight(rowHeight * 1.5))
                .AddDynamicText(
                    text: Lang.Get("egocarib-mapmarkers:custom-gui-description"),
                    font: CairoFont.WhiteSmallishText(),
                    bounds: ElementBounds.Fixed(0, 0, dgInnerWidth, rowHeight * 1.5),
                    key: "custom-tab-description")
                .EndChildElements();
                markerOptionRowBounds = markerOptionRowBounds.BelowCopy().WithFixedHeight(rowHeight);

                foreach (var customEntry in Layout.CustomEntries)
                {
                    ComposeMarkerRow(customEntry.Key, customEntry.Value, ref markerOptionRowBounds, disabledFont,
                        markerOptionPromptBounds, markerOptionToggleBounds, markerOptionDisabledMessage,
                        markerOptionIconColorButtonBounds);
                }
                return;
            }

            // Layout tabs
            var tab = Layout.Tabs.FirstOrDefault(t => t.TabKey == CurrentTab);
            if (tab == null)
                return;

            // Add top padding inside the inset before the first row
            markerOptionRowBounds = markerOptionRowBounds.FlatCopy().WithFixedHeight(9);
            markerOptionRowBounds = markerOptionRowBounds.BelowCopy().WithFixedHeight(rowHeight);

            bool isFirstSubgroup = true;
            foreach (var subgroup in tab.Subgroups)
            {
                // Render subheading if tab has multiple subgroups
                if (tab.HasSubheadings && subgroup.SubgroupName != null)
                {
                    // Add vertical spacing before non-first subheadings
                    if (!isFirstSubgroup)
                    {
                        markerOptionRowBounds = markerOptionRowBounds.FlatCopy().WithFixedHeight(rowHeight * 0.4);
                        markerOptionRowBounds = markerOptionRowBounds.BelowCopy().WithFixedHeight(rowHeight);
                    }

                    string subheadingText = Lang.Get("egocarib-mapmarkers:" + subgroup.SubgroupName.ToLowerInvariant().Replace(" ", "-"));
                    CairoFont subheadingFont = CairoFont.WhiteSmallishText().Clone().WithWeight(FontWeight.Bold);
                    SingleComposer.BeginChildElements(markerOptionRowBounds = markerOptionRowBounds.FlatCopy().WithFixedHeight(rowHeight))
                    .AddDynamicText(
                        text: subheadingText,
                        font: subheadingFont,
                        bounds: ElementBounds.Fixed(0, rowHeight * 0.25, dgInnerWidth, rowHeight).WithParent(markerOptionRowBounds),
                        key: "subheading-" + subgroup.SubgroupName)
                    .EndChildElements();
                    markerOptionRowBounds = markerOptionRowBounds.BelowCopy().WithFixedHeight(rowHeight);
                    isFirstSubgroup = false;
                }

                // Indent entries under subheadings
                double subIndent = tab.HasSubheadings ? 15 : 0;
                ElementBounds promptBounds = subIndent > 0 ? markerOptionPromptBounds.FlatCopy().WithFixedOffset(subIndent, 0) : markerOptionPromptBounds;
                ElementBounds toggleBounds = subIndent > 0 ? markerOptionToggleBounds.FlatCopy().WithFixedOffset(subIndent, 0) : markerOptionToggleBounds;
                ElementBounds disabledBounds = subIndent > 0 ? markerOptionDisabledMessage.FlatCopy().WithFixedOffset(subIndent, 0) : markerOptionDisabledMessage;
                ElementBounds iconBounds = subIndent > 0 ? markerOptionIconColorButtonBounds.FlatCopy().WithFixedOffset(subIndent, 0) : markerOptionIconColorButtonBounds;

                foreach (var entry in subgroup.Entries)
                {
                    if (entry.IsExpandable)
                    {
                        ComposeExpandableMarkerRow(entry, ref markerOptionRowBounds, rowHeight, disabledFont,
                            promptBounds, toggleBounds, disabledBounds, iconBounds);
                    }
                    else
                    {
                        ComposeMarkerRow(entry.DisplayName, entry.Setting, ref markerOptionRowBounds, disabledFont,
                            promptBounds, toggleBounds, disabledBounds, iconBounds);
                    }
                }
            }
        }

        /// <summary>
        /// Composes an expandable marker entry. When collapsed, renders as a normal entry row
        /// with an expand (+) button after the icon/color button. When expanded, shows a header
        /// row with a collapse (-) button near the label, then indented sub-entries below.
        /// </summary>
        private void ComposeExpandableMarkerRow(MarkerSettingEntry entry,
            ref ElementBounds markerOptionRowBounds, double rowHeight, CairoFont disabledFont,
            ElementBounds promptBounds, ElementBounds toggleBounds,
            ElementBounds disabledBounds, ElementBounds iconBounds)
        {
            var capturedEntry = entry;
            double expandBtnSize = 32; // Same height as icon/color button
            double expandBtnGap = 10;  // Gap between icon/color button and expand button

            if (entry.IsExpanded)
            {
                // Header row: label + collapse button at toggle position
                ElementBounds headerLabelBounds = promptBounds.FlatCopy().WithParent(markerOptionRowBounds);
                ElementBounds collapseButtonBounds = toggleBounds.FlatCopy().WithParent(markerOptionRowBounds)
                    .WithFixedSize(expandBtnSize, expandBtnSize);

                SingleComposer.BeginChildElements(markerOptionRowBounds)
                .AddDynamicText(
                    text: entry.DisplayName,
                    font: CairoFont.WhiteSmallishText(),
                    bounds: headerLabelBounds,
                    key: entry.EntryLabel + "-expand-header")
                .AddExpandButton(
                    onClick: () => { ToggleExpand(capturedEntry); },
                    expand: false,
                    bounds: collapseButtonBounds,
                    key: entry.EntryLabel + "-collapse-btn")
                .EndChildElements();
                markerOptionRowBounds = markerOptionRowBounds.BelowCopy();

                // Sub-entries indented
                double subIndent = 25;
                ElementBounds subPrompt = promptBounds.FlatCopy().WithFixedOffset(subIndent, 0);
                ElementBounds subToggle = toggleBounds.FlatCopy().WithFixedOffset(subIndent, 0);
                ElementBounds subDisabled = disabledBounds.FlatCopy().WithFixedOffset(subIndent, 0);
                ElementBounds subIcon = iconBounds.FlatCopy().WithFixedOffset(subIndent, 0);

                string subKeyPrefix = entry.EntryLabel + "-";
                foreach (var sub in entry.SubEntries)
                {
                    ComposeMarkerRow(sub.DisplayName, sub.Setting, ref markerOptionRowBounds, disabledFont,
                        subPrompt, subToggle, subDisabled, subIcon, keyPrefix: subKeyPrefix);
                }
            }
            else
            {
                // Collapsed: normal entry row + expand button after icon/color button
                if (entry.Setting != null)
                {
                    // Position expand button after the disabled text area (wider than icon button)
                    // so it doesn't overlap when the entry is disabled
                    double expandButtonX = iconBounds.fixedX + iconBounds.fixedWidth + expandBtnGap + 15;
                    ElementBounds expandButtonBounds = ElementBounds.Fixed(expandButtonX, iconBounds.fixedY, expandBtnSize, expandBtnSize);

                    // Capture current row bounds before ref is modified by ComposeMarkerRow
                    var currentRowBounds = markerOptionRowBounds;
                    ComposeMarkerRow(entry.DisplayName, entry.Setting, ref markerOptionRowBounds, disabledFont,
                        promptBounds, toggleBounds, disabledBounds, iconBounds, keyPrefix: "",
                        expandButton: () => {
                            SingleComposer.AddExpandButton(
                                onClick: () => { ToggleExpand(capturedEntry); },
                                expand: true,
                                bounds: expandButtonBounds.FlatCopy().WithParent(currentRowBounds),
                                key: entry.EntryLabel + "-expand-btn");
                        });
                }
            }
        }

        /// <summary>
        /// Toggles the expand/collapse state of an expandable entry.
        /// Invalidates layout cache and rebuilds the registry immediately.
        /// </summary>
        private void ToggleExpand(MarkerSettingEntry entry)
        {
            entry.IsExpanded = !entry.IsExpanded;
            ModSettings.ExpandStates[entry.EntryLabel] = entry.IsExpanded;
            ModSettings.InvalidateLayout();
            MapMarkerConfig.RebuildRegistry();
            Layout = ModSettings.GetMapMarkerSettingLayout();
            // Clamp scroll position if collapsing made it exceed the new content height
            if (!entry.IsExpanded)
            {
                float newContentHeight = CalculateTabContentHeight(42);
                float visibleHeight = (float)ScrollAreaHeight;
                float maxScroll = Math.Max(0, newContentHeight - visibleHeight);
                if (currentScrollY > maxScroll)
                    currentScrollY = maxScroll;
            }
            SetupDialog();
        }

        /// <summary>
        /// Composes a single marker option row (label + toggle + icon button or disabled text).
        /// </summary>
        private void ComposeMarkerRow(string markerSettingTitle, AutoMapMarkerSetting markerSetting,
            ref ElementBounds markerOptionRowBounds, CairoFont disabledFont,
            ElementBounds markerOptionPromptBounds, ElementBounds markerOptionToggleBounds,
            ElementBounds markerOptionDisabledMessage, ElementBounds markerOptionIconColorButtonBounds,
            string keyPrefix = "", Action expandButton = null)
        {
            string keyBase = keyPrefix + markerSettingTitle;
            double[] markerColorRGBADoubles = markerSetting.MarkerColorInteger.HasValue
                ? ColorUtil.ToRGBADoubles(markerSetting.MarkerColorInteger.Value)
                : new double[] { 1.0, 1.0, 1.0, 1.0 };

            ElementBounds labelBounds = markerOptionPromptBounds.FlatCopy().WithParent(markerOptionRowBounds);

            SingleComposer.BeginChildElements(markerOptionRowBounds)

            .AddDynamicText(
                text: markerSettingTitle,
                font: CairoFont.WhiteSmallishText(),
                bounds: labelBounds,
                key: keyBase + "-auto-markers-label")

            .AddInteractiveSwitch(
                onToggle: OnMarkerToggleEnabled,
                bounds: markerOptionToggleBounds.FlatCopy().WithParent(markerOptionRowBounds),
                key: keyBase + "-auto-markers-enabled");

            // Add expand button first (before disabled text) to test click dispatch order
            expandButton?.Invoke();

            SingleComposer
            .AddIf(markerSetting.Enabled == true)

                .AddColorIconButton(
                    icon: GetWaypointIconName(markerSetting.MarkerIcon),
                    iconColor: markerColorRGBADoubles,
                    onClick: () => { return OnIconColorButtonClick(CurrentTab, markerSettingTitle, markerSetting); },
                    bounds: markerOptionIconColorButtonBounds.FlatCopy().WithParent(markerOptionRowBounds),
                    style: EnumButtonStyle.Normal,
                    key: keyBase + "-auto-markers-icon")

            .EndIf()
            .AddIf(markerSetting.Enabled == false)

                .AddDynamicText(
                    text: Lang.Get("egocarib-mapmarkers:disabled"),
                    font: disabledFont,
                    bounds: markerOptionDisabledMessage.FlatCopy().WithParent(markerOptionRowBounds),
                    key: keyBase + "-auto-markers-disabled")

            .EndIf();

            SingleComposer.EndChildElements(); // markerOptionRowBounds

            SingleComposer.GetInteractiveSwitch(keyBase + "-auto-markers-enabled").SetValue(markerSetting.Enabled);

            markerOptionRowBounds = markerOptionRowBounds.BelowCopy();
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
                        if (!ModSettings.EnableMarkOnInteract && !ModSettings.EnableMarkOnSneak && !ModSettings.EnableDetectHotkey)
                        {
                            ModSettings.EnableMarkOnInteract = true; // Force at least one option to be chosen
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
                        if (!ModSettings.EnableMarkOnSneak && !ModSettings.EnableMarkOnInteract && !ModSettings.EnableDetectHotkey)
                        {
                            ModSettings.EnableMarkOnSneak = true; // Force at least one option to be chosen
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
                        if (!ModSettings.EnableDetectHotkey && !ModSettings.EnableMarkOnInteract && !ModSettings.EnableMarkOnSneak)
                        {
                            ModSettings.EnableDetectHotkey = true; // Force at least one option to be chosen
                        }
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
                        if (!isSelected && CurrentTab == "Custom")
                            CurrentTab = null; // will re-default on next SetupDialog
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
            // Update all layout tab entries (including sub-entries)
            foreach (var tab in Layout.Tabs)
                foreach (var subgroup in tab.Subgroups)
                    foreach (var entry in subgroup.Entries)
                    {
                        if (entry.IsExpandable)
                        {
                            if (entry.IsExpanded && entry.SubEntries != null)
                            {
                                string subKeyPrefix = entry.EntryLabel + "-";
                                foreach (var sub in entry.SubEntries)
                                {
                                    var sw = SingleComposer.GetInteractiveSwitch(subKeyPrefix + sub.DisplayName + "-auto-markers-enabled");
                                    if (sw != null) sub.Setting.Enabled = sw.On;
                                }
                            }
                            else if (entry.Setting != null)
                            {
                                var sw = SingleComposer.GetInteractiveSwitch(entry.DisplayName + "-auto-markers-enabled");
                                if (sw != null) entry.Setting.Enabled = sw.On;
                            }
                        }
                        else if (entry.Setting != null)
                        {
                            var sw = SingleComposer.GetInteractiveSwitch(entry.DisplayName + "-auto-markers-enabled");
                            if (sw != null) entry.Setting.Enabled = sw.On;
                        }
                    }
            // Update custom entries
            foreach (var customEntry in Layout.CustomEntries)
            {
                var sw = SingleComposer.GetInteractiveSwitch(customEntry.Key + "-auto-markers-enabled");
                if (sw != null) customEntry.Value.Enabled = sw.On;
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
