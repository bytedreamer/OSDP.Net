using OSDP.Net.Messages;

namespace OSDP.Net.Model.CommandData
{
    /// <summary>
    /// Base class representing a paylod of a PD command message
    /// </summary>
    public abstract class CommandData : PayloadData 
    {
        /// <summary>
        /// Message command code
        /// </summary>
        public abstract CommandType CommandType { get; }
    }
}
