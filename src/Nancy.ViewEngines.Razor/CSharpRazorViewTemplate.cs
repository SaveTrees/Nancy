using Nancy.ViewEngines.Razor.CSharp;

namespace Nancy.ViewEngines.Razor
{
    /// <summary>
    ///
    /// </summary>
    public class CSharpRazorViewTemplate : RazorViewTemplate
    {
        /// <summary>
        ///
        /// </summary>
        /// <param name="viewLocationResult"></param>
        /// <param name="cSharpRazorViewRenderer"></param>
        /// <param name="autoIncludeModelNamespace"></param>
        public CSharpRazorViewTemplate(ViewLocationResult viewLocationResult, CSharpRazorViewRenderer cSharpRazorViewRenderer, bool autoIncludeModelNamespace)
            :base(cSharpRazorViewRenderer.RazorCodeLanguage, viewLocationResult, cSharpRazorViewRenderer, autoIncludeModelNamespace)
        {
        }
    }
}