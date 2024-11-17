using System.Xml.Linq;

namespace VPlan_API_Adapter
{
    public interface IXMLSerializable
    {
        public XElement ToXML();
    }
}
