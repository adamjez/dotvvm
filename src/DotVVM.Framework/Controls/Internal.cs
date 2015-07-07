using System;
using System.Collections.Generic;
using System.Linq;
using DotVVM.Framework.Binding;
using DotVVM.Framework.Hosting;

namespace DotVVM.Framework.Controls
{
    /// <summary>
    /// Contains properties that are intended for internal use.
    /// </summary>
    [ContainsDotvvmProperties]
    public class Internal
    {
        
        public static readonly DotvvmProperty UniqueIDProperty =
            DotvvmProperty.Register<string, Internal>("UniqueID", isValueInherited: false);

        public static readonly DotvvmProperty IsNamingContainerProperty =
            DotvvmProperty.Register<bool, Internal>("IsNamingContainer", defaultValue: false, isValueInherited: false);

        public static readonly DotvvmProperty IsControlBindingTargetProperty =
            DotvvmProperty.Register<bool, Internal>("IsControlBindingTarget", defaultValue: false, isValueInherited: false);

        public static readonly DotvvmProperty RequestContextProperty =
            DotvvmProperty.Register<DotvvmRequestContext, Internal>("RequestContextProperty", false, isValueInherited: true);
    }
}
