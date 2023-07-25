using System.Web.Mvc;

public class AllowCrossSiteJsonAttribute : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext filterContext)
    {
        filterContext.RequestContext.HttpContext.Response.AddHeader("Access-Control-Allow-Origin", "*");
        filterContext.RequestContext.HttpContext.Response.AddHeader("Access-Control-Allow-Headers", "content-type");
        base.OnActionExecuting(filterContext);
    }
}