namespace Redwood.Samples.BasicSamples
{
    using System;
    using System.Reflection;

    internal class Sample9_Resources
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
                            resourceMan = new System.Resources.ResourceManager("Redwood.Samples.BasicSamples.Sample9_Resources", typeof(Sample9_Resources).GetTypeInfo().Assembly);
                        }
                return resourceMan;
            }
        }

        internal static System.Globalization.CultureInfo Culture { get; set; }

        internal static string LocalizedString1
        {
            get { return ResourceManager.GetString("LocalizedString1", Culture); }
        }

    }

}

