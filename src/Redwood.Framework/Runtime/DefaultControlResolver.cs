using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Framework.Runtime;
using Redwood.Framework.Binding;
using Redwood.Framework.Configuration;
using Redwood.Framework.Controls;
using Redwood.Framework.Hosting;
using Redwood.Framework.Parser;

namespace Redwood.Framework.Runtime
{
    /// <summary>
    /// Default Redwood control resolver.
    /// </summary>
    public class DefaultControlResolver : IControlResolver
    {

        private readonly RedwoodConfiguration configuration;
        private readonly IControlBuilderFactory controlBuilderFactory;

        private static ConcurrentDictionary<string, ControlType> cachedTagMappings = new ConcurrentDictionary<string, ControlType>();
        private static ConcurrentDictionary<Type, ControlResolverMetadata> cachedMetadata = new ConcurrentDictionary<Type, ControlResolverMetadata>();

        private static object locker = new object();
        private static bool isInitialized = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultControlResolver"/> class.
        /// </summary>
        public DefaultControlResolver(RedwoodConfiguration configuration)
        {
            this.configuration = configuration;
            this.controlBuilderFactory = configuration.ServiceProvider.GetService<IControlBuilderFactory>();

            if (!isInitialized)
            {
                lock (locker)
                {
                    if (!isInitialized)
                    {
                        InvokeStaticConstructorsOnAllControls();
                        isInitialized = true;
                    }
                }
            }
        }

        /// <summary>
        /// Invokes the static constructors on all controls to register all <see cref="RedwoodProperty"/>.
        /// </summary>
        private void InvokeStaticConstructorsOnAllControls()
        {
            foreach (var assembly in configuration.AssemblyHelper.GetAllAssemblies())
            {
                try
                {
                    foreach (var type in assembly.GetTypes().Where(t => t.GetTypeInfo().IsClass))
                    {
                        if (type.GetTypeInfo().GetCustomAttribute<ContainsRedwoodPropertiesAttribute>(true) != null)
                        {
                            RuntimeHelpers.RunClassConstructor(type.TypeHandle);
                        }
                    }
                }
                catch (Exception ex)
                {
                }
            }
        }


        /// <summary>
        /// Resolves the metadata for specified element.
        /// </summary>
        public ControlResolverMetadata ResolveControl(string tagPrefix, string tagName, out object[] activationParameters)
        {
            // html element has no prefix
            if (string.IsNullOrEmpty(tagPrefix))
            {
                activationParameters = new object[] { tagName };
                return ResolveControl(typeof (HtmlGenericControl));
            }

            // find cached value
            var searchKey = GetSearchKey(tagPrefix, tagName);
            activationParameters = null;
            var controlType = cachedTagMappings.GetOrAdd(searchKey, _ => FindControlMetadata(tagPrefix, tagName));
            var metadata = ResolveControl(controlType);
            return metadata;
        }

        private static string GetSearchKey(string tagPrefix, string tagName)
        {
            return tagPrefix + ":" + tagName;
        }

        /// <summary>
        /// Resolves the control metadata for specified type.
        /// </summary>
        public ControlResolverMetadata ResolveControl(ControlType controlType)
        {
            return cachedMetadata.GetOrAdd(controlType.Type, _ => BuildControlMetadata(controlType));
        }

        public ControlResolverMetadata ResolveControl(Type controlType)
        {
            return ResolveControl(new ControlType(controlType));
        }

        /// <summary>
        /// Resolves the binding type.
        /// </summary>
        public Type ResolveBinding(string bindingType)
        {
            if (bindingType == Constants.ValueBinding)
            {
                return typeof (ValueBindingExpression);
            }
            else if (bindingType == Constants.CommandBinding)
            {
                return typeof (CommandBindingExpression);
            }
            else if (bindingType == Constants.ControlStateBinding)
            {
                return typeof (ControlStateBindingExpression);
            }
            else if (bindingType == Constants.ControlPropertyBinding)
            {
                return typeof (ControlPropertyBindingExpression);
            }
            else if (bindingType == Constants.ControlCommandBinding)
            {
                return typeof(ControlCommandBindingExpression);
            }
            else if (bindingType == Constants.ResourceBinding)
            {
                return typeof (ResourceBindingExpression);
            }
            else
            {
                throw new NotSupportedException(string.Format("The binding {{{0}: ... }} is unknown!", bindingType));   // TODO: exception handling
            }
        }

        /// <summary>
        /// Finds the control metadata.
        /// </summary>
        private ControlType FindControlMetadata(string tagPrefix, string tagName)
        {
            // try to match the tag prefix and tag name
            var rules = configuration.Markup.Controls.Where(r => r.IsMatch(tagPrefix, tagName));
            foreach (var rule in rules)
            {
                // validate the rule
                rule.Validate();

                if (string.IsNullOrEmpty(rule.TagName))
                {
                    // find the code only control
                    var compiledControl = FindCompiledControl(tagName, rule.Namespace, rule.Assembly);
                    if (compiledControl != null)
                    {
                        return compiledControl;
                    }
                }
                else
                {
                    // find the markup control
                    return FindMarkupControl(rule.Src);
                }
            }

            throw new Exception(string.Format(Resources.Controls.ControlResolver_ControlNotFound, tagPrefix, tagName));
        }

        /// <summary>
        /// Finds the compiled control.
        /// </summary>
        private ControlType FindCompiledControl(string tagName, string namespaceName, string assemblyName)
        {
            var type = Type.GetType(namespaceName + "." + tagName + ", " + assemblyName, false);
            if (type == null)
            {
                // the control was not found
                return null;
            }

            return new ControlType(type);
        }

        /// <summary>
        /// Finds the markup control.
        /// </summary>
        private ControlType FindMarkupControl(string file)
        {
            var controlBuilder = controlBuilderFactory.GetControlBuilder(file);
            return new ControlType(controlBuilder.BuildControl(controlBuilderFactory).GetType(), controlBuilder.GetType(), file);
        }

        /// <summary>
        /// Gets the control metadata.
        /// </summary>
        private ControlResolverMetadata BuildControlMetadata(ControlType type)
        {
            var attribute = type.Type.GetTypeInfo().GetCustomAttribute<ControlMarkupOptionsAttribute>();

            var metadata = new ControlResolverMetadata()
            {
                Name = type.Type.Name,
                Namespace = type.Type.Namespace,
                HasHtmlAttributesCollection = typeof(IControlWithHtmlAttributes).IsAssignableFrom(type.Type),
                Type = type.Type,
                ControlBuilderType = type.ControlBuilderType,
                Properties = GetControlProperties(type.Type),
                IsContentAllowed = attribute.AllowContent,
                VirtualPath = type.VirtualPath
            };
            return metadata;
        }

        /// <summary>
        /// Gets the control properties.
        /// </summary>
        private Dictionary<string, RedwoodProperty> GetControlProperties(Type controlType)
        {
            return RedwoodProperty.ResolveProperties(controlType).ToDictionary(p => p.Name, p => p);
        }
    }
}