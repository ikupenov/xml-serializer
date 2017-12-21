using System;
using System.Xml.Linq;

namespace XmlSerializer
{
    interface IXmlFilterSerializer
    {
        Predicate<object> Filter { get; set; }

        void Serialize<T>(T instance, XElement root, Type type = null, bool mapAttributes = true);
    }
}
