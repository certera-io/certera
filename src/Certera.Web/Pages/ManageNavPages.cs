using System;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Certera.Web.Pages
{
    public static class ManageNavPages
    {
        public static string Tracking => "Tracking";

        public static string Acme => "Acme";

        public static string Keys => "Keys";

        public static string Certificates => "Certificates";

        public static string Notifications => "Notifications";

        public static string Settings => "Settings";

        public static string TrackingNavClass(ViewContext viewContext) => PageNavClass(viewContext, Tracking);

        public static string AcmeNavClass(ViewContext viewContext) => PageNavClass(viewContext, Acme);

        public static string KeysNavClass(ViewContext viewContext) => PageNavClass(viewContext, Keys);

        public static string CertificatesNavClass(ViewContext viewContext) => PageNavClass(viewContext, Certificates);

        public static string NotificationsNavClass(ViewContext viewContext) => PageNavClass(viewContext, Notifications);

        public static string SettingsNavClass(ViewContext viewContext) => PageNavClass(viewContext, Settings);

        private static string PageNavClass(ViewContext viewContext, string page)
        {
            var activePage = viewContext.ViewData["ActivePage"] as string
                ?? System.IO.Path.GetFileNameWithoutExtension(viewContext.ActionDescriptor.DisplayName);
            return string.Equals(activePage, page, StringComparison.OrdinalIgnoreCase) ? "is-active" : null;
        }
    }
}