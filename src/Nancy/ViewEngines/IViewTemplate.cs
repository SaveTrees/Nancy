namespace Nancy.ViewEngines
{
    /// <summary>
    ///
    /// </summary>
    public interface IViewTemplate
    {
        /// <summary>
        /// A unique id for providing to code generators.
        /// </summary>
        string Id { get; }

        /// <summary>
        /// The name of the language for code used within the view template.
        /// </summary>
        string LanguageName { get; }

        /// <summary>
        /// The fulle name of the view template file.
        /// </summary>
        string FileName { get; }
    }
}