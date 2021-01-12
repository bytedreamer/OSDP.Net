using System;
using System.Collections.Generic;
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

        public IEnumerable<bool> InputStatuses { get; private set; }

        internal static InputStatus ParseData(ReadOnlySpan<byte> data)
        {
            return new InputStatus {InputStatuses = data.ToArray().Select(Convert.ToBoolean)};
        }

        public override string ToString()
        {
            byte inputNumber = 0;
            var build = new StringBuilder();
            foreach (bool inputStatus in InputStatuses)
            {
                build.AppendLine($"Input Number {inputNumber++:00}: {inputStatus}");
            }

            return build.ToString();
        }
    }
}