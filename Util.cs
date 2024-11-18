using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
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

        public static IActionResult CreateXMLResult(IXMLSerializable serializable)
        {
            XDocument doc = new();
            doc.Add(serializable.ToXML());

            return new ContentResult()
            {
                StatusCode = 200,
                ContentType = "application/xml",
                Content = doc.ToString()
            };
        }

        public static bool ShouldReturnXML(this HttpRequest request)
        {
            return request.Headers.Accept.Contains("applicaation/xml") || (request.Headers.TryGetValue(ReturnTypeParameter.ReturnTypeHeaderName, out var strings) && strings.Contains("application/xml"));
        }

        public static IActionResult ProduceResult<T>(this HttpRequest request, T data, string listRootName = "ListRoot", string defaultContentType = "application/json", List<string>? allowedContentTypes = null)
        {
            allowedContentTypes ??= [];
            var mb = new StackFrame(1).GetMethod();
            if (mb != null)
            {
                var pAttr = mb.GetCustomAttribute<ProducesAttribute>();
                if (pAttr != null)
                {
                    allowedContentTypes.AddRange(pAttr.ContentTypes);
                }
            }
            if (allowedContentTypes.Count != 0) allowedContentTypes.Add(defaultContentType);

            string contentType = request.Headers.TryGetValue(ReturnTypeParameter.ReturnTypeHeaderName, out var strings) ? strings.FirstOrDefault(defaultContentType)! : defaultContentType;
            if (allowedContentTypes.Contains(contentType))
            {
                switch (contentType)
                {
                    case "application/json": return new JsonResult(data);
                    case "application/xml":
                        {
                            if (data is List<IXMLSerializable> list)
                            {
                                return CreateXMLResult(XMLSerializeableList<IXMLSerializable>.From(list, listRootName));
                            } else if (data is IXMLSerializable serializable)
                            {
                                return CreateXMLResult(serializable);
                            }

                            return new ContentResult()
                            {
                                StatusCode = 500,
                                Content = "[XML_FORMAT_FAILURE]\nUnable to format to XML. Please create an issue on Github if you see this message.\n" +
                                "Github-Link: https://github.com/QuatschVirus/VPlan-API-Adapter\n" +
                                "Error report (copy this into the issue):\n" +
                                "Error: Data should be XML serializable, but does not implement IXMLSerializable\n" + 
                                $"Endpoint: {request.Path}\n" +
                                $"Type: {data?.GetType().FullName}\n" +
                                $"Return-Type-Header: {string.Join(", ", [.. strings])}"
                            };
                        }
                    default:
                        {
                            return new ContentResult()
                            {
                                StatusCode = 501,
                                Content = "[FORMAT_NOT_IMPLEMENTED]\nUnable serialize result. Please create an issue on Github if you see this message.\n" +
                                "Github-Link: https://github.com/QuatschVirus/VPlan-API-Adapter\n" +
                                "Error report (copy this into the issue):\n" +
                                "Error: The type has been entered as allowed, but can not be serialized \n" +
                                $"Endpoint: {request.Path}\n" +
                                $"Type: {data?.GetType().FullName}\n" +
                                $"Content Type: {contentType}\n" +
                                $"Return-Type-Header: {string.Join(", ", [.. strings])}\n" + 
                                $"Allowed Types: {string.Join(", ", allowedContentTypes)}"
                            };
                        }
                }
            } else
            {
                return new ContentResult()
                {
                    StatusCode = 406,
                    Content = $"[TYPE_NOT_AVAILABLE]\nThe requested content type ({contentType}) is not available\nAvailable: {string.Join(", ", allowedContentTypes)}"
                };
            }
        }
    }
}
