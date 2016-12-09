namespace Nancy.ViewEngines.SuperSimpleViewEngine
{
    public class SuperSimpleView : INancyView
    {
        public SuperSimpleView(string body)
        {
            Body = body;
        }

        /// <summary>
        /// The body of the compiled view template.
        /// </summary>
        public string Body { get; }
    }
}
