namespace MetaWeblog.Server
{
    public static class SXLExtensions
    {
        public static System.Xml.Linq.XElement AddDivElement(this System.Xml.Linq.XElement parent)
        {
            var el_div = new System.Xml.Linq.XElement("div");
            parent.Add(el_div);
            return el_div;
        }

        public static System.Xml.Linq.XElement AddH1Element(this System.Xml.Linq.XElement parent, string text)
        {
            var el_h1 = new System.Xml.Linq.XElement("h1", text);
            parent.Add(el_h1);
            return el_h1;
        }

        public static System.Xml.Linq.XElement AddAnchorElement(this System.Xml.Linq.XElement parent, string href, string text)
        {
            var el_anchor = new System.Xml.Linq.XElement("a");
            el_anchor.SetAttributeValue("href", href);
            el_anchor.Value = text;
            parent.Add(el_anchor);
            return el_anchor;
        }

        public static System.Xml.Linq.XElement AddParagraphElement(this System.Xml.Linq.XElement el_body)
        {
            var el_para = new System.Xml.Linq.XElement("p");
            el_body.Add(el_para);
            return el_para;
        }

        public static System.Xml.Linq.XElement AddParagraphElement(this System.Xml.Linq.XElement el_body, string text)
        {
            var el_para = new System.Xml.Linq.XElement("p");
            el_body.Add(el_para);
            el_para.Value = text;
            return el_para;
        }

        public static System.Xml.Linq.XElement ElementSafe(this System.Xml.Linq.XElement parent, System.Xml.Linq.XName name)
        {
            var el = parent.Element(name);
            if (el == null)
            {
                string msg = string.Format("Could not find element named {0}", name);
                throw new System.ArgumentException(msg);
            }
            return el;
        }
       
    }
}