namespace Redwood.Framework.Resources
{
    using System;
    using System.Reflection;

    internal class RwHtmlTokenizerErrors
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
                            resourceMan = new System.Resources.ResourceManager("Redwood.Framework.Resources.RwHtmlTokenizerErrors", typeof(RwHtmlTokenizerErrors).GetTypeInfo().Assembly);
                        }
                return resourceMan;
            }
        }

        internal static System.Globalization.CultureInfo Culture { get; set; }

        internal static string AttributeValueNotClosed
        {
            get { return ResourceManager.GetString("AttributeValueNotClosed", Culture); }
        }

        internal static string BindingInvalidFormat
        {
            get { return ResourceManager.GetString("BindingInvalidFormat", Culture); }
        }

        internal static string BindingNotClosed
        {
            get { return ResourceManager.GetString("BindingNotClosed", Culture); }
        }

        internal static string CDataNotClosed
        {
            get { return ResourceManager.GetString("CDataNotClosed", Culture); }
        }

        internal static string CommentNotClosed
        {
            get { return ResourceManager.GetString("CommentNotClosed", Culture); }
        }

        internal static string DirectiveNameExpected
        {
            get { return ResourceManager.GetString("DirectiveNameExpected", Culture); }
        }

        internal static string DirectiveValueExpected
        {
            get { return ResourceManager.GetString("DirectiveValueExpected", Culture); }
        }

        internal static string DoctypeNotClosed
        {
            get { return ResourceManager.GetString("DoctypeNotClosed", Culture); }
        }

        internal static string DoubleBraceBindingNotClosed
        {
            get { return ResourceManager.GetString("DoubleBraceBindingNotClosed", Culture); }
        }

        internal static string InvalidCharactersInTag
        {
            get { return ResourceManager.GetString("InvalidCharactersInTag", Culture); }
        }

        internal static string MissingAttributeValue
        {
            get { return ResourceManager.GetString("MissingAttributeValue", Culture); }
        }

        internal static string MissingTagName
        {
            get { return ResourceManager.GetString("MissingTagName", Culture); }
        }

        internal static string MissingTagPrefix
        {
            get { return ResourceManager.GetString("MissingTagPrefix", Culture); }
        }

        internal static string TagNameExpected
        {
            get { return ResourceManager.GetString("TagNameExpected", Culture); }
        }

        internal static string TagNotClosed
        {
            get { return ResourceManager.GetString("TagNotClosed", Culture); }
        }

        internal static string XmlProcessingInstructionNotClosed
        {
            get { return ResourceManager.GetString("XmlProcessingInstructionNotClosed", Culture); }
        }

    }

}

