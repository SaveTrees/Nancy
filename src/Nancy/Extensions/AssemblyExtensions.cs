namespace Nancy.Extensions
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Reflection;

	/// <summary>
	/// Assembly extension methods
	/// </summary>
	public static class AssemblyExtensions
	{
		/// <summary>
		/// Gets exported types from an assembly and catches common errors
		/// that occur when running under test runners.
		/// </summary>
		/// <param name="assembly">Assembly to retrieve from</param>
		/// <returns>An array of types</returns>
		public static Type[] SafeGetExportedTypes(this Assembly assembly)
		{
			Type[] types;

		    try
		    {
		        types = assembly.GetExportedTypes();
		    }
		    catch (FileNotFoundException)
		    {
		        types = new Type[] {};
		    }
		    catch (NotSupportedException)
		    {
		        types = new Type[] {};
		    }
		    catch (FileLoadException)
		    {
		        // probably assembly version conflict
		        types = new Type[] {};
		    }
            //catch (ReflectionTypeLoadException rtle)
            //{
            //    // Mono issue.
            //    types = new Type[] { };
            //}
            catch (Exception exception)
		    {
		        //Log.CurrentLogger.Debug()("Couldn't export types from assembly: {@Assembly}", assembly);
                var exception2 = new Exception("assembly: " + assembly.FullName, exception);
		        throw exception2;
		    }

		    return types;
		}

		public static IEnumerable<Assembly> LoadAndReturnReferencedAssemblies(this Assembly assembly, bool loadRecursively = true, HashSet<string> currentlyLoading = null)
		{
			if (currentlyLoading == null)
			{
				currentlyLoading = new HashSet<string>();
			}

			foreach (var assemblyName in assembly.GetReferencedAssemblies())
			{
				if (!currentlyLoading.Contains(assemblyName.FullName))
				{
					currentlyLoading.Add(assemblyName.FullName);
					var loadedAssembly = Assembly.Load(assemblyName);
					if (loadRecursively)
					{
						foreach (var a in loadedAssembly.LoadAndReturnReferencedAssemblies(true, currentlyLoading))
						{
							yield return a;
						}
					}

					yield return loadedAssembly;
				}
			}
		}
	}
}