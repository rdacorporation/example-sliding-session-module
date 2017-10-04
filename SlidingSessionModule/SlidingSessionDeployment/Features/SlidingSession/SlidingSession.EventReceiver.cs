using System;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using Microsoft.SharePoint;
using Microsoft.SharePoint.Administration;
using System.Collections.Generic;

namespace SlidingSessionDeployment.Features.SlidingSession
{
    /// <summary>
    /// This class handles events raised during feature activation, deactivation, installation, uninstallation, and upgrade.
    /// </summary>
    /// <remarks>
    /// The GUID attached to this class may be used during packaging and should not be modified.
    /// </remarks>
    [Guid("a6f0eab4-da0c-4118-adb3-8bcd1e8ec81e")]
    public class SlidingSessionEventReceiver : SPFeatureReceiver
    {
        public override void FeatureActivated(SPFeatureReceiverProperties properties)
        {
            var currentWebApplication = SPContext.Current.Site.WebApplication;

            SPWebConfigModification httpModule = new SPWebConfigModification
            {
                Owner = "RDA.SlidingSessionModule",
                Name = "add[@name='SlidingSessionHttpModule']",
                Type = SPWebConfigModification.SPWebConfigModificationType.EnsureChildNode,
                Path = "configuration/system.webServer/modules",
                Sequence = 0,
                Value = @"<add name=""SlidingSessionHttpModule"" type=""RDA.SlidingSessionModule.SlidingSessionModule, RDA.SlidingSessionModule, Version=1.0.0.0, Culture=neutral, PublicKeyToken=b61de93f440f208f"" />"
            };

            currentWebApplication.WebConfigModifications.Add(httpModule);
            currentWebApplication.Update();
            currentWebApplication.WebService.ApplyWebConfigModifications();
        }

        public override void FeatureDeactivating(SPFeatureReceiverProperties properties)
        {
            using (SPSite site = SPContext.Current.Site)
            {
                site.WebApplication.FileNotFoundPage = "";
                site.WebApplication.Update(true);
            }

            if (properties.Feature.Parent is SPWebApplication webApp)
            {
                var mods = new List<SPWebConfigModification>();

                foreach (var mod in webApp.WebConfigModifications)
                {
                    if (mod.Owner == "RDA.SlidingSessionModule")
                    {
                        mods.Add(mod);
                    }
                }

                foreach (var mod in mods)
                {
                    webApp.WebConfigModifications.Remove(mod);
                }

                webApp.Update();
                webApp.WebService.ApplyWebConfigModifications();
            }
        }
    }
}
