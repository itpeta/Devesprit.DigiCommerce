﻿using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using Devesprit.Core.Localization;
using Devesprit.DigiCommerce.Areas.Admin.Factories.Interfaces;
using Devesprit.DigiCommerce.Areas.Admin.Models;
using Devesprit.DigiCommerce.Controllers;
using Devesprit.Services.Blog;
using Devesprit.Utilities.Extensions;
using Devesprit.WebFramework.Helpers;
using Elmah;
using Syncfusion.JavaScript;

namespace Devesprit.DigiCommerce.Areas.Admin.Controllers
{
    [Authorize(Roles = "Admin")]
    public partial class ManageBlogPostsController : BaseController
    {
        private readonly IBlogPostService _blogPostService;
        private readonly IAdminBlogPostModelFactory _adminBlogPostModelFactory;
        private readonly ILocalizationService _localizationService;
        private readonly ILocalizedEntityService _localizedEntityService;

        public ManageBlogPostsController(IBlogPostService blogPostService,
            IAdminBlogPostModelFactory adminBlogPostModelFactory,
            ILocalizationService localizationService,
            ILocalizedEntityService localizedEntityService)
        {
            _blogPostService = blogPostService;
            _adminBlogPostModelFactory = adminBlogPostModelFactory;
            _localizationService = localizationService;
            _localizedEntityService = localizedEntityService;
        }

        public virtual ActionResult Index()
        {
            return View();
        }


        public virtual ActionResult Grid(int? categoryId)
        {
            if (categoryId != null && categoryId > 0)
            {
                //Filter by category
                ViewBag.DataSource = Url.Action("GridDataSource", new { categoryId = categoryId });
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
                var record = await _blogPostService.FindByIdAsync(id.Value);
                if (record != null)
                {
                    return View(await _adminBlogPostModelFactory.PrepareBlogPostModelAsync(record));
                }
            }

            return View(await _adminBlogPostModelFactory.PrepareBlogPostModelAsync(null));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public virtual async Task<ActionResult> Editor(BlogPostModel model, bool? saveAndContinue)
        {
            if (!model.Slug.IsNormalizedUrl())
            {
                ModelState.AddModelError("Slug", string.Format(_localizationService.GetResource("InvalidFieldData"), _localizationService.GetResource("Slug")));
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var record = _adminBlogPostModelFactory.PrepareTblBlogPosts(model);
            var recordId = model.Id;
            try
            {
                if (model.Id == null)
                {
                    //Add new record
                    recordId = await _blogPostService.AddAsync(record);
                }
                else
                {
                    //Edit record
                    await _blogPostService.UpdateAsync(record);
                }

                await _localizedEntityService.SaveAllLocalizedStringsAsync(record, model);
            }
            catch (Exception e)
            {
                var errorCode = ErrorLog.GetDefault(System.Web.HttpContext.Current).Log(new Error(e, System.Web.HttpContext.Current));
                ModelState.AddModelError("", string.Format(_localizationService.GetResource("ErrorOnOperation"), e.Message, errorCode));
                return View(model);
            }

            if (saveAndContinue != null && saveAndContinue.Value)
            {
                return RedirectToAction("Editor", "ManageBlogPosts", new {id = recordId});
            }

            return Content(@"<script language='javascript' type='text/javascript'>
                                window.close();
                                window.opener.refreshBlogPostsGrid();
                             </script>");
        }

        [HttpPost]
        public virtual async Task<ActionResult> Delete(int[] keys)
        {
            try
            {
                foreach (var key in keys)
                    await _blogPostService.DeleteAsync(key);
                return Content("OK");
            }
            catch (Exception e)
            {
                var errorCode = ErrorLog.GetDefault(System.Web.HttpContext.Current).Log(new Error(e, System.Web.HttpContext.Current));
                return Content(string.Format(_localizationService.GetResource("ErrorOnOperation"), e.Message, errorCode));
            }
        }

        public virtual ActionResult GridDataSource(DataManager dm, int? categoryId)
        {
            var query = _blogPostService.GetAsQueryable();
            if (categoryId != null && categoryId > 0)
            {
                query = query.Where(p => p.Categories.Any(x => x.Id == categoryId));
            }
            var postUrl =  Url.Action("Post", "Blog", new { area = "" });
            var dataSource = query.Select(p => new
            {
                p.Id,
                p.IsFeatured,
                LastUpDate = p.LastUpDate ?? p.PublishDate,
                p.PinToTop,
                p.Title,
                p.PublishDate,
                p.ShowInHotList,
                p.NumberOfViews,
                p.Published,
                p.AllowCustomerReviews,
                p.PageTitle,
                p.Slug,
                PostUrl = "<a target='_blank' href='" + postUrl + "/" + p.Slug + "'>" + p.Title + "</a>"
            });

            var result = dataSource.ApplyDataManager(dm, out var count).ToList();
            return Json(dm.RequiresCounts ? new { result = result, count = count } : (object)result,
                JsonRequestBehavior.AllowGet);
        }
    }
}