using Nancy.Diagnostics;
using Nancy.Extensions;
using Nancy.ViewEngines.Razor.CSharp;

namespace Nancy.ViewEngines.Razor
{
    using System;
    using System.CodeDom;
    using System.CodeDom.Compiler;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Web.Razor;
    using System.Web.Razor.Parser.SyntaxTree;
    using Bootstrapper;
    using Helpers;
    using Responses;

    /// <summary>
    /// View engine for rendering razor views.
    /// </summary>
    public class RazorViewEngine : IRazorViewEngine, IDisposable
    {
        private readonly IRazorConfiguration razorConfiguration;
        private readonly IEnumerable<IRazorViewRenderer> viewRenderers;
        private readonly object compileLock = new object();
        private readonly string _nancyTempPath;

        /// <summary>
        /// Gets the extensions file extensions that are supported by the view engine.
        /// </summary>
        /// <value>An <see cref="IEnumerable{T}"/> instance containing the extensions.</value>
        /// <remarks>The extensions should not have a leading dot in the name.</remarks>
        public IEnumerable<string> Extensions
        {
            get { return this.viewRenderers.Select(x => x.Extension); }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RazorViewEngine"/> class.
        /// </summary>
        /// <param name="configuration">The <see cref="IRazorConfiguration"/> that should be used by the engine.</param>
        /// <param name="defaultPageBaseType"></param>
        public RazorViewEngine(IRazorConfiguration configuration, Type defaultPageBaseType = null)
        {
            _nancyTempPath = Path.Combine(Path.GetTempPath(), "Nancy");
            if (!Directory.Exists(_nancyTempPath))
            {
                //Directory.Delete(_nancyTempPath, true);
                Directory.CreateDirectory(_nancyTempPath);
            }

            this.viewRenderers = new List<IRazorViewRenderer>
            {
                new CSharp.CSharpRazorViewRenderer(defaultPageBaseType),
                new VisualBasic.VisualBasicRazorViewRenderer(defaultPageBaseType)
            };

            this.razorConfiguration = configuration;

            foreach (var renderer in this.viewRenderers)
            {
                this.AddDefaultNameSpaces(renderer.Host);
            }
        }

        /// <summary>
        /// Initialise the view engine (if necessary)
        /// </summary>
        /// <param name="viewEngineStartupContext">Startup context</param>
        public void Initialize(ViewEngineStartupContext viewEngineStartupContext)
        {
        }

        /// <summary>
        /// Renders the view.
        /// </summary>
        /// <param name="viewLocationResult">A <see cref="ViewLocationResult"/> instance, containing information on how to get the view template.</param>
        /// <param name="model">The model that should be passed into the view</param>
        /// <param name="renderContext">The render context.</param>
        /// <returns>A response.</returns>
        public Response RenderView(ViewLocationResult viewLocationResult, dynamic model, IRenderContext renderContext)
        {
            var response = new HtmlResponse
            {
                Contents = RenderView(viewLocationResult, model, renderContext, false)
            };

            return response;
        }

        /// <summary>
        /// Renders the view.
        /// </summary>
        /// <param name="viewLocationResult">A <see cref="ViewLocationResult"/> instance, containing information on how to get the view template.</param>
        /// <param name="model">The model that should be passed into the view</param>
        /// <param name="renderContext">The render context.</param>
        /// <param name="isPartial">Used by HtmlHelpers to declare a view as partial</param>
        /// <returns>A response.</returns>
        public Action<Stream> RenderView(ViewLocationResult viewLocationResult, dynamic model, IRenderContext renderContext, bool isPartial)
        {
            Assembly referencingAssembly = null;

            if (model != null)
            {
                var underlyingSystemType = model.GetType().UnderlyingSystemType;
                if (underlyingSystemType != null)
                {
                    referencingAssembly = Assembly.GetAssembly(underlyingSystemType);
                }
            }

            return stream =>
            {
                var writer = new StreamWriter(stream);

                var view = (INancyRazorView) this.GetViewInstance(viewLocationResult, renderContext.ViewCache, referencingAssembly, model);
                InitializeView(view, renderContext, model);

                view.ExecuteView(null, null);

                var body = view.Body;
                var sectionContents = view.SectionContents;

                var layout = view.HasLayout ? view.Layout : GetViewStartLayout(model, renderContext, referencingAssembly, isPartial);

                var root = string.IsNullOrWhiteSpace(layout);

                while (!root)
                {
                    var viewLocation =
                        renderContext.LocateView(layout, model);

                    if (viewLocation == null)
                    {
                        throw new InvalidOperationException("Unable to locate layout: " + layout);
                    }

                    view = (INancyRazorView) this.GetViewInstance(viewLocation, renderContext.ViewCache, referencingAssembly, model);
                    InitializeView(view, renderContext, model);

                    view.ExecuteView(body, sectionContents);

                    body = view.Body;
                    sectionContents = view.SectionContents;

                    layout = view.HasLayout ? view.Layout : GetViewStartLayout(model, renderContext, referencingAssembly, isPartial);

                    root = !view.HasLayout;
                }

                writer.Write(body);
                writer.Flush();
            };
        }

        private string GetViewStartLayout(dynamic model, IRenderContext renderContext, Assembly referencingAssembly, bool isPartial)
        {
            if (isPartial)
            {
                return string.Empty;
            }

            var viewLocationResult = renderContext.LocateView("_ViewStart", model);

            if (viewLocationResult == null)
            {
                return string.Empty;
            }

            if (!this.Extensions.Any(x => x.Equals(viewLocationResult.Extension, StringComparison.OrdinalIgnoreCase)))
            {
                return string.Empty;
            }

            var view = (INancyRazorView) GetViewInstance(viewLocationResult, renderContext.ViewCache, referencingAssembly, model);
            InitializeView(view, renderContext, model);

            view.ExecuteView(null, null);

            return view.Layout ?? string.Empty;
        }

        private void AddDefaultNameSpaces(RazorEngineHost engineHost)
        {
            engineHost.NamespaceImports.Add("System");
            engineHost.NamespaceImports.Add("System.IO");

            if (this.razorConfiguration != null)
            {
                var namespaces = this.razorConfiguration.GetDefaultNamespaces();

                if (namespaces == null)
                {
                    return;
                }

                foreach (var n in namespaces.Where(n => !string.IsNullOrWhiteSpace(n)))
                {
                    engineHost.NamespaceImports.Add(n);
                }
            }
        }

        private Func<INancyRazorView> GetCompiledViewFactory(string extension, TextReader reader, Assembly referencingAssembly, Type passedModelType, ViewLocationResult viewLocationResult)
        {
            var renderer = this.viewRenderers.First(x => x.Extension.Equals(extension, StringComparison.OrdinalIgnoreCase));
            var engine = new RazorTemplateEngine(renderer.Host);
            var razorResult = engine.GenerateCode(reader, null, null, "roo");
            var viewFactory = this.GenerateRazorViewFactory(renderer, razorResult, referencingAssembly, passedModelType, viewLocationResult);

            return viewFactory;
        }

        private Func<INancyRazorView> GenerateRazorViewFactory(IRazorViewRenderer viewRenderer, GeneratorResults razorResult, Assembly referencingAssembly, Type passedModelType, ViewLocationResult viewLocationResult)
        {
            var outputAssemblyName = Path.Combine(_nancyTempPath, string.Format("NancyRazorView_{0}.dll", Guid.NewGuid().ToString("N")));
            var modelType = FindModelType(razorResult.Document, passedModelType, viewRenderer.ModelCodeGenerator);

            var assemblies = new List<string>
            {
                GetAssemblyPath(typeof(System.Runtime.CompilerServices.CallSite)),
                GetAssemblyPath(typeof(IHtmlString)),
                GetAssemblyPath(Assembly.GetExecutingAssembly()),
                GetAssemblyPath(modelType)
            };

            assemblies.AddRange(AppDomainAssemblyTypeScanner.Assemblies.Select(GetAssemblyPath));

            if (referencingAssembly != null)
            {
                assemblies.Add(GetAssemblyPath(referencingAssembly));
            }

            if (this.razorConfiguration != null)
            {
                var assemblyNames = this.razorConfiguration.GetAssemblyNames();
                if (assemblyNames != null)
                {
                    assemblies.AddRange(assemblyNames.Select(Assembly.Load).Select(GetAssemblyPath));
                }

                if (this.razorConfiguration.AutoIncludeModelNamespace)
                {
                    AddModelNamespace(razorResult, modelType);
                }
            }

            var assemblies2 = assemblies
                .Union(viewRenderer.Assemblies)
                .Distinct()
                .OrderBy(a => a)
                .ToArray();

            var compilerParameters = new CompilerParameters(assemblies2, outputAssemblyName);

            CompilerResults results;
            lock (compileLock)
            {
                results = viewRenderer.Provider.CompileAssemblyFromDom(compilerParameters, razorResult.GeneratedCode);
            }

            if (results.Errors.HasErrors)
            {
                //var output = new string[results.Output.Count];
                //results.Output.CopyTo(output, 0);

                var fullTemplateName = viewLocationResult.Location + "/" + viewLocationResult.Name + "." + viewLocationResult.Extension;
                var templateLines = GetViewBodyLines(viewLocationResult);
                var errors = results.Errors.OfType<CompilerError>().Where(ce => !ce.IsWarning).ToArray();
                var errorMessages = BuildErrorMessages(errors);
                var compilationSource = this.GetCompilationSource(viewRenderer.Provider, razorResult.GeneratedCode);

                MarkErrorLines(errors, templateLines);

                var lineNumber = 1;

                var templateLinesAggregate = templateLines.Any() ? templateLines.Aggregate((s1, s2) => s1 + "<br/>" + s2) : "";
                var compliationSouceAggregate = compilationSource.Any() ? compilationSource.Aggregate((s1, s2) => s1 + "<br/>Line " + lineNumber++ + ":\t" + s2) : "";
                var errorDetails = string.Format(
                    "Error compiling template: <strong>{0}</strong><br/><br/>Errors:<br/>{1}<br/><br/>Details:<br/>{2}<br/><br/>Compilation Source:<br/><pre><code>{3}</code></pre>",
                    fullTemplateName,
                    errorMessages,
                    templateLinesAggregate,
                    compliationSouceAggregate);

                return () => new NancyRazorErrorView(errorDetails);
            }

            var assembly = Assembly.LoadFrom(outputAssemblyName);
            if (assembly == null)
            {
                const string error = "Error loading template assembly";
                return () => new NancyRazorErrorView(error);
            }

            var type = assembly.GetType("NancyRazorGeneratedOutput.RazorView");
            if (type == null)
            {
                var error = String.Format("Could not find type NancyRazorGeneratedOutput.Template in assembly {0}", assembly.FullName);
                return () => new NancyRazorErrorView(error);
            }

            return () =>
            {
                var instance = Activator.CreateInstance(type);
                var view = instance as INancyRazorView;
                if (view == null)
                {
                    const string error = "Could not construct NancyRazorGeneratedOutput.Template or it does not inherit from INancyRazorView";
                    return new NancyRazorErrorView(error);
                }

                return view;
            };
        }

        private string[] GetCompilationSource(CodeDomProvider provider, CodeCompileUnit generatedCode)
        {
            var compilationSourceBuilder = new StringBuilder();
            using (var writer = new IndentedTextWriter(new StringWriter(compilationSourceBuilder), "\t"))
            {
                provider.GenerateCodeFromCompileUnit(generatedCode, writer, new CodeGeneratorOptions());
            }

            var compilationSource = compilationSourceBuilder.ToString();
            return HttpUtility.HtmlEncode(compilationSource)
                .Split(new[] {Environment.NewLine}, StringSplitOptions.None);
        }

        private static string BuildErrorMessages(IEnumerable<CompilerError> errors)
        {
            return errors.Select(error => string.Format(
                "[{0}] Line: {1} Column: {2} - {3} (<a class='LineLink' href='#{1}'>show</a>)",
                error.ErrorNumber,
                error.Line,
                error.Column,
                error.ErrorText)).Aggregate((s1, s2) => s1 + "<br/>" + s2);
        }

        private static void MarkErrorLines(IEnumerable<CompilerError> errors, IList<string> templateLines)
        {
            foreach (var compilerError in errors)
            {
                var lineIndex = compilerError.Line - 1;
                if ((lineIndex <= templateLines.Count - 1) && (lineIndex >= 0))
                {
                    templateLines[lineIndex] = string.Format("<span class='error'><a name='{0}' />{1}</span>", compilerError.Line, templateLines[lineIndex]);
                }
            }
        }

        private static string[] GetViewBodyLines(ViewLocationResult viewLocationResult)
        {
            var templateLines = new List<string>();
            using (var templateReader = viewLocationResult.Contents.Invoke())
            {
                var currentLine = templateReader.ReadLine();
                while (currentLine != null)
                {
                    templateLines.Add(Helpers.HttpUtility.HtmlEncode(currentLine));

                    currentLine = templateReader.ReadLine();
                }
            }
            return templateLines.ToArray();
        }

        /// <summary>
        /// Tries to find the model type from the document
        /// So documents using @model will actually be able to reference the model type
        /// </summary>
        /// <param name="block">The document</param>
        /// <param name="passedModelType">The model type from the base class</param>
        /// <param name="modelCodeGenerator">The model code generator</param>
        /// <returns>The model type, if discovered, or the passedModelType if not</returns>
        private static Type FindModelType(Block block, Type passedModelType, Type modelCodeGenerator)
        {
            var modelBlock =
                block.Flatten().FirstOrDefault(b => b.CodeGenerator.GetType() == modelCodeGenerator);

            if (modelBlock == null)
            {
                return passedModelType ?? typeof(object);
            }

            if (string.IsNullOrEmpty(modelBlock.Content))
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

        private static IEnumerable<String> GetAssembliesInDirectories()
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

        private static void AddModelNamespace(GeneratorResults razorResult, Type modelType)
        {
            if (string.IsNullOrWhiteSpace(modelType.Namespace))
            {
                return;
            }

            if (razorResult.GeneratedCode.Namespaces[0].Imports.OfType<CodeNamespaceImport>().Any(x => x.Namespace == modelType.Namespace))
            {
                return;
            }

            razorResult.GeneratedCode.Namespaces[0].Imports.Add(new CodeNamespaceImport(modelType.Namespace));
        }

        private static string GetAssemblyPath(Type type)
        {
            return GetAssemblyPath(type.Assembly);
        }

        private static string GetAssemblyPath(Assembly assembly)
        {
            return new Uri(assembly.EscapedCodeBase).LocalPath;
        }

        private INancyView GetOrCompileView(ViewLocationResult viewLocationResult, IViewCache viewCache, Assembly referencingAssembly, Type passedModelType)
        {
            var viewFactory = viewCache.GetOrAdd(
                viewLocationResult,
                x =>
                {
                    using (var reader = x.Contents.Invoke())
                    {
                        return this.GetCompiledViewFactory(x.Extension, reader, referencingAssembly, passedModelType, viewLocationResult);
                    }
                });

            var view = viewFactory.Invoke();

            return view;
        }

        private INancyRazorView GetViewInstance(ViewLocationResult viewLocationResult, IViewCache viewCache, Assembly referencingAssembly, dynamic model)
        {
            var modelType = model == null ? typeof(object) : model.GetType();

            var view = this.GetOrCompileView(viewLocationResult, viewCache, referencingAssembly, modelType);

            return view;
        }

        /// <summary>
        /// Custom view initialization.
        /// </summary>
        /// <param name="view"></param>
        /// <param name="renderContext"></param>
        /// <param name="model"></param>
        public virtual void InitializeView(INancyRazorView view, IRenderContext renderContext, object model)
        {
            view.Initialize(this, renderContext, model);
        }

        /// <summary>
        /// Pre compile the views.
        /// </summary>
        public IEnumerable<ViewTemplateCompilationResult<IViewTemplate>> CompileViewTemplates(ICollection<ViewLocationResult> viewLocationResults, ITraceLog traceLog)
        {
            traceLog?.WriteLog(sb => sb.Append("Compiling all views."));

            // Todo: Convert to factory.
            var allViewTemplates = viewLocationResults.Select(v => new CSharpRazorViewTemplate(v, (CSharpRazorViewRenderer) viewRenderers.First(rvr => rvr.RazorCodeLanguage.LanguageName == RazorCodeLanguage.GetLanguageByExtension(v.Extension).LanguageName), this.razorConfiguration != null && this.razorConfiguration.AutoIncludeModelNamespace));
            foreach (var viewTemplatesByLanguage in allViewTemplates.GroupBy(vt => vt.RazorCodeLanguage))
            {
                var razorViewRenderer = viewRenderers.First(rvr => rvr.RazorCodeLanguage == viewTemplatesByLanguage.Key);
                var outputAssemblyName = Path.Combine(_nancyTempPath, string.Format("NancyRazorView_{0}_{1}.dll", viewTemplatesByLanguage.Key.LanguageName, Guid.NewGuid().ToString("N")));
                var compilerResults = CompileViewTemplatesForLanguage(outputAssemblyName, viewTemplatesByLanguage, razorViewRenderer);

                var errorsByFileNames = compilerResults.Errors
                    .OfType<CompilerError>()
                    .Where(ce => !ce.IsWarning)
                    .GroupBy(e => e.FileName)
                    .ToList();

                if (errorsByFileNames.Any())
                {
                    foreach (var errorsByFileName in errorsByFileNames)
                    {
                        foreach (var viewTemplateCompilationResult in ExtractFileCompilationErrors(errorsByFileName, viewTemplatesByLanguage, razorViewRenderer))
                        {
                            yield return viewTemplateCompilationResult;
                        }
                    }
                }
                else
                {
                    var assembly = Assembly.LoadFrom(outputAssemblyName);
                    if (assembly == null)
                    {
                        foreach (var razorViewTemplate in viewTemplatesByLanguage)
                        {
                            const string errorMessage = "Error loading template assembly";
                            yield return new ViewTemplateCompilationResult<IViewTemplate>(() => new NancyRazorErrorView(errorMessage), razorViewTemplate, new List<string> {errorMessage});
                        }
                    }
                    else
                    {
                        foreach (var razorViewTemplate in viewTemplatesByLanguage)
                        {
                            var type = assembly.GetType("NancyRazorGeneratedOutput." + razorViewTemplate.Id);
                            if (type == null)
                            {
                                var errorMessage = string.Format("Could not find type NancyRazorGeneratedOutput.Template in assembly {0}", assembly.FullName);
                                yield return new ViewTemplateCompilationResult<IViewTemplate>(() => new NancyRazorErrorView(errorMessage), razorViewTemplate, new List<string> {errorMessage});
                            }
                            else
                            {
                                var instance = Activator.CreateInstance(type);
                                var view = instance as INancyRazorView;
                                if (view == null)
                                {
                                    const string errorMessage = "Could not construct NancyRazorGeneratedOutput.Template or it does not inherit from INancyRazorView";
                                    yield return new ViewTemplateCompilationResult<IViewTemplate>(() => new NancyRazorErrorView(errorMessage), razorViewTemplate, new List<string> {errorMessage});
                                }
                                else
                                {
                                    yield return new ViewTemplateCompilationResult<IViewTemplate>(() => (INancyRazorView) Activator.CreateInstance(type), razorViewTemplate);
                                }
                            }
                        }
                    }
                }
            }
        }

        private CompilerResults CompileViewTemplatesForLanguage(string outputAssemblyName, IGrouping<RazorCodeLanguage, CSharpRazorViewTemplate> viewTemplatesByLanguage, IRazorViewRenderer razorViewRenderer)
        {
            var assemblyNames = new HashSet<string>(GetCommonAssmblies());
            foreach (var razorViewRendererAssemblies in razorViewRenderer.Assemblies)
            {
                assemblyNames.Add(razorViewRendererAssemblies);
            }
            foreach (var viewTemplate in viewTemplatesByLanguage)
            {
                assemblyNames.Add(GetAssemblyPath(viewTemplate.ModelType));
            }

            var compilerParameters = new CompilerParameters(assemblyNames.ToArray(), outputAssemblyName)
            {
                GenerateInMemory = true,
                TempFiles = new TempFileCollection(_nancyTempPath)
            };

            lock (compileLock)
            {
                var codeCompileUnits = viewTemplatesByLanguage.Select(vt => vt.GeneratedCodeResults.GeneratedCode).ToArray();
                return razorViewRenderer.Provider.CompileAssemblyFromDom(compilerParameters, codeCompileUnits);
            }
        }

        private HashSet<string> GetCommonAssmblies()
        {
            var assemblyPath = GetAssemblyPath(Assembly.GetExecutingAssembly());

            var commonAssemblyNames = new HashSet<string>
            {
                GetAssemblyPath(typeof(Microsoft.CSharp.RuntimeBinder.Binder)),
                GetAssemblyPath(typeof(System.Runtime.CompilerServices.CallSite)),
                GetAssemblyPath(typeof(IHtmlString)),
                assemblyPath
            };

            foreach (var assemblyName in AppDomainAssemblyTypeScanner.Assemblies.Select(GetAssemblyPath))
            {
                commonAssemblyNames.Add(assemblyName);
            }

            if (this.razorConfiguration != null)
            {
                var razorConfigurationAssemblyNames = this.razorConfiguration.GetAssemblyNames();
                if (razorConfigurationAssemblyNames != null)
                {
                    foreach (var assemblyName in razorConfigurationAssemblyNames.Select(Assembly.Load).Select(GetAssemblyPath))
                    {
                        commonAssemblyNames.Add(assemblyName);
                    }
                }
            }
            return commonAssemblyNames;
        }

        private IEnumerable<ViewTemplateCompilationResult<IViewTemplate>> ExtractFileCompilationErrors(IGrouping<string, CompilerError> errorsByFileName, IGrouping<RazorCodeLanguage, CSharpRazorViewTemplate> viewTemplatesByLanguage, IRazorViewRenderer razorViewRenderer)
        {
            var tempFileName = errorsByFileName.Key.Substring(_nancyTempPath.Length + 1).NormalizePathName();
            // Note: CodeDOM Compiler appears not to respect the directory name casing, so case-sensitive paths must be avoided.
            var razorViewTemplate = viewTemplatesByLanguage.FirstOrDefault(vt => vt.FileName.Equals(tempFileName, StringComparison.OrdinalIgnoreCase));
            if (razorViewTemplate == null)
            {
                // Can't yield, as we don't have an instance of ViewLocationResult.
                throw new ArgumentOutOfRangeException("errorsByFileName", "There were errors in compilation, but the source file could not be found.  There was no (case-insensitive) match between " + tempFileName + " and any razor templates.");
            }

            var templateLines = GetViewBodyLines(razorViewTemplate.ViewLocationResult);
            var errorMessages = BuildErrorMessages(errorsByFileName);
            var compilationSource = this.GetCompilationSource(razorViewRenderer.Provider, razorViewTemplate.GeneratedCodeResults.GeneratedCode);

            MarkErrorLines(errorsByFileName, templateLines);

            var lineNumber = 1;

            var templateLinesAggregate = templateLines.Any() ? templateLines.Aggregate((s1, s2) => s1 + "<br/>" + s2) : "";
            var compliationSouceAggregate = compilationSource.Any() ? compilationSource.Aggregate((s1, s2) => s1 + "<br/>Line " + lineNumber++ + ":\t" + s2) : "";
            var errorDetails = string.Format(
                "Error compiling template: <strong>{0}</strong><br/><br/>Errors:<br/>{1}<br/><br/>Details:<br/>{2}<br/><br/>Compilation Source:<br/><pre><code>{3}</code></pre>",
                razorViewTemplate.FileName,
                errorMessages,
                templateLinesAggregate,
                compliationSouceAggregate);

            yield return new ViewTemplateCompilationResult<IViewTemplate>(() => new NancyRazorErrorView(errorDetails), razorViewTemplate, new List<string> {errorMessages});
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            if (this.viewRenderers != null)
            {
                foreach (var disposable in this.viewRenderers.OfType<IDisposable>())
                {
                    disposable.Dispose();
                }
            }
        }
    }
}