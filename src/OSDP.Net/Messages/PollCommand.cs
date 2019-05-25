namespace OSDP.Net.Messages
{
    public class PollCommand : CommandBase
    {
        protected override byte CommandCode => 0x60;
    }
}