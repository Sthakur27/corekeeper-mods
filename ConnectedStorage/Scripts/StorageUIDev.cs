using UnityEngine;
namespace ConnectedStorage {
    // Fixed shipping layout. Opt-in capture tooling lives in Dev/Runtime instead.
    public static class StorageUIDev {
        public static void Init() { }
        public static Vector2 Browser => new Vector2(-7.5f,.5f);
        public static Vector2 Bag => new Vector2(6.75f,-1f);
        public static float BarY => 2f;
        public static float PanelScale => 1f;
        public static float TextScale => 1f;
        public static void Opened() { }
        public static void Tick(GameObject browser) { }
    }
}
