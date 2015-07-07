using DotVVM.Framework.Runtime;
using DotVVM.Framework.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotVVM.Framework.Controls
{
	/// <summary>
	/// Renders <c>select</c> HTML element control.
	/// </summary>
	public abstract class SelectHtmlControlBase : Selector
	{
		
        /// <summary>
		/// Initializes a new instance of the <see cref="SelectHtmlControlBase"/> class.
        /// </summary>
		public SelectHtmlControlBase()
			: base("select")
        {
            
        }

        /// <summary>
        /// Adds all attributes that should be added to the control begin tag.
        /// </summary>
        protected override void AddAttributesToRender(IHtmlWriter writer, RenderContext context)
        {
            if (!RenderOnServer)
            {
                writer.AddKnockoutDataBind("options", this, DataSourceProperty, () => { });

                if (!string.IsNullOrEmpty(DisplayMember))
                {
                    writer.AddKnockoutDataBind("optionsText", KnockoutHelper.MakeStringLiteral(DisplayMember));
                }
                if (!string.IsNullOrEmpty(ValueMember))
                {
                    writer.AddKnockoutDataBind("optionsValue", KnockoutHelper.MakeStringLiteral(ValueMember));
                }
            }

			// changed event
			var selectionChangedBinding = GetCommandBinding(SelectionChangedProperty);
			if (selectionChangedBinding != null)
			{
				writer.AddAttribute("onchange", KnockoutHelper.GenerateClientPostBackScript(selectionChangedBinding, context, this));
			}

			// selected value
            writer.AddKnockoutDataBind("value", this, SelectedValueProperty, () => { });
            
            base.AddAttributesToRender(writer, context);
        }

        /// <summary>
        /// Renders the contents inside the control begin and end tags.
        /// </summary>
        protected override void RenderContents(IHtmlWriter writer, RenderContext context)
        {
            if (RenderOnServer)
            {
                // render items
                foreach (var item in GetIEnumerableFromDataSource(DataSource))
                {
                    var value = string.IsNullOrEmpty(ValueMember) ? item : ReflectionUtils.GetObjectProperty(item, ValueMember);
                    var text = string.IsNullOrEmpty(DisplayMember) ? item : ReflectionUtils.GetObjectProperty(item, DisplayMember);

                    writer.AddAttribute("value", value != null ? value.ToString() : "");
                    writer.RenderSelfClosingTag("option");
                    writer.WriteText(text != null ? text.ToString() : "");
                    writer.RenderEndTag();
                }
            }
        }
	}
}
