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

        public abstract ReadOnlySpan<byte> SecurityControlBlock();
        
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
