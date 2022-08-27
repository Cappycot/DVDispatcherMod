using System.Collections.Generic;
using System.Linq;

namespace DVDispatcherMod.Extensions {
    public static class EnumerableExtensions {
        public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T> enumerable) where T : class {
            return enumerable.Where(item => item != null);
        }
    }
}