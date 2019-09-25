﻿using System.Globalization;
using System.Threading;
using System.Web.Mvc;
using System.Web.Routing;
using System.Web.UI;
using Devesprit.DigiCommerce.Controllers.Event;
using JetBrains.Annotations;

namespace Devesprit.DigiCommerce.Controllers
{
    public partial class ErrorController : BaseController
    {
        protected override void Execute(RequestContext requestContext)
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo(WorkContext.CurrentLanguage.IsoCode);
            Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(WorkContext.CurrentLanguage.IsoCode);
              
            base.Execute(requestContext);
        }

        [OutputCache(Duration = 60 * 60, Location = OutputCacheLocation.Server, VaryByParam = "*", VaryByCustom = "lang")]
        public virtual ActionResult PageNotFound([CanBeNull] string errorCode)
        {
            if (User.Identity.IsAuthenticated && User.IsInRole("Admin"))
            {  
                ViewBag.ErrorCode = errorCode;
            }

            EventPublisher.Publish(new PageNotFoundEvent(HttpContext, errorCode));

            return View("PageNotFound");
        }

        [OutputCache(Duration = 60 * 60, Location = OutputCacheLocation.Server, VaryByParam = "*", VaryByCustom = "lang")]
        public virtual ActionResult Index([CanBeNull] string errorCode)
        {
            if (User != null && User.Identity.IsAuthenticated && User.IsInRole("Admin"))
            {
                ViewBag.ErrorCode = errorCode;
            }

            EventPublisher.Publish(new ErrorEvent(HttpContext, errorCode));

            return View("Error");
        }
    }
}