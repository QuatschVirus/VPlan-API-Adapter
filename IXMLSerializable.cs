using System.Xml.Linq;

namespace VPlan_API_Adapter
{
    public interface IXMLSerializable
    {
        public XElement ToXML();
    }

    public class XMLSerializeableList<T> : List<T>, IXMLSerializable where T : IXMLSerializable
    {
        private string rootElementName;

        public XMLSerializeableList(string rootElementName)
        {
            this.rootElementName = rootElementName;
        }

        public static XMLSerializeableList<T> From(List<T> list, string rootElementName)
        {
            XMLSerializeableList<T> xList = (XMLSerializeableList<T>)list;
            xList.rootElementName = rootElementName;
            return xList;
        }

        public XElement ToXML()
        {
            XElement root = new(rootElementName);
            foreach (var element in this)
            {
                root.Add(element.ToXML());
            }
            return root;
        }
    }
}
