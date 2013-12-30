using System.ComponentModel;
using SXL=System.Xml.Linq;
using System.Linq;

namespace MetaWeblog.Portable.XmlRpc
{
    public class MethodCall
    {
        public ParameterList Parameters { get; private set; }

        public string Name { get; private set; }

        private MethodCall()
        {
            this.Name = null;
            this.Parameters = new ParameterList();
        }

        public MethodCall(string name)
        {
            this.Name = name;
            this.Parameters = new ParameterList();
        }

        public SXL.XDocument CreateDocument()
        {
            var doc = new SXL.XDocument();
            var root = new SXL.XElement("methodCall");

            doc.Add(root);

            var method = new SXL.XElement("methodName");
            root.Add(method);

            method.Add(this.Name);

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

        public static MethodCall Parse(string content)
        {
            SXL.XDocument xdoc;
            var mr = new MethodCall();
            ParseStringToParameters(content, mr.Parameters, out xdoc);
            var el_methodname = xdoc.Root.Element("methodName");
            if (el_methodname == null)
            {
                string msg = string.Format("Did not receive a methodName element");
                var exc = new XmlRpcException(msg);
                throw exc;
            }

            var methodname = el_methodname.Value;

            mr.Name = methodname;
            return mr;
        }

        internal static void ParseStringToParameters(string content, ParameterList parameterlist, out SXL.XDocument xdoc)
         {
            var lo = new System.Xml.Linq.LoadOptions();

            xdoc = System.Xml.Linq.XDocument.Parse(content,lo);
            var root = xdoc.Root;
            var fault_el = root.Element("fault");
            if (fault_el != null)
            {
                var f = Fault.ParseXml(fault_el);

                string msg = string.Format("XMLRPC FAULT [{0}]: \"{1}\"", f.FaultCode, f.FaultString);
                var exc = new XmlRpcException(msg);
                exc.Fault = f;

                throw exc;
            }

            var params_el = root.GetElement("params");
            var param_els = params_el.Elements("param").ToList();

            foreach (var param_el in param_els)
            {
                var value_el = param_el.GetElement("value");

                var val = XmlRpc.Value.ParseXml(value_el);
                parameterlist.Add(val);
            }
        }
    }
}