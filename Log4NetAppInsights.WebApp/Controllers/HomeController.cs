using log4net;
using Log4NetAppInsights.AppInsightsOperationAppender;
using System;
using System.Linq;
using System.Web.Mvc;

namespace Log4NetAppInsights.WebApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILog log;

        public HomeController()
        {
            this.log = LogManager.GetLogger(GetType());
        }

        public ActionResult Index()
        {
            // Using the ILog extension methods defined in the AppInsightsOperationAppender project
            // to add correlation metadata (operationId, parentOperationId).

            // See https://docs.microsoft.com/en-us/azure/azure-monitor/app/correlation#data-model-for-telemetry-correlation for more information.

            var operationId = Guid.NewGuid().ToString();

            log.Info("GET Index", operationId, GetRootRequestId());

            return View();
        }

        public ActionResult About()
        {
            var operationId = Guid.NewGuid().ToString();

            log.Info("GET About", operationId, GetRootRequestId());

            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            var operationId = Guid.NewGuid().ToString();

            log.Info("GET Contact", operationId, GetRootRequestId());

            ViewBag.Message = "Your contact page.";

            return View();
        }

        private string GetRootRequestId()
        {
            const string aiRequestHeaderName = "ApplicationInsights-RequestTrackingTelemetryModule-RootRequest-Id";

            if (Request?.Headers.AllKeys.Contains(aiRequestHeaderName) == true)
            {
                return Request.Headers[aiRequestHeaderName];
            }
            else
            {
                return null;
            }
        }
    }
}