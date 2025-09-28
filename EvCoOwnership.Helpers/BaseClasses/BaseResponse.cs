using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EvCoOwnership.Helpers.BaseClasses
{
    public class BaseResponse
    {
        public int StatusCode { get; set; } 
        public string Message { get; set; } 
        public object Data { get; set; } = default!;
        public object AdditionalData { get; set; } = default!;
    }
}
