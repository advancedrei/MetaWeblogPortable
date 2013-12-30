using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using SXL = System.Xml.Linq;

namespace MetaWeblog.Portable.XmlRpc
{
    public class MethodResponse
    {
        public ParameterList Parameters { get; private set; }

        public MethodResponse()
        {
            this.Parameters = new ParameterList();
        }

        public MethodResponse(string content) :
            this()
        {
            SXL.XDocument xdoc;
            MethodCall.ParseStringToParameters(content, this.Parameters, out xdoc);
        }

        public SXL.XDocument CreateDocument()
        {
            var doc = new SXL.XDocument();
            var root = new SXL.XElement("methodResponse");

            doc.Add(root);

            var params_el = new SXL.XElement("params");
            root.Add(params_el);

            foreach (var p in this.Parameters)
            {
                var param_el = new SXL.XElement("param");
                params_el.Add(param_el);

                p.AddXmlElement(param_el);
            }

            return doc;
        }
    }
}