using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Athentication.Entity
{
    //ResponseModel is for Apis Response output
    public class ResponseModel
    {
        public int status { get; set; }
        public string content { get; set; }
        public string message { get; set; }
    }
}
