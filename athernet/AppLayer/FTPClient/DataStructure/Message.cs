﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Athernet.AppLayer.FTPClient
{
    public class Message
    {
        public FtpStatusCode StatusCode { get; set; }
        public System.String FullMessage { get; set; }

        public Message(System.String CodeText, System.String FullText)
        {
            StatusCode = StringToCode(CodeText);
            FullMessage = FullText;
        }
        public static FtpStatusCode StringToCode(System.String NumberString)
        {
            int result;
            bool IsNumber = int.TryParse(NumberString, out result);
            if (IsNumber && IsFtpStatusCode(result))
            {
                return (FtpStatusCode)result;
            }
            else
            {
                return FtpStatusCode.Undefined;
            }
        }
        public StatusCodeClass GetCodeClass()
        {
            return (StatusCodeClass)((int)StatusCode / 100);
        }
        public static bool IsFtpStatusCode(int Number)
        {
            return Enum.IsDefined(typeof(FtpStatusCode), Number) && (Number != (int)FtpStatusCode.Undefined);
        }

    }
}
