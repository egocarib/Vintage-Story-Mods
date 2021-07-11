using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace Egocarib.AutoMapMarkers.GUI
{
    /// <summary>
    /// Mod system for our mod's settings GUI. Loaded only on the client side.
    /// </summary>
    public class MapMarkerGUISystem : ModSystem
    {
        GuiDialog mapMarkerDialog;

        public override bool ShouldLoad(EnumAppSide forSide)
        {
            return forSide == EnumAppSide.Client;
        }

        public override void StartClientSide(ICoreClientAPI clientAPI)
        {
            base.StartClientSide(clientAPI);

            mapMarkerDialog = new MapMarkerGUI(clientAPI);

            clientAPI.Input.RegisterHotKey(
                hotkeyCode: MapMarkerGUI.HotkeyCode,
                name: Lang.Get("egocarib-mapmarkers:config-keybind-name"),
                key: GlKeys.M,
                type: HotkeyType.GUIOrOtherControls,
                altPressed: false,
                ctrlPressed: true,
                shiftPressed: true);
            clientAPI.Input.SetHotKeyHandler(
                hotkeyCode: MapMarkerGUI.HotkeyCode,
                handler: ToggleGUI);
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
    }
}
