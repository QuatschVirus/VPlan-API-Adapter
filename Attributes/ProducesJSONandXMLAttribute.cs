using Microsoft.AspNetCore.Mvc;

namespace VPlan_API_Adapter.Attributes
{
    public class ProducesJSONandXMLAttribute : ProducesAttribute
    {
        public ProducesJSONandXMLAttribute(Type type) : base(type)
        {
        }

        public ProducesJSONandXMLAttribute(string contentType, params string[] additionalContentTypes) : base(contentType, additionalContentTypes)
        {
        }

        public ProducesJSONandXMLAttribute() : base ("application/json", "application/xml")
        {

        }
    }
}
