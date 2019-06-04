namespace OSDP.Net.Messages
{
    public class PollCommand : Command
    {
        public PollCommand(byte address, Control control)
        {
            Address = address;
            Control = control;
        }

        protected override byte CommandCode => 0x60;
        protected override byte Address { get; }
        public override Control Control { get; }
    }
}