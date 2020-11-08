namespace Athernet.MacLayer
{
    public enum MacType: byte
    {
        Data,
        Ack,
        // ReSharper disable once IdentifierTypo
        MacpingReq,
        // ReSharper disable once IdentifierTypo
        MacpingReply,
        ReTrans
    }
}