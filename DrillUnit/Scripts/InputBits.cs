namespace DrillUnit
{
    /// <summary>
    /// The Drill Unit's action rides in bits 29 and 30 of the player's own input (ClientInput.buttonSetMask:
    /// vanilla uses bits 0-17, Build Tools 18-28 and 31). They are only set while the rider holds the use
    /// button (left click): 29 alone = drill + lay floor, 30 alone = drill + dig, both = drill only. It is written by
    /// <see cref="Patches.SendInputPatch"/>, which runs after Build Tools' input patch so neither wipes the other.
    /// </summary>
    public static class InputBits
    {
        public const uint GroundBit = 1u << 29;
        public const uint DigBit = 1u << 30;
        public const uint ActionBits = GroundBit | DigBit;

        /// <summary>Holding the use button: the unit drills.</summary>
        public static bool Drilling(uint mask) => (mask & ActionBits) != 0;
        /// <summary>Holding the use button in lay-floor mode.</summary>
        public static bool Laying(uint mask) => (mask & ActionBits) == GroundBit;
        /// <summary>Holding the use button in dig mode.</summary>
        public static bool Digging(uint mask) => (mask & ActionBits) == DigBit;
    }
}
