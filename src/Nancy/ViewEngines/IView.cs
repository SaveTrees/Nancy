namespace Nancy.ViewEngines
{
    /// <summary>
    ///
    /// </summary>
    public interface INancyView
    {
        /// <summary>
        /// The body of the compiled view template.
        /// </summary>
        /// <returns></returns>
        string Body { get; }
    }
}