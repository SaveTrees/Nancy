using System;
using System.Collections.Generic;
using System.Linq;

namespace Nancy.ViewEngines
{
    /// <summary>
    /// Holds the results of compiling a view template.
    /// </summary>
    public class ViewTemplateCompilationResult<TViewTemplate> where TViewTemplate : IViewTemplate
    {
        /// <summary>
        /// Initialise
        /// </summary>
        /// <param name="compiledViewTemplateFactory"></param>
        /// <param name="viewTemplate"></param>
        /// <param name="errors"></param>
        public ViewTemplateCompilationResult(Func<INancyView> compiledViewTemplateFactory, TViewTemplate viewTemplate, ICollection<string> errors = null)
        {
            Errors = errors ?? new List<string>();
            Succeeded = !Errors.Any();
            CompiledViewTemplateFactory = compiledViewTemplateFactory;
            ViewTemplate = viewTemplate;
        }

        /// <summary>
        /// The errors, if the compilation failed.
        /// </summary>
        public IEnumerable<string> Errors { get; }

        /// <summary>
        /// Whether the compilation of the view succeeded.
        /// </summary>
        public bool Succeeded { get; }

        /// <summary>
        /// The factory for creating a new instance of the compiled view template.
        /// </summary>
        public Func<INancyView> CompiledViewTemplateFactory { get; }

        /// <summary>
        /// The view template.
        /// </summary>
        public TViewTemplate ViewTemplate { get; }
    }
}