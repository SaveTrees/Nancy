namespace Nancy.ViewEngines.Razor
{
	using System;
	using System.IO;

	/// <summary>
	/// 
	/// </summary>
	public interface IRazorViewEngine : IViewEngine
	{
		/// <summary>
		/// 
		/// </summary>
		/// <param name="viewLocationResult"></param>
		/// <param name="model"></param>
		/// <param name="renderContext"></param>
		/// <param name="isPartial"></param>
		/// <returns></returns>
		Action<Stream> RenderView(ViewLocationResult viewLocationResult, dynamic model, IRenderContext renderContext, bool isPartial);
	}
}