namespace OSDP.Net.Model.ReplyData
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T">Expected reply data type</typeparam>
    public class ReturnReplyData<T>
    {
        // True if Ack reply is returned
        public bool Ack { get; internal set; }

        // Contains Nak data if returned
        public Nak Nak { get; internal set; }

        // Contains expected reply data type if returned
        public T ReplyData { get; internal set; }
    }
}