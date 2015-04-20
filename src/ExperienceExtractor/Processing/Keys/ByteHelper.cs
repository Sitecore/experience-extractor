using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExperienceExtractor.Processing.Keys
{    
    internal static class ByteUtil
    {
        /// <summary>
        /// Converts common known types to byte arrays for hash functions.
        /// Default behavior is to return the byte represenetation of v.GetHashCode()
        /// 
        /// Since GetHashCode() is 32-bit some information is lost when hashing 64-bit and 128-bit numbers and strings increasing the chance of hash collisions.
        /// By hashing the bytes from this method, collisions are less likely.
        /// 
        /// For example 8704292282190210790L and 2697783581641838223L has the same 32-bit hash code when using Int64's GetHashCode()
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        public static byte[] GetBytes(object v)
        {
            //TODO: Make platform independent. Different architectures may represent numbers differently (e.g. endianness)

            if( v == null) return new byte[0];

            if (v is string)
            {
                return Encoding.UTF8.GetBytes((string)v);
            }
            if (v is System.DateTime)
            {
                v = ((System.DateTime) v).Ticks;
            }
            if (v is long)
            {
                return BitConverter.GetBytes((long)v);
            }
            if (v is decimal)
            {
                var bytes = new List<byte>(16);
                foreach (var bit in decimal.GetBits((decimal)v))
                {
                    bytes.AddRange(BitConverter.GetBytes(bit));
                }
                return bytes.ToArray();
            }
            if (v is Guid)
            {
                return ((Guid)v).ToByteArray();
            }
            if (v is double)
            {
                return BitConverter.GetBytes((double)v);
            }

            return BitConverter.GetBytes(v.GetHashCode());
        }
    }

}
