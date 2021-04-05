using System.IO;
using System.Runtime.InteropServices;

namespace ArcNET.Utilities
{
    public static class Marshalling
    {
        public static T ByteArrayToStructure<T>(BinaryReader reader)
        {
            var count = Marshal.SizeOf(typeof(T));
            var bytes = reader.ReadBytes(count);
            var handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
            var stuff = (T)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(T));
            handle.Free();

            return stuff;
        }
    }
}