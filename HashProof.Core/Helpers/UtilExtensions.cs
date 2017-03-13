using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HashProof.Core.Helpers
{
    public static class UtilExtensions
    {
        public static IEnumerable<IEnumerable<T>> Chunk<T>(this IEnumerable<T> source, int length)
        {
            if (length <= 0)
                throw new ArgumentOutOfRangeException("length");

            var section = new List<T>(length);

            foreach (var item in source)
            {
                section.Add(item);

                if (section.Count == length)
                {
                    yield return section.AsReadOnly();
                    section = new List<T>(length);
                }
            }

            if (section.Count > 0)
                yield return section.AsReadOnly();
        }
    }
}
