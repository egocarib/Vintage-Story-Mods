using Egocarib.AutoMapMarkers.Utilities;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace Egocarib.AutoMapMarkers.Patches
{
    /// <summary>
    /// Hides the vanilla server's waypoint command confirmations when they were caused by an
    /// automatic map marker.
    /// </summary>
    /// <remarks>
    /// When the mod isn't installed on the server, markers are created by sending the vanilla
    /// "/waypoint addati ..." chat command. The server answers every one of those with a chat line
    /// ("Ok, waypoint nr. 12 added"), which would turn routine berry picking into chat spam.
    ///
    /// This can't be done through ICoreClientAPI.Event.ChatMessage - that event uses ChatLineDelegate,
    /// which has no way to cancel a message (only the outgoing OnSendChatMessage event does). So the
    /// client's chat line handler gets a Harmony prefix instead.
    ///
    /// Suppression is deliberately narrow: it only applies while a command we sent is awaiting its
    /// reply, only to command success/error lines, and only to messages matching the game's own
    /// waypoint command responses in the active language.
    /// </remarks>
    public static class ChatSuppression
    {
        /// <summary>
        /// How long after sending a command we are willing to swallow its reply.
        /// </summary>
        private const long SuppressionWindowMs = 5000;

        /// <summary>
        /// Lang keys for every response the vanilla waypoint add/remove commands can produce.
        /// Suppressing the failures too keeps a misconfigured marker from spamming chat; the
        /// suppressed text is written to the client log instead.
        /// </summary>
        private static readonly string[] SuppressedLangKeys =
        {
            "Ok, waypoint nr. {0} added",
            "Ok, deleted waypoint.",
            "You have no waypoints to delete",
            "No such waypoint found",
            "Invalid waypoint number, valid ones are 0..{0}",
            "command-modwaypoint-invalidindex",
            "command-waypoint-invalidcolor",
            "command-waypoint-notext",
            "Invalid position. Syntax: /waypoint addati icon x y z pinned color title",
        };

        private static Regex[] suppressedPatterns;
        private static int pendingReplies;
        private static long suppressUntilMs;
        private static bool patchApplied;

        /// <summary>
        /// Marks that a waypoint command has just been sent and its reply should be hidden.
        /// </summary>
        public static void ArmSuppression()
        {
            var capi = MapMarkerMod.CoreClientAPI;
            if (capi == null || !patchApplied)
                return;

            pendingReplies++;
            suppressUntilMs = capi.World.ElapsedMilliseconds + SuppressionWindowMs;
        }

        /// <summary>
        /// Applies the prefix. Safe to call when the game's internals have moved: it logs and gives
        /// up, leaving the mod fully functional but chattier.
        /// </summary>
        public static void ApplyPatch(Harmony harmony)
        {
            if (patchApplied || harmony == null)
                return;

            MethodInfo target = ResolveTarget();
            if (target == null)
            {
                MessageUtil.Log("Could not find the client's chat line handler. Waypoint confirmations from"
                    + " the server will be visible in chat.");
                return;
            }

            try
            {
                var prefix = new HarmonyMethod(typeof(ChatSuppression).GetMethod(
                    nameof(SuppressWaypointReplyPrefix), BindingFlags.NonPublic | BindingFlags.Static));
                harmony.Patch(target, prefix: prefix);
                patchApplied = true;
            }
            catch (Exception e)
            {
                MessageUtil.LogError($"Unable to suppress server waypoint confirmations - {e.Message}");
            }
        }

        public static void Reset()
        {
            patchApplied = false;
            pendingReplies = 0;
            suppressUntilMs = 0;
            suppressedPatterns = null;
        }

        /// <summary>
        /// Both candidates take (int groupId, string message, EnumChatType chattype, string data).
        /// The chat HUD's own handler is preferred because blocking there leaves the message visible
        /// to other mods listening on ICoreClientAPI.Event.ChatMessage; the event manager's trigger
        /// is the fallback if that private method is ever renamed.
        /// </summary>
        private static MethodInfo ResolveTarget()
        {
            var candidates = new (string typeName, string methodName)[]
            {
                ("Vintagestory.Client.NoObf.HudDialogChat", "OnNewServerToClientChatLine"),
                ("Vintagestory.Client.NoObf.ClientEventManager", "TriggerNewServerChatLine"),
            };

            foreach (var (typeName, methodName) in candidates)
            {
                Type type = AccessTools.TypeByName(typeName);
                if (type == null)
                    continue;

                MethodInfo method = AccessTools.Method(type, methodName,
                    new[] { typeof(int), typeof(string), typeof(EnumChatType), typeof(string) });
                if (method != null)
                    return method;
            }

            return null;
        }

        /// <summary>
        /// Harmony prefix. Parameters are taken positionally (__1 / __2) because the two candidate
        /// targets spell their parameters differently ("groupId" vs "groupid").
        /// </summary>
        private static bool SuppressWaypointReplyPrefix(string __1, EnumChatType __2)
        {
            try
            {
                if (!ShouldSuppress(__1, __2))
                    return true;

                pendingReplies--;
                MessageUtil.Log($"Suppressed waypoint command reply: \"{__1}\"");
                return false;
            }
            catch (Exception)
            {
                return true;  // Never let a suppression bug swallow the player's chat
            }
        }

        private static bool ShouldSuppress(string message, EnumChatType chatType)
        {
            if (pendingReplies <= 0 || string.IsNullOrEmpty(message))
                return false;
            if (chatType != EnumChatType.CommandSuccess && chatType != EnumChatType.CommandError)
                return false;

            var capi = MapMarkerMod.CoreClientAPI;
            if (capi == null)
                return false;

            if (capi.World.ElapsedMilliseconds > suppressUntilMs)
            {
                pendingReplies = 0;  // Replies we were waiting for never arrived; stop watching
                return false;
            }

            foreach (Regex pattern in GetSuppressedPatterns())
            {
                if (pattern.IsMatch(message))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Builds match patterns from the translated command responses, so suppression works in
        /// whatever language the client is running. "{0}" placeholders become wildcards.
        /// </summary>
        private static Regex[] GetSuppressedPatterns()
        {
            if (suppressedPatterns != null)
                return suppressedPatterns;

            var patterns = new List<Regex>();
            foreach (string key in SuppressedLangKeys)
            {
                string translated = Lang.GetUnformatted(key);
                if (string.IsNullOrEmpty(translated))
                    continue;

                // Regex.Escape turns "{0}" into "\{0}"; turn those placeholders back into wildcards.
                string regex = "^" + Regex.Escape(translated) + "$";
                regex = Regex.Replace(regex, @"\\\{\d+\}", ".*");
                patterns.Add(new Regex(regex, RegexOptions.Compiled));
            }

            suppressedPatterns = patterns.ToArray();
            return suppressedPatterns;
        }
    }
}
