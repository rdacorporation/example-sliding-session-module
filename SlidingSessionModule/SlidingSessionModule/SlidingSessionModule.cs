namespace RDA.SlidingSessionModule
{
    using Microsoft.IdentityModel.Web;
    using Microsoft.SharePoint;
    using System;
    using System.Web;

    public class SlidingSessionModule : IHttpModule
    {
        public void Init(HttpApplication context)
        {
            // Sliding session
            FederatedAuthentication.SessionAuthenticationModule.SessionSecurityTokenReceived += SessionAuthenticationModule_SessionSecurityTokenReceived;
            context.EndRequest += new EventHandler(OnEndRequest);
        }

        public void Dispose()
        {
        }

        private void SessionAuthenticationModule_SessionSecurityTokenReceived(object sender, SessionSecurityTokenReceivedEventArgs e)
        {
            double sessionLifetimeInMinutes =
                (e.SessionToken.ValidTo - e.SessionToken.ValidFrom).TotalMinutes;
            var logonTokenCacheExpirationWindow = TimeSpan.FromSeconds(1);
            SPSecurity.RunWithElevatedPrivileges(delegate ()
            {
                logonTokenCacheExpirationWindow =
                    Microsoft.SharePoint.Administration.Claims.SPSecurityTokenServiceManager.Local.LogonTokenCacheExpirationWindow;
            });

            DateTime now = DateTime.UtcNow;
            DateTime validTo = e.SessionToken.ValidTo - logonTokenCacheExpirationWindow;
            DateTime validFrom = e.SessionToken.ValidFrom;

            if ((now < validTo) && (now > validFrom.AddMinutes((validTo - validFrom).TotalMinutes / 2)))
            {
                SessionAuthenticationModule sam = FederatedAuthentication.SessionAuthenticationModule;
                e.SessionToken = sam.CreateSessionSecurityToken(e.SessionToken.ClaimsPrincipal, e.SessionToken.Context,
                    now, now.AddMinutes(sessionLifetimeInMinutes), e.SessionToken.IsPersistent);

                e.ReissueCookie = true;
            }
        }

        public void OnEndRequest(Object sender, EventArgs e)
        {
            var httpApp = (HttpApplication)sender;
            httpApp.Context.Response.AppendHeader("X-SlidingSessionActive", "Yep");
        }
    }
}
