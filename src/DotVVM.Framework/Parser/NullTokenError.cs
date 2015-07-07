using System;
using System.Collections.Generic;
using System.Linq;

namespace DotVVM.Framework.Parser
{
    public class NullTokenError<TToken, TTokenType> : TokenError<TToken, TTokenType> where TToken : TokenBase<TTokenType>, new()
    {
        public NullTokenError(TokenizerBase<TToken, TTokenType> tokenizer) : base("", tokenizer)
        {
        }

        protected override TextRange GetRange()
        {
            return null;
        }
    }
}