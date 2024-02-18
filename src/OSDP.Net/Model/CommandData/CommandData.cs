using System;
using System.Collections.Generic;
using OSDP.Net.Messages;

namespace OSDP.Net.Model.CommandData
{
    /// <summary>
    /// Base class representing a payload of a PD command message
    /// </summary>
    public abstract class CommandData : PayloadData
    {
        /// <summary>
        /// Message command code
        /// </summary>
        public abstract CommandType CommandType { get; }

        /// <summary>
        /// Splits data into multiple elements of type T based on a specified size.
        /// </summary>
        /// <typeparam name="T">The type of elements in the resulting array.</typeparam>
        /// <param name="size">The size of each element in bytes.</param>
        /// <param name="parseDataFunction">The function used to parse each element of type T from a byte array.</param>
        /// <param name="payloadData">The payload data to be split.</param>
        /// <returns>An array of elements of type T.</returns>
        protected static T[] SplitData<T>(int size, Func<byte[], T> parseDataFunction, ReadOnlySpan<byte> payloadData)
        {
            var data = new List<T>();
            for (int index = 0; index + size <= payloadData.Length; index += size)
            {
                data.Add(parseDataFunction(payloadData.Slice(index, size).ToArray()));
            }

            return data.ToArray();
        }
    }
}
