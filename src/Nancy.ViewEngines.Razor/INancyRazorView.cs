namespace Nancy.ViewEngines.Razor
{
    using System.Collections.Generic;

    /// <summary>
    /// A Nancy razor view.
    /// </summary>
    public interface INancyRazorView : INancyView
    {
        /// <summary>
        /// Writes literals like markup: "<p>Foo</p>"
        /// </summary>
        /// <param name="value">The value.</param>
        void WriteLiteral(object value);

        /// <summary>
        ///
        /// </summary>
        /// <param name="razorViewEngine"></param>
        /// <param name="renderContext"></param>
        /// <param name="model"></param>
        void Initialize(IRazorViewEngine razorViewEngine, IRenderContext renderContext, dynamic model);

        /// <summary>
        /// Gets or sets the section contents.
        /// </summary>
        /// <value>
        /// The section contents.
        /// </value>
        IDictionary<string, string> SectionContents { get; set; }

        /// <summary>
        /// Gets a value indicating whether this instance has layout.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance has layout; otherwise, <c>false</c>.
        /// </value>
        bool HasLayout { get; }

        /// <summary>
        /// Gets or sets the layout.
        /// </summary>
        /// <value>
        /// The layout.
        /// </value>
        string Layout { get; set; }

        /// <summary>
        /// Executes the view.
        /// </summary>
        /// <param name="body">The body.</param>
        /// <param name="sectionContents">The section contents.</param>
        void ExecuteView(string body, IDictionary<string, string> sectionContents);
    }

    /// <summary>
    /// A Nancy razor view.
    /// </summary>
    public interface INancyRazorView<TModel> : INancyRazorView
    {
    }
}