namespace VPlan_API_Adapter.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public class HeaderFilterAttribute(string name, string? description, bool required) : Attribute, IHeaderFilter
    {
        public string HeaderName { get; } = name;
        public string? HeaderDescription { get; } = description;
        public bool IsHeaderRequired => required;
    }
}
