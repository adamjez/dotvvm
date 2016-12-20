﻿using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace DotVVM.Framework.Compilation.Javascript.Ast
{
    public class JsLiteral: JsExpression
    {
		private object value;

		public object Value
		{
			get { return value; }
			set { ThrowIfFrozen(); this.value = value; }
		}

        /// <summary>
		/// Javascript (JSON) representation of the object.
		/// </summary>
        public string LiteralValue
		{
			get => JsonConvert.SerializeObject(Value);
			set => Value = JsonConvert.DeserializeObject(value);
		}

		public JsLiteral() { }
		public JsLiteral(object value)
		{
			this.Value = value;
		}

        public override void AcceptVisitor(IJsNodeVisitor visitor) => visitor.VisitLiteral(this);
    }
}
