using System;
using System.Collections.Generic;
using System.Linq;
using DotVVM.Framework.Binding;

namespace DotVVM.Framework.Controls
{
    [ContainsDotvvmProperties]
    public class Validate
    {

        [AttachedProperty(typeof(bool))]
        public static DotvvmProperty EnabledProperty = DotvvmProperty.Register<bool, Validate>("Enabled", true);

        [AttachedProperty(typeof(object))]
        public static DotvvmProperty TargetProperty = DotvvmProperty.Register<object, Validate>("Target", new ValueBindingExpression("_root"));

    }
}
