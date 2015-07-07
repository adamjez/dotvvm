namespace DotVVM.Framework.Resources
{
    using System;
    using System.Reflection;

    internal class DothtmlParserErrors
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
                            resourceMan = new System.Resources.ResourceManager("DotVVM.Framework.Resources.DothtmlParserErrors", typeof(DothtmlParserErrors).GetTypeInfo().Assembly);
                        }
                return resourceMan;
            }
        }

        internal static System.Globalization.CultureInfo Culture { get; set; }

        internal static string ClosingTagHasNoMatchingOpenTag
        {
            get { return ResourceManager.GetString("ClosingTagHasNoMatchingOpenTag", Culture); }
        }

        internal static string UnexpectedEndOfInputTagNotClosed
        {
            get { return ResourceManager.GetString("UnexpectedEndOfInputTagNotClosed", Culture); }
        }

    }

}

