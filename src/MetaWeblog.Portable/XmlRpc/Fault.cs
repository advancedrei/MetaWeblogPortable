namespace MetaWeblog.Portable.XmlRpc
{
    public class Fault
    {
        public int FaultCode { get; set; }
        public string FaultString { get; set; }
        public string RawData { get; set; }

        public static Fault ParseXml(System.Xml.Linq.XElement fault_el)
        {
            var value_el = fault_el.GetElement("value");
            var fault_value = (Struct)XmlRpc.Value.ParseXml(value_el);

            int fault_code = -1;
            var fault_code_val = fault_value.Get("faultCode");
            if (fault_code_val != null)
            {
                if (fault_code_val is StringValue)
                {
                    var s = (StringValue)fault_code_val;
                    fault_code = int.Parse(s.String);
                }
                else if (fault_code_val is IntegerValue)
                {
                    var i = (IntegerValue)fault_code_val;
                    fault_code = i.Integer;
                }
                else
                {
                    string msg = string.Format("Fault Code value is not int or string {0}", value_el.ToString());
                    throw new MetaWeblogException(msg);
                }
            }

            string fault_string = fault_value.Get<StringValue>("faultString").String;

            var f = new Fault();
            f.FaultCode = fault_code;
            f.FaultString = fault_string;
            f.RawData = fault_el.Document.ToString();
            return f;
        }

        public System.Xml.Linq.XDocument CreateDocument()
        {
            var doc = new System.Xml.Linq.XDocument();
            var root = new System.Xml.Linq.XElement("methodResponse");

            doc.Add(root);

            var f = new System.Xml.Linq.XElement("fault");

            root.Add(f);


            var struct_ = new XmlRpc.Struct();
            struct_["faultCode"] = new XmlRpc.IntegerValue(this.FaultCode);
            struct_["faultString"] = new XmlRpc.StringValue(this.FaultString);
            struct_.AddXmlElement(f);

            return doc;
        }
    }
}