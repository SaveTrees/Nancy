using System.Collections.Generic;
using Nancy.Diagnostics;

namespace Nancy.ViewEngines
{
    using System;

    /// <summary>
    /// Defines the functionality of a Nancy view cache.
    /// </summary>
    public interface IViewCache : IEnumerable<KeyValuePair<ViewLocationResult, object>>
    {
        /// <summary>
        /// Gets or adds a view from the cache.
        /// </summary>
        /// <param name="viewLocationResult">A <see cref="ViewLocationResult"/> instance that describes the view that is being added or retrieved from the cache.</param>
        /// <param name="valueFactory">A function that produces the value that should be added to the cache in case it does not already exist.</param>
        /// <returns>An instance of the view type.</returns>
        Func<INancyView> GetOrAdd(ViewLocationResult viewLocationResult, Func<ViewLocationResult, Func<INancyView>> valueFactory);

        ///       <summary>
        ///
        ///       </summary>
        /// <param name="viewLocator"></param>
        /// <param name="viewEngines"></param>
        /// <param name="rootPathProvider"></param>
        /// <param name="traceLog"></param>
        /// <returns></returns>
        IEnumerable<ViewTemplateCompilationResult<IViewTemplate>> CacheAllViewTemplates(IViewLocator viewLocator, IEnumerable<IViewEngine> viewEngines, IRootPathProvider rootPathProvider, ITraceLog traceLog);
    }
}