using HarmonyLib;

namespace BuildTools
{
    /// <summary>
    /// Local chat line for mode changes ("Blocks: ground only"). Only on this client, nothing is sent.
    /// The chat window's line allocator is private, but its public AddInfoText allocates a line and
    /// renders it from a template; while <see cref="Pending"/> is set, that render draws our text instead.
    /// </summary>
    public static class ChatNote
    {
        internal static string Pending;

        public static void Show(string text)
        {
            var chat = Manager.ui != null ? Manager.ui.chatWindow : null;
            if (chat == null) return;
            Pending = text;
            try
            {
                chat.AddInfoText(null, ChatWindow.MessageTextType.Received);
            }
            finally
            {
                Pending = null;
            }
        }
    }

    [HarmonyPatch(typeof(PugText), nameof(PugText.Render), new[] { typeof(bool), typeof(bool) })]
    public static class ChatNoteRenderPatch
    {
        [HarmonyPrefix]
        public static bool Prefix(PugText __instance, bool activate)
        {
            if (ChatNote.Pending == null) return true;
            string text = ChatNote.Pending;
            ChatNote.Pending = null;
            __instance.Render(text, false, false, activate);
            return false;
        }
    }
}
