using System;

namespace MetaWeblog.Portable.XmlRpc
{
    public class XmlRpcException : Exception
    {





        public XmlRpcException() { }
        public XmlRpcException(string message) : base(message) { }
        public XmlRpcException(string message, Exception inner) : base(message, inner) { }


        public Fault Fault;
    }
}