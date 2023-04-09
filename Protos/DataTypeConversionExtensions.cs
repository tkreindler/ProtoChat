using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Protos
{
    internal static class DataTypeConversionExtensions
    {
        public static string? ToObject(this Protos.NullableString? input)
        {
            return input?.Val;
        }
        public static Protos.NullableString? ToProto(this string? input)
        {
            if (input is null)
            {
                return null;
            }

            return new NullableString
            {
                Val = input,
            };
        }
    }
}
