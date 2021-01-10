namespace Athernet.AppLayer.AthernetFTPClient.DataStructure
{
    public enum StatusCodeClass : int
    {
        PositivePreliminaryReply = 1,
        PositiveCompletionReply = 2,
        PositiveIntermediateReply = 3,
        TransientNegativeCompletionReply = 4,
        PermanentNegativeCompletionReply = 5
    }

    public class StatusCode
    {
        //static public FtpStatusCode FTPState;

        public const int LengthNumber = 3;
    }
}
