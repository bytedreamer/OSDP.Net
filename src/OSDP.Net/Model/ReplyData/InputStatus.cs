using System;
using System.Linq;
using System.Text;
using OSDP.Net.Messages;

namespace OSDP.Net.Model.ReplyData
{
    public class InputStatus
    {
        private InputStatus()
        {
        }

        public bool[] InputStatuses { get; private set; }

        internal static InputStatus ParseData(Reply reply)
        {
            var data = reply.ExtractReplyData.ToArray();

            return new InputStatus {InputStatuses = data.Select(Convert.ToBoolean).ToArray()};
        }

        public override string ToString()
        {
            byte inputNumber = 1;
            var build = new StringBuilder();
            foreach (bool inputStatus in InputStatuses)
            {
                build.AppendLine($"Input Number {inputNumber++:00}: {inputStatus}");
            }

            return build.ToString();
        }
    }
}