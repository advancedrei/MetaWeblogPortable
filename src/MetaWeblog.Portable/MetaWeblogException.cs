namespace MetaWeblog.Portable
{

    /// <summary>
    /// 
    /// </summary>
    public class MetaWeblogException : System.Exception
    {

        /// <summary>
        /// 
        /// </summary>
        public MetaWeblogException() { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        public MetaWeblogException(string message) : base(message) { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <param name="inner"></param>
        public MetaWeblogException(string message, System.Exception inner) : base(message, inner) { }

    }
}