﻿using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using Devesprit.Core.Localization;
using Devesprit.Data.Enums;
using Devesprit.DigiCommerce.Areas.Admin.Factories.Interfaces;
using Devesprit.DigiCommerce.Areas.Admin.Models;
using Devesprit.DigiCommerce.Controllers;
using Devesprit.Services.Blog;
using Devesprit.Services.Pages;
using Devesprit.Services.Products;
using Devesprit.Services.Redirects;
using Devesprit.WebFramework.Helpers;
using Elmah;
using Syncfusion.JavaScript;

namespace Devesprit.DigiCommerce.Areas.Admin.Controllers
{
    [Authorize(Roles = "Admin")]
    public partial class ManageRedirectsController : BaseController
    {
        private readonly IRedirectsService _redirectsService;
        private readonly IRedirectModelFactory _redirectModelFactory;
        private readonly ILocalizationService _localizationService;
        private readonly IProductService _productService;
        private readonly IBlogPostService _blogPostService;
        private readonly IPagesService _pagesService;

        public ManageRedirectsController(IRedirectsService redirectsService,
            IRedirectModelFactory redirectModelFactory,
            ILocalizationService localizationService,
            IProductService productService, 
            IBlogPostService blogPostService,
            IPagesService pagesService)
        {
            _redirectsService = redirectsService;
            _redirectModelFactory = redirectModelFactory;
            _localizationService = localizationService;
            _productService = productService;
            _blogPostService = blogPostService;
            _pagesService = pagesService;
        }

        public virtual ActionResult Index()
        {
            return View();
        }

        public virtual ActionResult Grid(RedirectRuleGroup? redirectGroup, int? entityId)
        {
            if (redirectGroup != null && entityId != null)
            {
                ViewBag.RedirectGroup = redirectGroup;
                ViewBag.EntityId = entityId;
                ViewBag.DataSource = Url.Action("GridDataSource", new { redirectGroup = redirectGroup , entityId = entityId });
            }
            else
            {
                ViewBag.DataSource = Url.Action("GridDataSource");
            }
            return PartialView();
        }

        public virtual async Task<ActionResult> Editor(int? id)
        {
            if (id != null)
            {
                var record = await _redirectsService.FindByIdAsync(id.Value);
                if (record != null)
                {
                    return View(_redirectModelFactory.PrepareRedirectModel(record));
                }
            }

            return View(_redirectModelFactory.PrepareRedirectModel(null));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public virtual async Task<ActionResult> Editor(RedirectModel model, bool? saveAndContinue)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var record = _redirectModelFactory.PrepareTblRedirects(model);
            var recordId = model.Id;
            try
            {
                if (model.Id == null)
                {
                    //Add new record
                    recordId = await _redirectsService.AddAsync(record);
                }
                else
                {
                    //Edit record
                    await _redirectsService.UpdateAsync(record);
                }
            }
            catch (Exception e)
            {
                var errorCode = ErrorLog.GetDefault(System.Web.HttpContext.Current).Log(new Error(e, System.Web.HttpContext.Current));
                ModelState.AddModelError("", string.Format(_localizationService.GetResource("ErrorOnOperation"), e.Message, errorCode));
                return View(model);
            }

            if (saveAndContinue != null && saveAndContinue.Value)
            {
                return RedirectToAction("Editor", "ManageRedirects", new { id = recordId });
            }

            return Content(@"<script language='javascript' type='text/javascript'>
                                window.close();
                                window.opener.refreshRedirectsGrid();
                             </script>");
        }

        [HttpPost]
        public virtual async Task<ActionResult> Delete(int[] keys)
        {
            try
            {
                foreach (var key in keys)
                    await _redirectsService.DeleteAsync(key);
                return Content("OK");
            }
            catch (Exception e)
            {
                var errorCode = ErrorLog.GetDefault(System.Web.HttpContext.Current).Log(new Error(e, System.Web.HttpContext.Current));
                return Content(string.Format(_localizationService.GetResource("ErrorOnOperation"), e.Message, errorCode));
            }
        }
        
        public virtual async Task<ActionResult> EntitySlugEditor(int? ruleId, RedirectRuleGroup redirectGroup, int entityId)
        {
            RedirectModel model;
            if (ruleId != null)
            {
                var record = await _redirectsService.FindByIdAsync(ruleId.Value);
                model = _redirectModelFactory.PrepareRedirectModel(record);
            }
            else
            {
                model = _redirectModelFactory.PrepareRedirectModel(null);

                model.MatchType = MatchType.Wildcards;
                model.ResponseType = ResponseType.Redirect;
                model.RequestedUrl = "*/";
                model.RedirectStatus = RedirectStatusCode.MovedPermanently;

                switch (redirectGroup)
                {
                    case RedirectRuleGroup.None:
                        break;
                    case RedirectRuleGroup.Product:
                        var product = await _productService.FindByIdAsync(entityId);
                        model.ResponseUrl = await RemoveLanguageFromUrl(Url.Action("Index", "Product", new { slug = product.Slug, area = "" }));
                        break;
                    case RedirectRuleGroup.BlogPost:
                        var blogPost = await _blogPostService.FindByIdAsync(entityId);
                        model.ResponseUrl = await RemoveLanguageFromUrl(Url.Action("Post", "Blog", new { slug = blogPost.Slug, area = "" }));
                        break;
                    case RedirectRuleGroup.Page:
                        var page = await _pagesService.FindByIdAsync(entityId);
                        model.ResponseUrl = await RemoveLanguageFromUrl(Url.Action("Index", "Page", new { slug = page.Slug, area = "" }));
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            model.EntityId = entityId;
            model.RedirectGroup = redirectGroup;

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public virtual async Task<ActionResult> EntitySlugEditor(RedirectModel model)
        {
            model.Active = true;
            model.StopProcessingOfSubsequentRules = false;
            model.AppendQueryString = true;
            model.IgnoreCase = true;
            model.Order = 0;
            model.Name = model.RequestedUrl;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var record = _redirectModelFactory.PrepareTblRedirects(model);

            try
            {
                if (model.Id == null)
                {
                    //Add new record
                    await _redirectsService.AddAsync(record);
                }
                else
                {
                    //Edit record
                    await _redirectsService.UpdateAsync(record);
                }
            }
            catch (Exception e)
            {
                var errorCode = ErrorLog.GetDefault(System.Web.HttpContext.Current).Log(new Error(e, System.Web.HttpContext.Current));
                ModelState.AddModelError("", string.Format(_localizationService.GetResource("ErrorOnOperation"), e.Message, errorCode));
                return View(model);
            }

            return Content($@"<script language='javascript' type='text/javascript'>
                                window.close();
                                window.opener.refreshRedirectsGrid();
                             </script>");
        }

        protected virtual async Task<string> RemoveLanguageFromUrl(string url)
        {
            var isoList = await LanguagesService.GetAllLanguagesIsoListAsync();
            
            foreach (var lang in isoList)
            {
                if (url.StartsWith($"/{lang}/", StringComparison.InvariantCultureIgnoreCase))
                {
                    url = url.Remove(0, $"/{lang}/".Length);
                }
            }

            if (!url.StartsWith("/"))
            {
                url = "/" + url;
            }

            return url;
        }

        public virtual ActionResult GridDataSource(DataManager dm, RedirectRuleGroup? redirectGroup, int? entityId)
        {
            var query = _redirectsService.GetAsQueryable();
            if (redirectGroup != null && entityId != null)
            {
                query = query.Where(p => p.EntityId == entityId && p.RedirectGroup == redirectGroup)
                    .OrderBy(p => p.Order);
            }
            else
            {
                query = query.Where(p => p.EntityId == null && p.RedirectGroup == RedirectRuleGroup.None);
            }

            var dataSource = query.Select(p => new
            {
                p.Id,
                p.Name,
                p.RequestedUrl,
                p.ResponseUrl,
                p.IgnoreCase,
                p.AppendQueryString,
                p.StopProcessingOfSubsequentRules,
                p.Order,
                p.Active,
                MatchType = p.MatchType + "",
                ResponseType = p.ResponseType + "",
                RedirectStatus = p.RedirectStatus == null ? "-" : p.RedirectStatus + "",
            });

            var result = dataSource.ApplyDataManager(dm, out var count).ToList();
            return Json(dm.RequiresCounts ? new { result = result, count = count } : (object)result,
                JsonRequestBehavior.AllowGet);
        }
    }
}