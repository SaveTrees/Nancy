using System;
using Nancy.Extensions;

namespace Nancy.ViewEngines
{
    /// <summary>
    ///
    /// </summary>
    public abstract class NancyViewTemplate : IViewTemplate
    {
        ///   <summary>
        ///
        ///   </summary>
        /// <param name="fileName"></param>
        /// <param name="languageName"></param>
        /// <param name="viewLocationResult"></param>
        /// <param name="id"></param>
        protected NancyViewTemplate(string fileName, string languageName, ViewLocationResult viewLocationResult, string id = null)
        {
            Id = id ?? "NancyRazorView_" + Guid.NewGuid().ToString("N");
            FileName = fileName.NormalizePathName();
            ViewLocationResult = viewLocationResult;
            LanguageName = languageName;
        }

        /// <summary>
        /// A unique id for providing to code generators.
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// The name of the language for code used within the view template.
        /// </summary>
        public string LanguageName { get; }

        /// <summary>
        /// The fulle name of the view template file.
        /// </summary>
        public string FileName { get; }

        /// <summary>
        /// The view location result.
        /// </summary>
        public ViewLocationResult ViewLocationResult { get; }
    }
}