namespace DotVVM.Framework.Resources
{
    using System;
    using System.Reflection;

    internal class Controls
    {

        private static System.Resources.ResourceManager resourceMan;
        private static System.Object locker = new System.Object();
        internal static System.Resources.ResourceManager ResourceManager
        {
            get
            {
                if (resourceMan == null)
                    lock (locker)
                        if (resourceMan == null)
                        {
                            resourceMan = new System.Resources.ResourceManager("DotVVM.Framework.Resources.Controls", typeof(Controls).GetTypeInfo().Assembly);
                        }
                return resourceMan;
            }
        }

        internal static System.Globalization.CultureInfo Culture { get; set; }

        internal static string ControlResolver_ControlNotFound
        {
            get { return ResourceManager.GetString("ControlResolver_ControlNotFound", Culture); }
        }

        internal static string ViewCompiler_TypeSpecifiedInBaseTypeDirectiveNotFound
        {
            get { return ResourceManager.GetString("ViewCompiler_TypeSpecifiedInBaseTypeDirectiveNotFound", Culture); }
        }

        internal static string ViewCompiler_MarkupControlMustDeriveFromDotvvmMarkupControl
        {
            get { return ResourceManager.GetString("ViewCompiler_MarkupControlMustDeriveFromDotvvmMarkupControl", Culture); }
        }

    }

}

