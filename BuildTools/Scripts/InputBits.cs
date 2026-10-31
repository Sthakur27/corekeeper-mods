namespace BuildTools
{
    /// <summary>
    /// Build Tools state rides in the unused high bits of the player's own input
    /// (ClientInput.buttonSetMask: vanilla uses bits 0-17, the whole uint is sent every tick and the
    /// server and the client's prediction both see it). No chat commands, no extra RPCs, and the client
    /// can predict instantly. Written by <see cref="Patches.SendInputPatch"/>; a client without the mod
    /// sends 0, so <see cref="Present"/> is false and every system leaves that player alone.
    ///
    /// Layout: 31 present | 27-28 roof mode | 23-26 size | 21-22 brush shape | 19-20 block mode | 18 replace.
    /// "size" is the wanted variation of the held tool (15 = no wish) or, for placeables, the brush edge.
    /// </summary>
    public struct InputBits
    {
        public const uint VanillaMask = (1u << 18) - 1;
        private const uint PresentBit = 1u << 31;
        private const uint ReplaceBit = 1u << 18;
        public const int NoSize = 15;

        public bool Present;
        public bool Replace;
        public BlockMode Block;
        public BrushShape Shape;
        public int Size;
        public RoofMode Roof;

        public uint Encode()
        {
            uint m = PresentBit;
            if (Replace) m |= ReplaceBit;
            m |= ((uint)Block & 3u) << 19;
            m |= ((uint)Shape & 3u) << 21;
            m |= ((uint)Size & 15u) << 23;
            m |= ((uint)Roof & 3u) << 27;
            return m;
        }

        public static InputBits Decode(uint mask)
        {
            if ((mask & PresentBit) == 0) return default;
            uint block = (mask >> 19) & 3u, roof = (mask >> 27) & 3u;
            return new InputBits
            {
                Present = true,
                Replace = (mask & ReplaceBit) != 0,
                Block = (BlockMode)(block > (uint)BlockMode.Wall ? 0 : block),
                Shape = (BrushShape)((mask >> 21) & 3u),
                Size = (int)((mask >> 23) & 15u),
                Roof = (RoofMode)(roof > (uint)RoofMode.ClearRoof ? 0 : roof),
            };
        }
    }
}
