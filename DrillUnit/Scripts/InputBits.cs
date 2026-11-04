namespace DrillUnit
{
    /// <summary>
    /// The Drill Unit's lay-ground toggle rides in bit 29 of the player's own input
    /// (ClientInput.buttonSetMask: vanilla uses bits 0-17, Build Tools 18-28 and 31). It is written by
    /// <see cref="Patches.SendInputPatch"/>, which runs after Build Tools' input patch so neither wipes the other.
    /// </summary>
    public static class InputBits
    {
        public const uint GroundBit = 1u << 29;
    }
}
