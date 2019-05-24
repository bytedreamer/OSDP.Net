namespace OSDP.Net.Messages
{
    public abstract class CommandBase
    {
        byte[] BuildCommand( )
        {
            return new byte[]{};
        }
    }
}