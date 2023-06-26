using System;
using System.Collections.Generic;
using System.Text;

namespace QRMenu.DAL
{
    public class ResponseStatus
    {
        public int value { get; set; } //Error Found: 0=no error(success), >0 =error found(fail)
        public string code { get; set; }
        public string message { get; set; }
    }
}
