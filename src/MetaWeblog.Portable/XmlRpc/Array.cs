using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SXL = System.Xml.Linq;

namespace MetaWeblog.Portable.XmlRpc
{
    public class Array : Value, IEnumerable<Value>
    {


        private readonly List<Value> _items;

        public Array()
        {
            this._items = new List<Value>();
        }

        public Array(int capacity)
        {
            this._items = new List<Value>(capacity);
        }

        public void Add(Value v)
        {
            this._items.Add(v);
        }

        public void Add(int v)
        {
            this._items.Add(new IntegerValue(v));
        }

        public void Add(double v)
        {
            this._items.Add(new DoubleValue(v));
        }

        public void Add(bool v)
        {
            this._items.Add(new BooleanValue(v));
        }

        public void Add(System.DateTime v)
        {
            this._items.Add(new DateTimeValue(v));
        }

        public void AddRange(IEnumerable<Value> items)
        {
            foreach (var item in items)
            {
                this._items.Add(item);
            }
        }

        public Value this[int index]
        {
            get { return this._items[index]; }
        }

        public IEnumerator<Value> GetEnumerator()
        {
            return _items.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public static string TypeString
        {
            get { return "array"; }
        }

        public int Count
        {
            get { return this._items.Count; }
        }

        protected override void AddToTypeEl(SXL.XElement parent)
        {
            var data_el = new SXL.XElement("data");
            parent.Add(data_el);
            foreach (Value item in this)
            {
                item.AddXmlElement(data_el);
            }
        }

        internal static Array XmlToValue(SXL.XElement typeEl)
        {
            var data_el = typeEl.GetElement("data");

            var value_els = data_el.Elements("value").ToList();
            var list = new XmlRpc.Array();
            foreach (var value_el2 in value_els)
            {
                var o = Value.ParseXml(value_el2);
                list.Add(o);
            }
            return list;
        }

        public override bool Equals(System.Object obj)
        {
            if (obj == null)
            {
                return false;
            }

            var p = obj as Array;
            if (p == null)
            {
                return false;
            }

            // Return true if the fields match:
            if (this._items != p._items)
            {
                if (this._items.Count!= p._items.Count)
                {
                    return false;
                }

                return !this._items.Where((t, i) => !(t.Equals(p[i]))).Any();
            }
            return true;
        }

        protected override string GetTypeString()
        {
            return Array.TypeString;
        }

        public override int GetHashCode()
        {
            return this._items.GetHashCode();
        }
    }
}