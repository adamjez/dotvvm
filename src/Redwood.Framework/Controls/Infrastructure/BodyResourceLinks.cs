﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using Redwood.Framework.Configuration;
using Redwood.Framework.Hosting;
using Redwood.Framework.Runtime;
using Redwood.Framework.ResourceManagement;

namespace Redwood.Framework.Controls.Infrastructure
{
    /// <summary>
    /// Renders the script elements and the serialized viewmodel. This control must be on every page just before the end of body element.
    /// </summary>
    public class BodyResourceLinks : RedwoodControl
    {

        /// <summary>
        /// Renders the control into the specified writer.
        /// </summary>
        protected override void RenderControl(IHtmlWriter writer, RenderContext context)
        {
            // render resource links
            var resources = context.ResourceManager.GetResourcesInCorrectOrder().Where(r => r.GetRenderPosition() == ResourceRenderPosition.Body);
            foreach (var resource in resources)
            {
                resource.Render(writer);
            }

            // render the serialized viewmodel
            var serializedViewModel = context.RequestContext.GetSerializedViewModel();
            writer.AddAttribute("type", "hidden");
            writer.AddAttribute("id", "__rw_viewmodel_" + context.CurrentPageArea);
            writer.AddAttribute("value", serializedViewModel);
            writer.RenderSelfClosingTag("input");

            // init on load
            writer.RenderBeginTag("script");
            writer.WriteUnencodedText(string.Format("redwood.onDocumentReady(function () {{ redwood.init('{0}', '{1}'); }});", context.CurrentPageArea, CultureInfo.CurrentUICulture.Name));
            writer.RenderEndTag();
        }
    }
}
