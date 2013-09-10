namespace LibMSPackN
{
    using System;

    /// <summary>
    /// Used with <see cref="MSCabinet.Flags"/>
    /// </summary>
    [Flags]
    public enum MSCabinetFlags
    {
        /** Cabinet header flag: cabinet has a predecessor */
        MscabHdrPrevcab = 0x01,
        /** Cabinet header flag: cabinet has a successor */
        MscabHdrNextcab = 0x02,
        /** Cabinet header flag: cabinet has reserved header space */
        MscabHdrResv = 0x04
    }
}