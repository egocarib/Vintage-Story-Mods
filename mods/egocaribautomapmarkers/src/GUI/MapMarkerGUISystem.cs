using Egocarib.AutoMapMarkers.Settings;
using Egocarib.AutoMapMarkers.Utilities;
using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace Egocarib.AutoMapMarkers.GUI
{
    /// <summary>
    /// Mod system for our mod's settings GUI. Loaded only on the client side.
    /// </summary>
    public class MapMarkerGUISystem : ModSystem
    {
        private GuiDialog mapMarkerDialog;
        public ICoreClientAPI ClientAPI;
        public const string HotkeyCustom1 = "egocarib_hkCustomMarker1";
        public const string HotkeyCustom2 = "egocarib_hkCustomMarker2";
        public const string HotkeyCustom3 = "egocarib_hkCustomMarker3";
        public const string HotkeyDeleteMarker = "egocarib_hkDeleteNearestMarker";
        private bool RegisteredCustomHotkeys = false;
        private bool RegisteredDeleteHotkey = false;

        public override bool ShouldLoad(EnumAppSide forSide)
        {
            return forSide == EnumAppSide.Client;
        }

        public override void StartClientSide(ICoreClientAPI clientAPI)
        {
            ClientAPI = clientAPI;
            mapMarkerDialog = new MapMarkerGUI(ClientAPI, RegisterCustomHotkeys, RegisterDeleteHotkey);
            ClientAPI.Event.BlockTexturesLoaded += RegisterHotkeys;
        }

        private void RegisterHotkeys()
        {
            ClientAPI.Input.RegisterHotKey(
                hotkeyCode: MapMarkerGUI.HotkeyCode,
                name: Lang.Get("egocarib-mapmarkers:config-keybind-name"),
                key: GlKeys.M,
                type: HotkeyType.GUIOrOtherControls,
                altPressed: false, ctrlPressed: true, shiftPressed: true);

            ClientAPI.Input.SetHotKeyHandler(hotkeyCode: MapMarkerGUI.HotkeyCode, handler: ToggleGUI);

            MapMarkerConfig.Settings settings = MapMarkerConfig.GetSettings(ClientAPI, false, false);
            if (settings != null)
            {
                if (settings.EnableCustomHotkeys)
                {
                    RegisterCustomHotkeys();
                }
                if (settings.EnableWaypointDeletionHotkey)
                {
                    RegisterDeleteHotkey();
                }
            }
        }

        private void RegisterCustomHotkeys()
        {
            if (!RegisteredCustomHotkeys)
            {
                RegisteredCustomHotkeys = true;
                string prefix = Lang.Get("egocarib-mapmarkers:modname") + ": ";

                ClientAPI.Input.RegisterHotKey(
                    hotkeyCode: HotkeyCustom1,
                    name: prefix + Lang.Get("egocarib-mapmarkers:config-keybind-custom1"),
                    key: GlKeys.Keypad1,
                    type: HotkeyType.GUIOrOtherControls,
                    altPressed: true, ctrlPressed: true, shiftPressed: false);
                ClientAPI.Input.RegisterHotKey(
                    hotkeyCode: HotkeyCustom2,
                    name: prefix + Lang.Get("egocarib-mapmarkers:config-keybind-custom2"),
                    key: GlKeys.Keypad2,
                    type: HotkeyType.GUIOrOtherControls,
                    altPressed: true, ctrlPressed: true, shiftPressed: false);
                ClientAPI.Input.RegisterHotKey(
                    hotkeyCode: HotkeyCustom3,
                    name: prefix + Lang.Get("egocarib-mapmarkers:config-keybind-custom3"),
                    key: GlKeys.Keypad3,
                    type: HotkeyType.GUIOrOtherControls,
                    altPressed: true, ctrlPressed: true, shiftPressed: false);

                ClientAPI.Input.SetHotKeyHandler(hotkeyCode: HotkeyCustom1, handler: AddCustomMapMarker1);
                ClientAPI.Input.SetHotKeyHandler(hotkeyCode: HotkeyCustom2, handler: AddCustomMapMarker2);
                ClientAPI.Input.SetHotKeyHandler(hotkeyCode: HotkeyCustom3, handler: AddCustomMapMarker3);
            }
        }

        private void RegisterDeleteHotkey()
        {
            if (!RegisteredDeleteHotkey)
            {
                RegisteredDeleteHotkey = true;
                string prefix = Lang.Get("egocarib-mapmarkers:modname") + ": ";

                ClientAPI.Input.RegisterHotKey(
                    hotkeyCode: HotkeyDeleteMarker,
                    name: prefix + Lang.Get("egocarib-mapmarkers:config-keybind-delete-nearest"),
                    key: GlKeys.Keypad0,
                    type: HotkeyType.GUIOrOtherControls,
                    altPressed: true, ctrlPressed: true, shiftPressed: false);

                ClientAPI.Input.SetHotKeyHandler(hotkeyCode: HotkeyDeleteMarker, handler: DeleteNearestMapMarker);
            }
        }

        /// <summary>
        /// Opens or closes the Map Marker GUI when the hotkey is pressed.
        /// </summary>
        private bool ToggleGUI(KeyCombination keyCombo)
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

        public bool AddCustomMapMarker1(KeyCombination keyCombo) => AddCustomMapMarker(1);
        public bool AddCustomMapMarker2(KeyCombination keyCombo) => AddCustomMapMarker(2);
        public bool AddCustomMapMarker3(KeyCombination keyCombo) => AddCustomMapMarker(3);

        /// <summary>
        /// Event raised when the player presses a custom map marker hotkey.
        /// Validates settings and then kicks off the creation request
        /// </summary>
        public bool AddCustomMapMarker(int index)
        {
            try
            {
                var settings = MapMarkerConfig.GetSettings(ClientAPI);
                if (settings.EnableCustomHotkeys)
                {
                    var customMarkerSettings = settings.AutoMapMarkers.Custom.SettingByIndex(index);
                    if (customMarkerSettings.Enabled)
                    {
                        EntityPos pos = ClientAPI.World.Player.Entity.Pos;
                        if (pos != null)
                        {
                            MapMarkerMod.Network.RequestWaypointFromServer(
                                position: pos.XYZ,
                                settings: customMarkerSettings,
                                sendChatMessage: settings.ChatNotifyOnWaypointCreation);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                MessageUtil.LogError($"Exception attempting to add custom map marker #{index} - {e.Message}");
            }
            return true;
        }

        /// <summary>
        /// Event raised when the player presses the hotkey to delete nearest waypoint.
        /// Validates settings and then kicks off the deletion request.
        /// </summary>
        public bool DeleteNearestMapMarker(KeyCombination keyCombo)
        {
            var settings = MapMarkerConfig.GetSettings(ClientAPI);
            if (settings.EnableWaypointDeletionHotkey)
            {
                bool chatNotify = settings.ChatNotifyOnWaypointDeletion;
                MapMarkerMod.Network.RequestNearestWaypointDeletionFromServer(chatNotify);
            }
            else
            {
                MessageUtil.Chat("Auto Map Markers: You pressed the delete map marker hotkey, but deleting map markers is not"
                    + " enabled in the mod settings.");
            }
            return true;
        }
    }
}
