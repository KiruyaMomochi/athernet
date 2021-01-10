namespace Athernet.Sockets
{
    public enum TcpState
    {
        Closed,
        SynSent,
        Established,
        WaitAck,
        CloseWait,
        LastAck
    }
}