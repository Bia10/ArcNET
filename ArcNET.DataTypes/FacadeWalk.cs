using System;
using System.Runtime.InteropServices;

namespace ArcNET.DataTypes
{
    public class FacadeWalk
    {
        public FacWalkMarker Marker;
        public FacWalkHeader Header;
        public FacWalkEntry[] Entries;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct FacWalkMarker
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 14)]
        public string fileMarker; // fileTypeMarker 'F', 'a', 'c', 'W', 'a', 'l', 'k', ' ', + fileTypeVersion 'V', '1', '0', '1', ' ', ' '
    }

    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct FacWalkHeader
    {
        // base terrain, index + outdoor + flippable
        [MarshalAs(UnmanagedType.U4)]
        public uint terrain; // index to tilename.mes
        [MarshalAs(UnmanagedType.U4)]
        public uint outdoor; // boolean, 1 = outdoor
        [MarshalAs(UnmanagedType.U4)]
        public uint flippable; // boolean, 1 = flippable
        [MarshalAs(UnmanagedType.U4)]
        public uint width; // width of facade, isometric
        [MarshalAs(UnmanagedType.U4)]
        public uint height; // height of facade, isometric
        [MarshalAs(UnmanagedType.U4)]
        public uint entryCount; // equals to number of frames of equivalent Art file.
    }

    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct FacWalkEntry
    {
        [MarshalAs(UnmanagedType.U4)]
        public uint x; // x position
        [MarshalAs(UnmanagedType.U4)]
        public uint y; // y position
        [MarshalAs(UnmanagedType.U4)]
        public uint walkable; // boolean, 0 = blocked
    }
}
