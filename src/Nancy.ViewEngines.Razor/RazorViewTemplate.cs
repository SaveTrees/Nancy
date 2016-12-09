using System;
using System.CodeDom;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.Razor;
using System.Web.Razor.Parser.SyntaxTree;
using Nancy.Bootstrapper;

namespace Nancy.ViewEngines.Razor
{
    /// <summary>
    ///
    /// </summary>
    public abstract class RazorViewTemplate : NancyViewTemplate
    {
        ///    <summary>
        ///
        ///    </summary>
        /// <param name="razorViewRenderer"></param>
        /// <param name="autoIncludeModelNamespace"></param>
        ///  <param name="id"></param>
        ///    <param name="razorCodeLanguage"></param>
        ///   <param name="viewLocationResult"></param>
        protected RazorViewTemplate(RazorCodeLanguage razorCodeLanguage, ViewLocationResult viewLocationResult, IRazorViewRenderer razorViewRenderer, bool autoIncludeModelNamespace = true, string id = null)
            : base(Path.Combine(viewLocationResult.Location, viewLocationResult.Name + "." + viewLocationResult.Extension), razorCodeLanguage.LanguageName, viewLocationResult, id)
        {
            RazorCodeLanguage = razorCodeLanguage;
            RazorViewRenderer = razorViewRenderer;

            using (var reader = viewLocationResult.Contents.Invoke())
            {
                var razorTemplateEngine = new RazorTemplateEngine(RazorViewRenderer.Host);
                GeneratedCodeResults = razorTemplateEngine.GenerateCode(reader, Id, null, FileName);
            }

            ModelType = FindModelType(GeneratedCodeResults.Document, RazorViewRenderer.ModelCodeGenerator);

            if (autoIncludeModelNamespace)
            {
                AddModelNamespace(GeneratedCodeResults, ModelType);
            }

        }

        /// <summary>
        ///  The razor view renderer.
        /// </summary>
        public IRazorViewRenderer RazorViewRenderer { get; }

        /// <summary>
        /// The razor code language.
        /// </summary>
        public RazorCodeLanguage RazorCodeLanguage { get; }

        /// <summary>
        /// The type of the model- the default is <see cref="Object"/>, which allows dynamic binding to any model in the view template.
        /// </summary>
        public Type ModelType { get; }

        /// <summary>
        /// The generated code results after view template compilation.
        /// </summary>
        public GeneratorResults GeneratedCodeResults { get; }

        /// <summary>
        /// Tries to find the model type from the document
        /// So documents using @model will actually be able to reference the model type
        /// </summary>
        /// <param name="block">The document</param>
        /// <param name="modelCodeGenerator">The model code generator</param>
        /// <param name="passedModelType">The model type from the base class</param>
        /// <returns>The model type, if discovered, or the passedModelType if not</returns>
        private static Type FindModelType(Block block, Type modelCodeGenerator, Type passedModelType = null)
        {
            var modelBlock = block.Flatten().FirstOrDefault(b => b.CodeGenerator.GetType() == modelCodeGenerator);

            if (modelBlock == null || string.IsNullOrEmpty(modelBlock.Content))
            {
                return passedModelType ?? typeof(object);
            }

            var discoveredModelType = modelBlock.Content.Trim();
            var modelType = Type.GetType(discoveredModelType);
            if (modelType != null)
            {
                return modelType;
            }

            modelType = AppDomainAssemblyTypeScanner.Types.FirstOrDefault(t => t.FullName == discoveredModelType);
            if (modelType != null)
            {
                return modelType;
            }

            modelType = AppDomainAssemblyTypeScanner.Types.FirstOrDefault(t => t.Name == discoveredModelType);
            if (modelType != null)
            {
                return modelType;
            }

            throw new NotSupportedException(string.Format(
                "Unable to discover CLR Type for model by the name of {0}.\n\nTry using a fully qualified type name and ensure that the assembly is added to the configuration file.\n\nAppDomain Assemblies:\n\t{1}.\n\nCurrent ADATS assemblies:\n\t{2}.\n\nAssemblies in directories\n\t{3}",
                discoveredModelType,
                AppDomain.CurrentDomain.GetAssemblies().Select(a => a.FullName).Aggregate((n1, n2) => n1 + "\n\t" + n2),
                AppDomainAssemblyTypeScanner.Assemblies.Select(a => a.FullName).Aggregate((n1, n2) => n1 + "\n\t" + n2),
                GetAssembliesInDirectories().Aggregate((n1, n2) => n1 + "\n\t" + n2)));
        }

        private static void AddModelNamespace(GeneratorResults razorResult, Type modelType)
        {
            if (string.IsNullOrWhiteSpace(modelType.Namespace) || razorResult.GeneratedCode.Namespaces[0].Imports.OfType<CodeNamespaceImport>().Any(x => x.Namespace == modelType.Namespace))
            {
                return;
            }

            razorResult.GeneratedCode.Namespaces[0].Imports.Add(new CodeNamespaceImport(modelType.Namespace));
        }

        private static IEnumerable<string> GetAssembliesInDirectories()
        {
            return GetAssemblyDirectories().SelectMany(d => Directory.GetFiles(d, "*.dll"));
        }

        /// <summary>
        /// Returns the directories containing dll files. It uses the default convention as stated by microsoft.
        /// </summary>
        /// <see cref="http://msdn.microsoft.com/en-us/library/system.appdomainsetup.privatebinpathprobe.aspx"/>
        private static IEnumerable<string> GetAssemblyDirectories()
        {
            var privateBinPathDirectories = AppDomain.CurrentDomain.SetupInformation.PrivateBinPath == null
                ? new string[] {}
                : AppDomain.CurrentDomain.SetupInformation.PrivateBinPath.Split(';');

            foreach (var privateBinPathDirectory in privateBinPathDirectories)
            {
                if (!string.IsNullOrWhiteSpace(privateBinPathDirectory))
                {
                    yield return privateBinPathDirectory;
                }
            }

            if (AppDomain.CurrentDomain.SetupInformation.PrivateBinPathProbe == null)
            {
                yield return AppDomain.CurrentDomain.SetupInformation.ApplicationBase;
            }
        }
    }
}