﻿using System;
using System.Web.Mvc;
using Devesprit.Core.Localization;
using Devesprit.Core.Settings;
using Devesprit.DigiCommerce.Controllers;
using Elmah;
using Plugin.Other.SMS.Models;

namespace Plugin.Other.SMS.Controllers
{
    public partial class SmsNotifierController : BaseController
    {
        private readonly ILocalizationService _localizationService;
        private readonly ISettingService _settingService;

        public SmsNotifierController(ILocalizationService localizationService, ISettingService settingService)
        {
            _localizationService = localizationService;
            _settingService = settingService;
        }

        [Authorize(Roles = "Admin")]
        public virtual ActionResult Configure()
        {
            var model = _settingService.LoadSetting<SmsNotifierSettingsModel>();
            return View("~/Plugins/Plugin.Other.SMS/Views/Configure.cshtml", model);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public virtual ActionResult Configure(SmsNotifierSettingsModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("~/Plugins/Plugin.Other.SMS/Views/Configure.cshtml", model);
            }

            try
            {
                _settingService.SaveSetting(model);
            }
            catch (Exception e)
            {
                var errorCode = ErrorLog.GetDefault(System.Web.HttpContext.Current).Log(new Error(e, System.Web.HttpContext.Current));
                ModelState.AddModelError("", string.Format(_localizationService.GetResource("ErrorOnOperation"), e.Message, errorCode));
                return View("~/Plugins/Plugin.Other.SMS/Views/Configure.cshtml", model);
            }
            
            return Content(@"<script language='javascript' type='text/javascript'>
                                window.close();
                             </script>");
        }
    }
}
