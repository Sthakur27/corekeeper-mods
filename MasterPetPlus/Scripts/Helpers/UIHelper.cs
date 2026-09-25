using UnityEngine;
using MasterPet.UI;

namespace MasterPet.Helpers
{
    public static class UIHelper
    {
        public static void FitSpriteToSize(SpriteRenderer spriteRenderer, Sprite sprite, float targetSize)
        {
            if (spriteRenderer == null || sprite == null) return;

            spriteRenderer.drawMode = SpriteDrawMode.Simple;
            spriteRenderer.sprite = sprite;
            spriteRenderer.color = Color.white;

            float width = sprite.rect.width / sprite.pixelsPerUnit;
            float height = sprite.rect.height / sprite.pixelsPerUnit;
            float largestDimension = Mathf.Max(width, height);

            if (largestDimension <= 0f) return;

            float scale = targetSize / largestDimension;
            spriteRenderer.transform.localScale = new Vector3(scale, scale, 1f);
        }

        public static void FillGradientTexture(Texture2D texture, GradientMapDataBlock gradient)
        {
            if (texture == null || gradient == null || !gradient.hasData) return;

            if (texture.width != gradient.textureWidth)
                texture.Reinitialize(gradient.textureWidth, 1);

            Color32[] pixels = new Color32[gradient.textureWidth];

            for (int x = 0; x < gradient.textureWidth; x++)
                pixels[x] = gradient.GetPixel(x);

            texture.SetPixels32(pixels);
            texture.Apply();
        }

        public static Transform FindChildRecursive(Transform parent, string childName)
        {
            if (parent == null) return null;

            if (parent.name == childName) return parent;

            for (int i = 0; i < parent.childCount; i++)
            {
                Transform result = FindChildRecursive(parent.GetChild(i), childName);
                if (result != null) return result;
            }

            return null;
        }

        public static bool NeedsLargeIcon(ObjectID objectID)
        {
            return objectID == ObjectID.PetMagic;
        }

        public static float GetPetTargetSize(ObjectID objectID)
        {
            if (objectID == ObjectID.PetTardigrade || objectID == ObjectID.PetMagic)
                return 0.625f;

            if (objectID == ObjectID.PetMoth || objectID == ObjectID.PetElectric)
                return 0.875f;

            return 1f;
        }

        public static void SetResetPositions(GameObject petTalentsWindow)
        {
            if (petTalentsWindow == null) return;

            Transform resetButton = FindChildRecursive(petTalentsWindow.transform, "resetButton");
            Transform resetText = FindChildRecursive(petTalentsWindow.transform, "resetText");

            if (resetButton != null)
                resetButton.localPosition = new Vector3(-1f, -4.6875f, 0f);

            if (resetText != null)
                resetText.localPosition = new Vector3(-1f, -3.8125f, 0f);
        }

        public static GameObject InjectOpenMasterPetButton(GameObject openButtonPrefab, GameObject petTalentsWindow)
        {
            if (openButtonPrefab == null || petTalentsWindow == null) return null;

            Transform root = petTalentsWindow.transform.Find("root");
            if (root == null) return null;

            Transform resetButton = root.Find("resetButton");
            if (resetButton == null) return null;

            GameObject button = Object.Instantiate(openButtonPrefab, resetButton.parent);
            button.name = "OpenMasterPetButton";

            var openButton = button.GetComponent<OpenMasterPetButton>();
            if (openButton != null)
                openButton.Setup();

            UIelement openUE = button.GetComponent<UIelement>();
            UIelement resetUE = resetButton.GetComponent<UIelement>();
            UIelement t8UE = root.Find("Talents/PetTalentUIElement (8)")?.GetComponent<UIelement>();
            UIelement t2UE = root.Find("Talents/PetTalentUIElement (2)")?.GetComponent<UIelement>();

            if (openUE != null)
            {
                if (resetUE != null)
                {
                    openUE.leftUIElements.Add(resetUE);
                    openUE.rightUIElements.Add(resetUE);
                    resetUE.rightUIElements.Add(openUE);
                }

                if (t8UE != null)
                {
                    openUE.topUIElements.Add(t8UE);
                    t8UE.bottomUIElements.Add(openUE);
                }

                if (t2UE != null)
                    openUE.bottomUIElements.Add(t2UE);
            }

            return button;
        }
    }
}