using System.Collections.Generic;
using System.Linq;

namespace ArcNET.Utilities
{
    public static class Enumeration
    {
        public static IEnumerable<(T item, int index)> WithIndex<T>(this IEnumerable<T> self) 
            => self?.Select((item, index) 
            => (item, index)) 
            ?? Enumerable.Empty<(T, int)>();
    }
}