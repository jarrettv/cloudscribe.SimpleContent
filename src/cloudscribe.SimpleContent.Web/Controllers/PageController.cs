﻿// Copyright (c) Source Tree Solutions, LLC. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
// Author:                  Joe Audette
// Created:                 2016-02-24
// Last Modified:           2016-09-11
// 

using cloudscribe.SimpleContent.Models;
using cloudscribe.SimpleContent.Web.ViewModels;
using cloudscribe.Web.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace cloudscribe.SimpleContent.Web.Controllers
{
    public class PageController : Controller
    {
        public PageController(
            IProjectService projectService,
            IPageService blogService,
            IAuthorizationService authorizationService,
            ITimeZoneHelper timeZoneHelper,
            ILogger<PageController> logger)
        {
            this.projectService = projectService;
            this.pageService = blogService;
            this.authorizationService = authorizationService;
            this.timeZoneHelper = timeZoneHelper;
            log = logger;
        }

        private IProjectService projectService;
        private IPageService pageService;
        private IAuthorizationService authorizationService;
        private ITimeZoneHelper timeZoneHelper;
        private ILogger log;

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Index(
            string slug = "", 
            string mode = "")
        {
            var projectSettings = await projectService.GetCurrentProjectSettings();

            if (projectSettings == null)
            {
                HttpContext.Response.StatusCode = 404;
                return NotFound();
            }

            if(slug == "none") { slug = string.Empty; }
            
            var canEdit = await User.CanEditPages(projectSettings.Id, authorizationService);
            var isNew = canEdit && (mode == "new");
            var isEditing = canEdit && (mode == "edit");
            if(!isNew && string.IsNullOrEmpty(slug)) { slug = projectSettings.DefaultPageSlug; }

            IPage page = null;
            if(!string.IsNullOrEmpty(slug))
            {
                page = await pageService.GetPageBySlug(projectSettings.Id, slug);
                
            }
            
            var model = new PageViewModel();

            if (page == null)
            {
                if (isNew)
                {
                    page = new Page();
                    page.ProjectId = projectSettings.Id;
                }
                else
                {
                    var rootList = await pageService.GetRootPages().ConfigureAwait(false);
                    // a site starts out with no pages 
                    if (canEdit && rootList.Count == 0)
                    {
                        page = new Page();
                        page.ProjectId = projectSettings.Id;
                        mode = "new";
                    }
                    else
                    {
                        
                        if(rootList.Count > 0)
                        {
                            Response.StatusCode = 404;
                            // return View("NotFound", 404);
                            return NotFound();
                        }
                        else
                        {
                            Response.StatusCode = 404;
                            return View("NoPages", 404);
                        }    
                    } 
                }
            }
            else
            {
                // if the page is protected by view roles return 404 if user is not in an allowed role
                if((!canEdit) && (!string.IsNullOrEmpty(page.ViewRoles)))
                {
                    if(!User.IsInRoles(page.ViewRoles))
                    {
                        Response.StatusCode = 404;
                        //return View("NotFound", 404);
                        return NotFound();
                    }
                }

                ViewData["Title"] = page.Title;
            }

            model.Mode = mode;
            model.CurrentPage = page;
            model.ProjectSettings = projectSettings;
            model.CanEdit = canEdit;
            model.ShowComments = mode.Length == 0; // do we need this for a global disable
            //model.CommentsAreOpen = await blogService.CommentsAreOpen(post, canEdit);
            model.CommentsAreOpen = false;
            model.TimeZoneHelper = timeZoneHelper;
            model.TimeZoneId = model.ProjectSettings.TimeZoneId;

            if (canEdit)
            {
                if(model.CurrentPage != null)
                {
                    model.EditorSettings.CancelEditPath = Url.Action("Index", "Page", new { slug = model.CurrentPage.Slug });
                    model.EditorSettings.CurrentSlug = model.CurrentPage.Slug;
                    model.EditorSettings.IsPublished = model.CurrentPage.IsPublished;
                    model.EditorSettings.EditPath = Url.Action("Index", "Page", new { slug = model.CurrentPage.Slug, mode="edit"});
                    model.EditorSettings.SortOrder = model.CurrentPage.PageOrder;
                    model.EditorSettings.ParentSlug = model.CurrentPage.ParentSlug;
                    model.EditorSettings.ViewRoles = model.CurrentPage.ViewRoles;
                    model.EditorSettings.ShowHeading = model.CurrentPage.ShowHeading;
                }
                else
                {
                    model.EditorSettings.CancelEditPath = Url.Content("~/");
                    model.EditorSettings.EditPath = Url.Action("Index", "Page", new { slug="",  mode = "new" });
                }

                model.EditorSettings.EditMode = mode;
                model.EditorSettings.NewItemButtonText = "New Page";
                model.EditorSettings.IndexUrl = Url.Content("~/");
                model.EditorSettings.CategoryPath = Url.Action("Category", "Page"); // TODO: should we support categories on pages? this action doesn't exist right now
                model.EditorSettings.DeletePath = Url.Action("AjaxDelete", "Page");
                model.EditorSettings.SavePath = Url.Action("AjaxPost", "Page");
                model.EditorSettings.NewItemPath = Url.Action("Index", "Page", new { slug = "", mode = "new" });
                model.EditorSettings.ContentType = "Page";
                model.EditorSettings.SupportsCategories = false;
                model.EditorSettings.ProjectId = projectSettings.Id;

            }

            return View(model);
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task AjaxPost(PageEditViewModel model)
        {
            // disable status code page for ajax requests
            var statusCodePagesFeature = HttpContext.Features.Get<IStatusCodePagesFeature>();
            if (statusCodePagesFeature != null)
            {
                statusCodePagesFeature.Enabled = false;
            }

            if (string.IsNullOrEmpty(model.Title))
            {
                // if a page has been configured to not show the title
                // this may be null on edit, if it is a new page then it should be required
                // because it is used for generating the slug
                //if (string.IsNullOrEmpty(model.Slug))
                //{
                    log.LogInformation("returning 500 because no title was posted");
                    Response.StatusCode = 500;
                    return;
                //}
                
            }

            var project = await projectService.GetCurrentProjectSettings();

            if (project == null)
            {
                log.LogInformation("returning 500 blog not found");
                Response.StatusCode = 500;
                return;
            }

            var canEdit = await User.CanEditPages(project.Id, authorizationService);
            
            if (!canEdit)
            {
                log.LogInformation("returning 403 user is not allowed to edit");
                Response.StatusCode = 403;
                return;
            }

            //string[] categories = new string[0];
            //if (!string.IsNullOrEmpty(model.Categories))
            //{
            //    categories = model.Categories.Split(new char[] { ',' },
            //    StringSplitOptions.RemoveEmptyEntries);
            //}


            IPage page = null;
            if (!string.IsNullOrEmpty(model.Id))
            {
                page = await pageService.GetPage(model.Id);
            }

            var needToClearCache = false;
            var isNew = false;
            if (page != null)
            {
                page.Title = model.Title;
                page.MetaDescription = model.MetaDescription;
                page.Content = model.Content;
                if (page.PageOrder != model.PageOrder) needToClearCache = true;
               
            }
            else
            {
                isNew = true;
                needToClearCache = true;
                var slug = ContentUtils.CreateSlug(model.Title);
                var available = await pageService.SlugIsAvailable(project.Id, slug);
                if (!available)
                {
                    log.LogInformation("returning 409 because slug already in use");
                    Response.StatusCode = 409;
                    return;
                }

                page = new Page()
                {
                    ProjectId = project.Id,
                    Author = User.GetUserDisplayName(),
                    Title = model.Title,
                    MetaDescription = model.MetaDescription,
                    Content = model.Content,
                    Slug = slug,
                    ParentId = "0"
                    
                    //,Categories = categories.ToList()
                };
            }

            if(!string.IsNullOrEmpty(model.ParentSlug))
            {
                var parentPage = await pageService.GetPageBySlug(project.Id, model.ParentSlug);
                if (parentPage != null)
                {
                    if(parentPage.Id != page.ParentId)
                    {
                        page.ParentId = parentPage.Id;
                        page.ParentSlug = parentPage.Slug;
                        needToClearCache = true;
                    }
                    
                }
            }
            else
            {
                // empty means root level
                page.ParentSlug = string.Empty;
                page.ParentId = "0";
            }
            if(page.ViewRoles != model.ViewRoles)
            {
                needToClearCache = true;
            }
            page.ViewRoles = model.ViewRoles;

            page.PageOrder = model.PageOrder;
            page.IsPublished = model.IsPublished;
            page.ShowHeading = model.ShowHeading;
            if (!string.IsNullOrEmpty(model.PubDate))
            {
                var localTime = DateTime.Parse(model.PubDate);
                page.PubDate = timeZoneHelper.ConvertToUtc(localTime, project.TimeZoneId);
                
            }

            if(isNew)
            {
                await pageService.Create(page, model.IsPublished);
            }
            else
            {
                await pageService.Update(page, model.IsPublished);
            }

            
            if(needToClearCache)
            {
                pageService.ClearNavigationCache();
            }

            var url = Url.Action("Index", "Page", new { slug = page.Slug });
            await Response.WriteAsync(url);

        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task AjaxDelete(string id)
        {
            // disable status code page for ajax requests
            var statusCodePagesFeature = HttpContext.Features.Get<IStatusCodePagesFeature>();
            if (statusCodePagesFeature != null)
            {
                statusCodePagesFeature.Enabled = false;
            }

            var project = await projectService.GetCurrentProjectSettings();

            if (project == null)
            {
                log.LogInformation("returning 500 blog not found");
                Response.StatusCode = 500;
                return; // new EmptyResult();
            }

            var canEdit = await User.CanEditPages(project.Id, authorizationService);
            
            if (!canEdit)
            {
                log.LogInformation("returning 403 user is not allowed to edit");
                Response.StatusCode = 403;
                return; //new EmptyResult();
            }

            if (string.IsNullOrEmpty(id))
            {
                log.LogInformation("returning 404 postid not provided");
                Response.StatusCode = 404;
                return; //new EmptyResult();
            }

            var page = await pageService.GetPage(id);

            if (page == null)
            {
                log.LogInformation("returning 404 not found");
                Response.StatusCode = 404;
                return; //new EmptyResult();
            }

            await pageService.DeletePage(project.Id, page.Id);

            // TODO: clear the page tree cache

            Response.StatusCode = 200;
            return; //new EmptyResult();

        }


    }
}
