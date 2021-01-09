using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Athernet.AppLayer.FTPClient
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
