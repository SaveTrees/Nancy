using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Nancy.Diagnostics;

namespace Nancy.ViewEngines
{
    using System;
    using System.Collections.Concurrent;

    /// <summary>
    /// View cache that supports expiring content if it is stale
    /// </summary>
    public class DefaultViewCache : IViewCache
    {
        private readonly ConcurrentDictionary<ViewLocationResult, object> cache;

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultViewCache"/> class.
        /// </summary>
        public DefaultViewCache()
        {
            this.cache = new ConcurrentDictionary<ViewLocationResult, object>();
        }

        /// <summary>
        /// Gets or adds a view from the cache.
        /// </summary>
        /// <param name="viewLocationResult">A <see cref="ViewLocationResult"/> instance that describes the view that is being added or retrieved from the cache.</param>
        /// <param name="valueFactory">A function that produces the value that should be added to the cache in case it does not already exist.</param>
        /// <returns>An instance of the view type.</returns>
        public Func<INancyView> GetOrAdd(ViewLocationResult viewLocationResult, Func<ViewLocationResult, Func<INancyView>> valueFactory)
        {
            if (StaticConfiguration.Caching.EnableRuntimeViewUpdates)
            {
                if (viewLocationResult.IsStale())
                {
                    object old;
                    this.cache.TryRemove(viewLocationResult, out old);
                }
            }

            var compiledView = (Func<INancyView>)this.cache.GetOrAdd(viewLocationResult, valueFactory);
            return compiledView;
        }

        ///      <summary>
        ///
        ///      </summary>
        /// <param name="viewLocator"></param>
        /// <param name="viewEngines"></param>
        /// <param name="rootPathProvider"></param>
        /// <param name="traceLog"></param>
        /// <returns></returns>
        public IEnumerable<ViewTemplateCompilationResult<IViewTemplate>> CacheAllViewTemplates(IViewLocator viewLocator, IEnumerable<IViewEngine> viewEngines, IRootPathProvider rootPathProvider, ITraceLog traceLog)
        {
            this.cache.Clear();

            var viewLocationResults = viewLocator.GetAllCurrentlyDiscoveredViews().ToList();
            foreach (var viewEnginesByExtension in viewEngines.GroupBy(ve => ve.Extensions))
            {
                var viewEngineViewLocationResults = viewLocationResults.Where(v => viewEnginesByExtension.Key.Any(extension => extension == v.Extension)).ToList();
                foreach (var viewEngine in viewEnginesByExtension)
                {
                    var viewTemplateCompilationResults = viewEngine.CompileViewTemplates(viewEngineViewLocationResults, traceLog);
                    foreach (var viewTemplateCompilationResult in viewTemplateCompilationResults)
                    {
                        var nancyViewTemplate = viewTemplateCompilationResult.ViewTemplate as NancyViewTemplate;
                        if (nancyViewTemplate == null)
                        {
                            throw new ArgumentOutOfRangeException("The view template instance must be convertible to " + typeof(NancyViewTemplate) + ".");
                        }
                        this.cache.GetOrAdd(nancyViewTemplate.ViewLocationResult, vlr => viewTemplateCompilationResult.CompiledViewTemplateFactory);
                        yield return viewTemplateCompilationResult;
                    }
                }
            }
        }

        /// <summary>Returns an enumerator that iterates through the collection.</summary>
        /// <returns>A <see cref="T:System.Collections.Generic.IEnumerator`1" /> that can be used to iterate through the collection.</returns>
        public IEnumerator<KeyValuePair<ViewLocationResult, object>> GetEnumerator()
        {
            return cache.GetEnumerator();
        }

        /// <summary>Returns an enumerator that iterates through a collection.</summary>
        /// <returns>An <see cref="T:System.Collections.IEnumerator" /> object that can be used to iterate through the collection.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return cache.GetEnumerator();
        }
    }
}