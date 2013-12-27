using System.Xml.Linq;

namespace MetaWeblog.Portable
{
    public static class XmlExtensions
    {

        /// <summary>
        /// 
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static string GetElementString(this XElement parent, string name)
        {
            var childElement = parent.GetElement(name);
            return childElement.Value;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static XElement GetElement(this XElement parent, string name)
        {
            var childElement = parent.Element(name);         
            if (childElement != null) return childElement;

            var msg = string.Format("Xml Error: <{0}/> element does not contain <{1}/> element",
                parent.Name, name);
            throw new MetaWeblogException(msg);
        }
    }
}