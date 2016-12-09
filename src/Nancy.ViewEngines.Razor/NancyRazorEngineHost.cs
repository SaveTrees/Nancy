namespace Nancy.ViewEngines.Razor
{
    using System;
    using System.Web.Razor;
    using System.Web.Razor.Generator;
    using System.Web.Razor.Parser;

    using CSharp;

    using VisualBasic;

    /// <summary>
    /// A custom razor engine host responsible for decorating the existing code generators with nancy versions.
    /// </summary>
    public sealed class NancyRazorEngineHost : RazorEngineHost
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NancyRazorEngineHost"/> class.
        /// </summary>
        /// <param name="language">The language.</param>
        /// <param name="defaultPageBaseType"></param>
        public NancyRazorEngineHost(RazorCodeLanguage language, Type defaultPageBaseType = null)
            : base(language)
        {
            if (defaultPageBaseType == null)
            {
                defaultPageBaseType = typeof (NancyRazorViewBase);
            }

            this.DefaultBaseClass = defaultPageBaseType.FullName;
            this.DefaultNamespace = "NancyRazorGeneratedOutput";
            this.DefaultClassName = "RazorView";

            var context = new GeneratedClassContext("Execute", "Write", "WriteLiteral", "WriteTo", "WriteLiteralTo", typeof(HelperResult).FullName, "DefineSection")
            {
                ResolveUrlMethodName = "ResolveUrl"
            };
            this.GeneratedClassContext = context;
        }

        /// <summary>
        /// Decorates the code parser.
        /// </summary>
        /// <param name="incomingCodeParser">The incoming code parser.</param>
        /// <returns></returns>
        public override ParserBase DecorateCodeParser(ParserBase incomingCodeParser)
        {
            if (incomingCodeParser is CSharpCodeParser)
            {
                return new NancyCSharpRazorCodeParser();
            }

            if (incomingCodeParser is VBCodeParser)
            {
                return new NancyVisualBasicRazorCodeParser();
            }

            return base.DecorateCodeParser(incomingCodeParser);
        }
    }
}