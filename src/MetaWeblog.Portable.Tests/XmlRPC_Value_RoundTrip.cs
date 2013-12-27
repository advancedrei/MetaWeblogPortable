using System;
using System.Xml.Linq;
using MetaWeblog.Portable.XmlRpc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using X=MetaWeblog.Portable.XmlRpc;

namespace MetaWeblogSharpTests
{
    [TestClass]
    public class XmlRPC_Value_RoundTrip
    {
        [TestMethod]
        public void TestMethod1()
        {
            var struct_ = new Struct();
        }

        [TestMethod]
        public void RoundTrip_Struct()
        {
            // In this test, we'll simply make sure that
            // an array can handle all the supported types

            var src = new X.Struct();
            src["a"] = (IntegerValue)0;
            src["b"] = (IntegerValue)18;
            src["c"] = (IntegerValue)(-18);
            src["d"] = (DoubleValue)1.0893;
            src["e"] = (DoubleValue)(-1.0893);
            src["f"] = (BooleanValue)true;
            src["g"] = (BooleanValue)false;
            src["h"] = (DateTimeValue)System.DateTime.Now;
            src["i"] = new Base64Data(new byte[] { 0, 1, 2, 3 });
            src["j"] = new Struct();
            src["k"] = new X.Array();

            var dest = RoundTrip(src);

            Assert.AreEqual(src.Count,dest.Count);
            foreach (var src_pair in src)
            {
                Assert.IsTrue(dest.ContainsKey(src_pair.Key));
                Assert.AreEqual(src[src_pair.Key],dest[src_pair.Key]);
            }
        }
        [TestMethod]
        public void RoundTrip_Array()
        {
            // In this test, we'll simply make sure that
            // an array can handle all the supported types

            var src = new X.Array();
            src.Add(0);
            src.Add(18);
            src.Add(-18);
            src.Add(1.0893);
            src.Add(-1.0893);
            src.Add(true);
            src.Add(false);
            src.Add(System.DateTime.Now);
            src.Add(new Base64Data(new byte[ ] {0,1,2,3}));
            src.Add(new Struct());
            src.Add(new X.Array());

            var dest = RoundTrip(src);

            for (int i = 0; i < src.Count; i++)
            {
                var s = src[i];
                var d = dest[i];

                Assert.AreEqual(src.GetType(),dest.GetType());
                Assert.AreEqual(src,dest);
            }
        }

        [TestMethod]
        public void RoundTrip_Base64()
        {
            var src = new Base64Data(new byte[] {1, 2, 3, 4, 5});
            var dest = RoundTrip(src);

            for (int i = 0; i < src.Bytes.Length; i++)
            {
                Assert.AreEqual(src.Bytes[i],dest.Bytes[i]);
            }
        }

        [TestMethod]
        public void RoundTrip_Int()
        {
            var src = new IntegerValue(7);
            var dest = RoundTrip(src);
            Assert.AreEqual(src.Integer,dest.Integer);
        }

        [TestMethod]
        public void RoundTrip_Double()
        {
            var src = new DoubleValue(6.02);
            var dest = RoundTrip(src);
            Assert.AreEqual(src.Double, dest.Double);
        }

        [TestMethod]
        public void RoundTrip_Bool_true()
        {
            var src = new BooleanValue(true);
            var dest = RoundTrip(src);
            Assert.AreEqual(src.Boolean, dest.Boolean);
        }

        [TestMethod]
        public void RoundTrip_Bool_false()
        {
            var src = new BooleanValue(false);
            var dest = RoundTrip(src);
            Assert.AreEqual(src.Boolean, dest.Boolean);
        }

        public T RoundTrip<T>(T sourceValue) where T :Value
        {
            // Create Source Document
            var src_parent = new XElement("X");
            var src_value_el = sourceValue.AddXmlElement(src_parent);

            var desr_value_o = Value.ParseXml(src_value_el);
            var dest_value = (T)desr_value_o;
            return dest_value;
        }
    }
}
