using System;
using System.Collections;
using System.Linq;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace XmlSerializer
{
    public class XmlFilterSerializer : IXmlFilterSerializer
    {
        public Predicate<object> Filter { get; set; }

        public void Serialize<T>(T instance, XElement root, Type type = null, bool mapAttributes = true)
        {
            if (type == null)
            {
                type = typeof(T);
            }

            foreach (var property in type.GetProperties())
            {
                foreach (var attribute in property.GetCustomAttributes(false))
                {
                    if (mapAttributes && attribute is XmlAttributeAttribute)
                    {
                        var xmlAttribute = (XmlAttributeAttribute)attribute;

                        var attributeName = xmlAttribute.AttributeName;
                        if (string.IsNullOrEmpty(attributeName))
                        {
                            attributeName = property.Name;
                        }

                        var propertyValue = property.GetValue(instance, null);
                        if (propertyValue != null && (!IsFiltered(propertyValue)))
                        {
                            root.Add(new XAttribute(attributeName, propertyValue));
                        }
                    }
                    else if (attribute is XmlElementAttribute)
                    {
                        var xmlAttribute = (XmlElementAttribute)attribute;

                        var elementName = xmlAttribute.ElementName;
                        var propertyValue = property.GetValue(instance, null);

                        BindElement(elementName, propertyValue, root);
                    }
                    else if (attribute is XmlArrayItemAttribute)
                    {
                        var xmlAttributeArrayItem = (XmlArrayItemAttribute)attribute;

                        var xmlAttributeArrayObj = property.GetCustomAttributes(false)
                            .FirstOrDefault(a => a is XmlArrayAttribute);

                        var arrayName = (xmlAttributeArrayObj as XmlArrayAttribute)?.ElementName ?? property.Name;
                        var itemName = xmlAttributeArrayItem.ElementName;

                        var propertyValue = property.GetValue(instance, null);
                        if (propertyValue is IEnumerable)
                        {
                            BindArray(arrayName, itemName, (IEnumerable)propertyValue, root);
                        }
                    }
                    else if (attribute is XmlArrayAttribute)
                    {
                        var xmlAttributeArray = (XmlArrayAttribute)attribute;

                        var xmlAttributeArrayItemObj = property.GetCustomAttributes(false)
                            .FirstOrDefault(a => a is XmlArrayItemAttribute);

                        if (xmlAttributeArrayItemObj == null)
                        {
                            continue;
                        }

                        var xmlAttributeArrayItem = (XmlArrayItemAttribute)xmlAttributeArrayItemObj;

                        var arrayName = xmlAttributeArray.ElementName;
                        var itemName = xmlAttributeArrayItem.ElementName;

                        var propertyValue = property.GetValue(instance, null);
                        if (propertyValue is IEnumerable)
                        {
                            BindArray(arrayName, itemName, (IEnumerable)propertyValue, root);
                        }
                    }
                    else if (attribute is XmlTextAttribute)
                    {
                        var propertyValue = property.GetValue(instance, null).ToString();
                        root.SetValue(propertyValue);
                    }
                }
            }
        }

        private void BindElement(string elementName, object elementValue, XElement parentElement)
        {
            if (elementValue == null)
            {
                return;
            }

            var xmlns = parentElement.Name.Namespace;

            if (elementValue is IEnumerable)
            {
                var collection = (IEnumerable)elementValue;
                foreach (var item in collection)
                {
                    var xmlElement = new XElement(xmlns + elementName);

                    if (!IsFiltered(item))
                    {
                        parentElement.Add(xmlElement);
                    }

                    Serialize(item, xmlElement, item.GetType());
                }
            }
            else
            {
                var xmlElement = new XElement(xmlns + elementName);

                if (!IsFiltered(elementValue))
                {
                    parentElement.Add(xmlElement);
                }

                Serialize(elementValue, xmlElement, elementValue.GetType());
            }
        }

        private void BindArray(string arrayName, string itemName, IEnumerable collection, XElement parentElement)
        {
            var xmlns = parentElement.Name.Namespace;

            var arrayElement = new XElement(xmlns + arrayName);

            var isEmpty = true;

            foreach (var item in collection)
            {
                if (IsFiltered(item))
                {
                    continue;
                }

                if (isEmpty)
                {
                    parentElement.Add(arrayElement);
                    isEmpty = false;
                }

                var xmlElement = new XElement(xmlns + itemName);
                arrayElement.Add(xmlElement);

                Serialize(item, xmlElement, item.GetType());
            }
        }

        private bool IsFiltered(object obj, bool defaultValue = false)
        {
            return Filter?.Invoke(obj) ?? defaultValue;
        }
    }
}
