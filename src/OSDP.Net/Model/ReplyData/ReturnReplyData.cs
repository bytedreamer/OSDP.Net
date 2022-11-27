namespace OSDP.Net.Model.ReplyData
{
    /// <summary>
    /// Reply from a command that has different possible results.
    /// </summary>
    /// <typeparam name="T">Expected reply data type</typeparam>
    public class ReturnReplyData<T>
    {
        /// True if Ack reply is returned
        public bool Ack { get; internal set; }

        /// Contains expected reply data type if returned
        public T ReplyData { get; internal set; }

        /// <inheritdoc />
        public override string ToString()
        {
            if (Ack)
            {
                return "Ack";
            }

            return ReplyData?.ToString();
        }
    }
}