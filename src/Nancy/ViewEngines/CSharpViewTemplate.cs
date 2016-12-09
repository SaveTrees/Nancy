using Nancy.Extensions;

namespace Nancy.ViewEngines
{
    public abstract class CSharpViewTemplate : IViewTemplate
    {
        protected CSharpViewTemplate(string id, string fileName)
        {
            Id = id;
            FileName = fileName.NormalizePathName();
            LanguageName = "csharp";
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
        /// The normalized, relative or absolute path and name of the view template file.
        /// </summary>
        public string FileName { get; }
    }
}
