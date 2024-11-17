using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;

namespace VPlan_API_Adapter
{
    public static class Util
    {
        public static byte[] Hash(this XElement root)
        {
            return SHA256.HashData(Encoding.UTF8.GetBytes(GetAllXMLHashables(root)));
        }

        public static string GetAllXMLHashables(XElement root)
        {
            string hashable = GetXMLHashable(root);
            foreach (XElement element in root.Elements())
            {
                hashable += "|" + GetAllXMLHashables(element);
            }
            return hashable;
        }

        public static string GetXMLHashable(XElement e)
        {
            return e.Name.LocalName + ";" + string.Join(',', e.Attributes().Select(a => a.Name.LocalName + "=" + a.Value)) + ";" + e.Value;
        }
    }
}
