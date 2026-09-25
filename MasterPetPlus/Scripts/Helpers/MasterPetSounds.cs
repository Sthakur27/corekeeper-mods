using UnityEngine;

namespace MasterPet.Helpers
{
    public static class MasterPetSounds
    {
        public static void PlayOpen()
        {
            AudioManager.SfxUI(SfxID.FIXME_menu_select, 0.6f, false, 1f, 0f);
        }

        public static void PlayClose()
        {
            AudioManager.SfxUI(SfxID.FIXME_menu_select, 0.6f, false, 1f, 0f);
        }

        public static void PlayButtonClick()
        {
            AudioManager.SfxUI(SfxID.FIXME_menu_select, 0.6f, false, 1f, 0f);
        }

        public static void PlayDenied()
        {
            AudioManager.SfxUI(SfxID.menu_denied, 1f, false, 1f, 0f);
        }
    }
}