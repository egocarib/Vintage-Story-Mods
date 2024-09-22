using System;
using System.Collections.Generic;
using System.Globalization;
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
        public int CurrentPage;
        public int DynamicDrawColor;
        private Action RegisterCustomHotkeys;
        private Action RegisterDeleteHotkey;
        public string[] icons;
        public string[] iconsVTML;
        public int[] colors;
        private List<IAsset> loadedIconAssets;
        MapMarkerIconSettingsGUI iconConfigPopup;

        ////TEST
        //BitmapRef floraBitmap;
        ////TEST

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
                LoadIconTextures();
                LoadColorOptions();
                //LoadScenery(); //TEST
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

        ////TEST
        //private void LoadScenery()
        //{
        //    floraBitmap = capi.Assets.Get("automapmarkers:textures/gui/amm_flora_cover.png").ToBitmap(capi);
        //}
        ////TEST

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
             *      togglePageButtonBarBounds
             *        togglePageButtonBounds [multiple]
             *      mainContentBounds
             *        markerOptionAreaBounds
             *          markerOptionRowBounds [multiple]
             *            markerOption* [multiple]
             */

            // Determine current tab and whether it needs to be paged
            if (string.IsNullOrEmpty(CurrentTab))
                CurrentTab = AutoMapMarkerSettings.Keys.First();
            bool needsPages = AutoMapMarkerSettings.ContainsKey(CurrentTab) && AutoMapMarkerSettings[CurrentTab].Count > 13;

            // Auto-sized dialog at the center of the screen
            ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.CenterMiddle);

            // Fixed position values
            double dialogPadding = 40;
            double xpos = 0;
            double ypos = 2.5;
            double yStart = 0;
            double opAreaHeight = 575;
            double dgMinWidth = 920;  //NEW
            double dgMaxWidth = 1080; //NEW
            double toggleBarHeight = 76;
            double headerHeight = 48;
            double rowIndent = 20;
            double rowHeight = 42;
            //double uiElementHeight = 25;
            double opPromptWidth = 280;
            double toggleWidth = 64; // 100; //Toggle switch, plus empty space after
            double disabledMsgWidth = 200;
            //double iconLabelWidth = GetFontTextWidth(CairoFont.WhiteSmallishText(), Lang.Get("egocarib-mapmarkers:icon"), 12.0);  // Important to calculate these for localization
            //double iconDropdownWidth = 60;
            double iconColorButtonWidth = 70;
            double iconColorButtonHeight = 32;
            //double iconDropdownAfter = 28;
            //double colorLabelWidth = GetFontTextWidth(CairoFont.WhiteSmallishText(), Lang.Get("egocarib-mapmarkers:color"), 12.0);
            //double colorInputWidth = 140;
            //double colorInputAfter = 10;
            //double colorPreviewWidth = 55; //Color preview box, plus empty space after
            //double nameLabelWidth = GetFontTextWidth(CairoFont.WhiteSmallishText(), Lang.Get("egocarib-mapmarkers:name"), 12.0);
            //double nameInputWidth = 140;
            double titleBarThickness = 31;
            double dgInnerWidth = opPromptWidth + toggleWidth + /* iconLabelWidth + iconDropdownWidth */ iconColorButtonWidth /* + iconDropdownAfter + colorLabelWidth + colorInputWidth
                + colorInputAfter + colorPreviewWidth + nameLabelWidth + nameInputWidth */;
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

            ElementBounds togglePageButtonBarBounds = ElementBounds
                .Fixed(0, titleBarThickness + toggleBarHeight, dgWidth, toggleBarHeight)
                .WithFixedPadding(GuiStyle.ElementToDialogPadding)
                .WithFixedWidth(dgWidth - 2.0 * GuiStyle.ElementToDialogPadding);
            ElementBounds togglePageButtonBounds = ElementBounds.Fixed(0, 0, 160, 40).WithFixedPadding(0, 3);

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
            //ElementBounds markerOptionIconLabelBounds = ElementBounds.Fixed(xpos, ypos, iconLabelWidth, rowHeight);
            //ElementBounds markerOptionIconDropdownBounds = ElementBounds.Fixed(xpos += iconLabelWidth, ypos, iconDropdownWidth, uiElementHeight);
            ElementBounds markerOptionIconColorButtonBounds = ElementBounds.Fixed(xpos /*+= iconLabelWidth*/, ypos - 3.5, iconColorButtonWidth, iconColorButtonHeight);
            //ElementBounds markerOptionColorLabelBounds = ElementBounds.Fixed(xpos += iconDropdownWidth + iconDropdownAfter, ypos, colorLabelWidth, rowHeight);
            //ElementBounds markerOptionColorInputBounds = ElementBounds.Fixed(xpos += colorLabelWidth, ypos, colorInputWidth, uiElementHeight);
            //ElementBounds markerOptionColorPreviewBounds = ElementBounds.Fixed(xpos += colorInputWidth + colorInputAfter, ypos, colorPreviewWidth, uiElementHeight);
            //ElementBounds markerOptionNameLabelBounds = ElementBounds.Fixed(xpos += colorPreviewWidth, ypos, nameLabelWidth, rowHeight);
            //ElementBounds markerOptionNameInputBounds = ElementBounds.Fixed(xpos += nameLabelWidth, ypos, nameInputWidth, uiElementHeight);

            ElementBounds mainContentBounds = ElementBounds.Fixed(0, titleBarThickness + toggleBarHeight, dgWidth - dialogPadding * 2, opAreaHeight)
                .WithFixedPadding(dialogPadding);
            // Uncomment the following line to make each tab resize the GUI to fit its contents (decided I didn't like that)
            //.WithSizing(horizontalSizing: ElementSizing.Fixed, verticalSizing: ElementSizing.FitToChildren)
            //.WithChildren(/*markerHeaderBounds,*/ markerOptionAreaBounds);

            ElementBounds bgBounds = ElementBounds.Fill
                .WithChildren(mainContentBounds, toggleButtonBarBounds)
                .WithSizing(ElementSizing.FitToChildren);
            if (needsPages)
                bgBounds = bgBounds.WithChild(togglePageButtonBarBounds);

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

            // Add Paging toggle bar if needed:
            int pagedOptionStartNum = 1;
            int pagedOptionEndNum = 14;
            if (needsPages)
            {
                // Draw the paging bar if this category of options needs to be paged
                SingleComposer
                    .AddStaticCustomDraw(togglePageButtonBarBounds, delegate (Context ctx, ImageSurface surface, ElementBounds bounds)
                    {
                        ctx.SetSourceRGBA(1.0, 1.0, 1.0, 0.06);
                        GuiElement.RoundRectangle(ctx, GuiElement.scaled(5.0) + bounds.bgDrawX, GuiElement.scaled(5.0) + bounds.bgDrawY, bounds.OuterWidth - GuiElement.scaled(10.0), GuiElement.scaled(75.0), 1.0);
                        ctx.Fill();
                    })
                    .BeginChildElements(); // begin paging bar
                int pageCt = AutoMapMarkerSettings[CurrentTab].Count / 12 + 1;
                CurrentPage = (CurrentPage <= 0) ? 1 : CurrentPage;
                for (int p = 1; p <= pageCt; p++)
                {
                    string pageName = $"{Lang.Get("egocarib-mapmarkers:page")} {p}";
                    CairoFont buttonFont = CairoFont.ButtonText();
                    int pVal = p;
                    SingleComposer.AddToggleButton(
                        text: pageName,
                        font: buttonFont,
                        onToggle: isSelected => OnPageToggle(pVal),
                        bounds: togglePageButtonBounds.WithFixedWidth(GetFontTextWidth(buttonFont, pageName)),
                        key: $"page-{p}-toggle-tab");
                    togglePageButtonBounds = togglePageButtonBounds.RightCopy(15, 0);
                }
                SingleComposer.GetToggleButton($"page-{CurrentPage}-toggle-tab").SetValue(true);
                SingleComposer.EndChildElements(); // end paging bar

                // Update paging bounds
                pagedOptionStartNum = (CurrentPage - 1) * 12 + 1;
                pagedOptionEndNum = pagedOptionStartNum + 11;

                // To accomodate paging toolbar we added, shift main content bounds downward and shrink its height
                mainContentBounds.fixedY += toggleBarHeight;
                mainContentBounds.fixedHeight -= toggleBarHeight;
            }

            SingleComposer
                .BeginChildElements(mainContentBounds);


            SingleComposer.BeginChildElements(markerOptionAreaBounds);

            ////TEST
            //if (CurrentTab == Lang.Get("egocarib-mapmarkers:organic-matter"))
            //{
            //    //ElementBounds markerOptionAreaBounds2 = ElementBounds.Fixed(0, (int)(yStart))
            //    //    .WithFixedPadding(rowIndent, 0)
            //    //    .WithSizing(ElementSizing.FitToChildren);
            //    //ElementBounds markerOptionAreaBounds3 = ElementBounds.Fixed(430, (int)(yStart))
            //    //    .WithFixedPadding(rowIndent, 0)
            //    //    .WithSizing(ElementSizing.FitToChildren);

            //    SingleComposer
            //    //.BeginChildElements(markerOptionAreaBounds2)
            //    .AddStaticCustomDraw(markerOptionAreaBounds.FlatCopy().WithParent(markerOptionAreaBounds), delegate (Context ctx, ImageSurface surface, ElementBounds bounds)
            //        {
            //            MessageUtil.Log("attempting to draw .png ...");
            //            surface.Image(floraBitmap, 0, 10, floraBitmap.Width, floraBitmap.Height);


            //            ////This header design copied from Vintagestory.Client.NoObf.GuiCompositeSettings.ComposerHeader
            //            //ctx.SetSourceRGBA(1.0, 1.0, 1.0, 0.1);
            //            //GuiElement.RoundRectangle(ctx, GuiElement.scaled(5.0) + bounds.bgDrawX, GuiElement.scaled(5.0) + bounds.bgDrawY, bounds.OuterWidth - GuiElement.scaled(10.0), GuiElement.scaled(75.0), 1.0);
            //            //ctx.Fill();
            //        });
                
            //    //.EndChildElements();
            //}
            ////TEST

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

                int optionCt = 0;
                foreach (var setting in settingGroup.Value)
                {
                    optionCt++;
                    if (needsPages && optionCt < pagedOptionStartNum || optionCt > pagedOptionEndNum)
                        continue;  // Paging is active and option shouldn't be shown on current page

                    string markerSettingTitle = setting.Key;
                    AutoMapMarkerSetting markerSetting = setting.Value;

                    double[] markerColorRGBADoubles = markerSetting.MarkerColorInteger.HasValue
                        ? ColorUtil.ToRGBADoubles(markerSetting.MarkerColorInteger.Value)
                        : new double[] { 1.0, 1.0, 1.0, 1.0};

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

                        //// Map marker icon
                        //.AddStaticText(
                        //    text: Lang.Get("egocarib-mapmarkers:icon"),
                        //    font: CairoFont.WhiteSmallishText(),
                        //    bounds: markerOptionIconLabelBounds.FlatCopy().WithParent(markerOptionRowBounds))
                        //.AddDropDown(
                        //    values: icons,
                        //    names: iconsVTML,
                        //    selectedIndex: GetIconIndex(icons, markerSetting),
                        //    onSelectionChanged: OnMarkerIconChanged,
                        //    bounds: markerOptionIconDropdownBounds.FlatCopy().WithParent(markerOptionRowBounds),
                        //    key: markerSettingTitle + "-auto-markers-icon")
                        .AddColorIconButton(
                            icon: GetWaypointIconName(markerSetting.MarkerIcon),
                            iconColor: markerColorRGBADoubles,
                            onClick: () => { return OnIconColorButtonClick(CurrentTab, markerSettingTitle, markerSetting); },
                            bounds: markerOptionIconColorButtonBounds.FlatCopy().WithParent(markerOptionRowBounds),
                            style: EnumButtonStyle.Normal,
                            key: markerSettingTitle + "-auto-markers-icon")
                        

                        //// Map marker color
                        //.AddStaticText(
                        //    text: Lang.Get("egocarib-mapmarkers:color"),
                        //    font: CairoFont.WhiteSmallishText(),
                        //    bounds: markerOptionColorLabelBounds.FlatCopy().WithParent(markerOptionRowBounds))
                        //.AddTextInput(
                        //    bounds: markerOptionColorInputBounds.FlatCopy().WithParent(markerOptionRowBounds),
                        //    onTextChanged: OnMarkerColorChanged,
                        //    font: CairoFont.TextInput(),
                        //    key: markerSettingTitle + "-auto-markers-color")
                        //.AddDynamicCustomDraw(
                        //    bounds: markerOptionColorPreviewBounds.FlatCopy().WithParent(markerOptionRowBounds),
                        //    onDraw: OnDrawColorRect,
                        //    key: markerSettingTitle + "-auto-markers-color-rect")

                        //// Map marker name
                        //.AddStaticText(
                        //    text: Lang.Get("egocarib-mapmarkers:name"),
                        //    font: CairoFont.WhiteSmallishText(),
                        //    bounds: markerOptionNameLabelBounds.FlatCopy().WithParent(markerOptionRowBounds))
                        //.AddTextInput(
                        //    bounds: markerOptionNameInputBounds.FlatCopy().WithParent(markerOptionRowBounds),
                        //    onTextChanged: OnMarkerNameChanged,
                        //    font: CairoFont.TextInput(),
                        //    key: markerSettingTitle + "-auto-markers-name")

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
                    //if (markerSetting.Enabled)
                    //{
                    //    //SingleComposer.GetTextInput(markerSettingTitle + "-auto-markers-color").SetValue(markerSetting.MarkerColor);
                    //    SingleComposer.GetTextInput(markerSettingTitle + "-auto-markers-name").SetValue(markerSetting.MarkerTitle);
                    //}

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

            //OnMarkerColorChanged(); // Force color tiles to be redrawn with correct colors.
        }

        ///// <summary>
        ///// Calculates the dropdown index of an icon associated with a given setting.
        ///// </summary>
        //private int GetIconIndex(string[] icons, AutoMapMarkerSetting setting)
        //{
        //    int index = Array.FindIndex(icons, i => i.Equals(setting.MarkerIcon, StringComparison.OrdinalIgnoreCase));
        //    if (index < 0)
        //    {
        //        // Reset icon setting if invalid
        //        index = 0;
        //        setting.MarkerIcon = icons[0];
        //    }
        //    return index;
        //}

        /// <summary>
        /// Called when the selected menu tab changes. Recomposes the GUI to draw the new menu screen.
        /// </summary>
        private void OnTabToggle(string tabName)
        {
            CurrentTab = tabName;
            CurrentPage = 1;
            SetupDialog();
        }

        /// <summary>
        /// Called when the selected page tab changes. Recomposes the GUI to draw the new menu screen.
        /// </summary>
        private void OnPageToggle(int pageNumber)
        {
            CurrentPage = pageNumber;
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

        ///// <summary>
        ///// Called when an icon dropdown choice changes. Updates the related map marker settings.
        ///// </summary>
        //private void OnMarkerIconChanged(string iconName, bool selected)
        //{
        //    foreach (var settingGroup in AutoMapMarkerSettings)
        //    {
        //        foreach (KeyValuePair<string, AutoMapMarkerSetting> settings in settingGroup.Value)
        //        {
        //            GuiElementSwitch enabledSwitch = SingleComposer.GetSwitch(settings.Key + "-auto-markers-enabled");
        //            if (enabledSwitch == null || !enabledSwitch.On)
        //            {
        //                continue; //this marker option not enabled & won't have icon fields.
        //            }
        //            settings.Value.MarkerIcon = SingleComposer.GetDropDown(settings.Key + "-auto-markers-icon").SelectedValue;
        //        }
        //    }
        //}

        ///// <summary>
        ///// Called when the color input text changes. Updates the related map marker settings.
        ///// </summary>
        //private void OnMarkerColorChanged(string colorstring = "")
        //{
        //    int colorTransparent = Color.Transparent.ToArgb();
        //    int colorBlack = Color.Black.ToArgb();
        //    bool saveEnabled = true;

        //    foreach (var settingGroup in AutoMapMarkerSettings)
        //    {
        //        foreach (KeyValuePair<string, AutoMapMarkerSetting> settings in settingGroup.Value)
        //        {
        //            GuiElementSwitch enabledSwitch = SingleComposer.GetSwitch(settings.Key + "-auto-markers-enabled");
        //            if (enabledSwitch == null || !enabledSwitch.On)
        //            {
        //                continue; //this marker option not enabled & won't have color fields.
        //            }
        //            GuiElementTextInput colorInput = SingleComposer.GetTextInput(settings.Key + "-auto-markers-color");
        //            GuiElementTextInput nameInput = SingleComposer.GetTextInput(settings.Key + "-auto-markers-name");
        //            GuiElementCustomDraw colorTile = SingleComposer.GetCustomDraw(settings.Key + "-auto-markers-color-rect");
        //            colorstring = colorInput.GetText();
        //            int? parsedColor = null;
        //            if (colorstring.StartsWith("#", StringComparison.Ordinal))
        //            {
        //                if (colorstring.Length == 7)
        //                {
        //                    string s = colorstring.Substring(1);
        //                    try
        //                    {
        //                        parsedColor = (int.Parse(s, NumberStyles.HexNumber) | colorBlack);
        //                    }
        //                    catch (Exception)
        //                    {
        //                    }
        //                }
        //            }
        //            else
        //            {
        //                Color color = Color.FromName(colorstring);
        //                if (color.A == byte.MaxValue)
        //                {
        //                    parsedColor = color.ToArgb();
        //                }
        //            }
        //            DynamicDrawColor = (parsedColor ?? colorTransparent);
        //            colorInput.Font.Color = (parsedColor.HasValue ? GuiStyle.DialogDefaultTextColor : GuiStyle.ErrorTextColor);
        //            bool nameValid = nameInput.GetText().Trim() != "";
        //            bool colorValid = parsedColor.HasValue;
        //            if (!nameValid || !colorValid)
        //            {
        //                saveEnabled = false;
        //            }
        //            colorTile.Redraw();
        //            if (colorValid)
        //            {
        //                settings.Value.MarkerColor = colorstring;
        //            }
        //        }
        //    }
        //    SetSaveButtonState(saveEnabled);
        //}

        ///// <summary>
        ///// Called when the name input text changes. Updates the related map marker settings.
        ///// </summary>
        //private void OnMarkerNameChanged(string name)
        //{
        //    bool saveEnabled = true;

        //    foreach (var settingGroup in AutoMapMarkerSettings)
        //    {
        //        foreach (KeyValuePair<string, AutoMapMarkerSetting> settings in settingGroup.Value)
        //        {
        //            GuiElementSwitch enabledSwitch = SingleComposer.GetSwitch(settings.Key + "-auto-markers-enabled");
        //            if (enabledSwitch == null || !enabledSwitch.On)
        //            {
        //                continue; //this marker option not enabled & won't have name fields.
        //            }
        //            //bool colorValid = SingleComposer.GetTextInput(settings.Key + "-auto-markers-color").Font.Color != GuiStyle.ErrorTextColor;
        //            name = SingleComposer.GetTextInput(settings.Key + "-auto-markers-name").GetText();
        //            bool nameValid = name.Trim() != "";
        //            //if (!colorValid || !nameValid)
        //            if (!nameValid)
        //            {
        //                saveEnabled = false;
        //            }
        //            if (nameValid)
        //            {
        //                settings.Value.MarkerTitle = name;
        //            }
        //        }
        //    }
        //    SetSaveButtonState(saveEnabled);
        //}

        private bool OnIconColorButtonClick(string settingGroup, string settingName, AutoMapMarkerSetting settings)
        {
            //MessageUtil.Log($"clicked button for category '{settingGroup}' and setting '{settingName}'");



            iconConfigPopup = new MapMarkerIconSettingsGUI(capi, settingName, settings, icons, colors);
            iconConfigPopup.TryOpen();
            iconConfigPopup.OnClosed += delegate
            {
                capi.Gui.RequestFocus(this);
                SetupDialog();  // Redraw GUI
            };
            //GuiDialogWorldMap mapdlg = capi.ModLoader.GetModSystem<WorldMapManager>().worldMapDlg;
            //editWpDlg = new GuiDialogEditWayPoint(capi, mapdlg.MapLayers.FirstOrDefault((MapLayer l) => l is WaypointMapLayer) as WaypointMapLayer, waypoint, waypointIndex);
            //editWpDlg.TryOpen();
            //editWpDlg.OnClosed += delegate
            //{
            //    capi.Gui.RequestFocus(mapdlg);
            //};
            //TODO: recompose gui to recolor and re-icon the button after changes...
            return true;
        }

        ///// <summary>
        ///// Enables or disables the Save button. Disabled when a color input is invalid or if no Name is specified for an option.
        ///// </summary>
        //private void SetSaveButtonState(bool enabled)
        //{
        //    GuiElementTextButton saveButton = SingleComposer.GetButton("auto-markers-saveButton");
        //    if (saveButton != null)
        //    {
        //        saveButton.Enabled = enabled;
        //    }
        //}

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
