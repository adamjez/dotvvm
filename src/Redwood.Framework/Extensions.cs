using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.Http;

namespace Redwood.Framework
{
    public static class Extensions
    {

        public static IEnumerable<T> TakeWhileReverse<T>(this IList<T> items, Func<T, bool> predicate)
        {
            for (int i = items.Count - 1; i >= 0; i--)
            {
                if (!predicate(items[i]))
                {
                    yield break;
                }
                yield return items[i];
            }
        }

        public static string GetPathAndQuery(this HttpRequest request)
        {
            return request.Path.ToString() + request.QueryString.ToString();
        }

        public static string GetAbsoluteUrl(this HttpRequest request)
        {
            return request.Scheme.ToString() + "://" + request.Host.ToString() + request.GetPathAndQuery();
        }

        public static T GetService<T>(this IServiceProvider serviceProvider)
        {
            return (T)serviceProvider.GetService(typeof (T));
        }
    }
}