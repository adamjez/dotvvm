﻿using System;
using System.Collections.Generic;
using System.Linq;
using Redwood.Framework.Configuration;
using Redwood.Framework.Controls;
using Redwood.Framework.Controls.Infrastructure;
using Redwood.Framework.Hosting;
using Redwood.Framework.Parser;
using Redwood.Framework.Runtime.Compilation;

namespace Redwood.Framework.Runtime
{
    /// <summary>
    /// Builds the Redwood view and resolves the master pages.
    /// </summary>
    public class DefaultRedwoodViewBuilder : IRedwoodViewBuilder
    {

        private IMarkupFileLoader markupFileLoader;

        private IControlBuilderFactory controlBuilderFactory;



        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultRedwoodViewBuilder"/> class.
        /// </summary>
        public DefaultRedwoodViewBuilder(RedwoodConfiguration configuration)
        {
            markupFileLoader = configuration.ServiceLocator.GetService<IMarkupFileLoader>();
            controlBuilderFactory = configuration.ServiceLocator.GetService<IControlBuilderFactory>();
        }

        /// <summary>
        /// Builds the <see cref="RedwoodView"/> for the specified HTTP request, resolves the master page hierarchy and performs the composition.
        /// </summary>
        public RedwoodView BuildView(RedwoodRequestContext context)
        {
            // get the page markup
            var markup = markupFileLoader.GetMarkupFileVirtualPath(context);

            // build the page
            var pageBuilder = controlBuilderFactory.GetControlBuilder(markup);
            var contentPage = pageBuilder.BuildControl(controlBuilderFactory) as RedwoodView;

            // check for master page and perform composition recursively
            while (IsNestedInMasterPage(contentPage))
            {
                // load master page
                var masterPageFile = contentPage.Directives[Constants.MasterPageDirective];
                var masterPage = (RedwoodView)controlBuilderFactory.GetControlBuilder(masterPageFile).BuildControl(controlBuilderFactory);

                PerformMasterPageComposition(contentPage, masterPage);
                contentPage = masterPage;
            }

            // verifies the SPA request
            VerifySpaRequest(context, contentPage);

            return contentPage;
        }

        /// <summary>
        /// If the request is SPA request, we need to verify that the page contains the same SpaContentPlaceHolder.
        /// Also we need to check that the placeholder is the same.
        /// </summary>
        private void VerifySpaRequest(RedwoodRequestContext context, RedwoodView page)
        {
            if (context.IsSpaRequest)
            {
                var spaContentPlaceHolders = page.GetAllDescendants().OfType<SpaContentPlaceHolder>().ToList();
                if (spaContentPlaceHolders.Count > 1)
                {
                    throw new Exception("Multiple controls of type <rw:SpaContentPlaceHolder /> found on the page! This control can be used only once!");   // TODO: exception handling
                }
                if (spaContentPlaceHolders.Count == 0 || spaContentPlaceHolders[0].GetSpaContentPlaceHolderUniqueId() != context.GetSpaContentPlaceHolderUniqueId())
                {
                    // the client has loaded different page which does not contain current SpaContentPlaceHolder - he needs to be redirected
                    context.Redirect(context.HttpContext.Request.GetAbsoluteUrl());
                }
            }
        }

        /// <summary>
        /// Determines whether the page is nested in master page.
        /// </summary>
        private bool IsNestedInMasterPage(RedwoodView page)
        {
            return page.Directives.ContainsKey(Constants.MasterPageDirective);
        }

        /// <summary>
        /// Performs the master page nesting.
        /// </summary>
        private void PerformMasterPageComposition(RedwoodView childPage, RedwoodView masterPage)
        {
            // find content place holders
            var placeHolders = GetMasterPageContentPlaceHolders(masterPage);

            // find contents
            var contents = GetChildPageContents(childPage, placeHolders);

            // perform the composition
            foreach (var content in contents)
            {
                // find the corresponding placeholder
                var placeHolder = placeHolders.SingleOrDefault(p => p.ID == content.ContentPlaceHolderID);
                if (placeHolder == null)
                {
                    throw new Exception(string.Format("The placeholder with ID '{0}' was not found in the master page!", content.ContentPlaceHolderID));
                }

                // replace the contents
                var children = content.Children.ToArray();
                placeHolder.Children.Clear();
                content.Children.Clear();
                foreach (var child in children)
                {
                    placeHolder.Children.Add(child);
                }
            }

            // copy the directives from content page to the master page (except the @masterpage)
            foreach (var directive in childPage.Directives)
            {
                if (directive.Key == Constants.MasterPageDirective)
                {
                    continue;
                }
                masterPage.Directives[directive.Key] = directive.Value;
            }
        }

        /// <summary>
        /// Gets the content place holders.
        /// </summary>
        private List<ContentPlaceHolder> GetMasterPageContentPlaceHolders(RedwoodControl masterPage)
        {
            var placeHolders = masterPage.GetAllDescendants().OfType<ContentPlaceHolder>().ToList();

            // check that no placeholder is nested in another one and that each one has valid ID
            foreach (var placeHolder in placeHolders)
            {
                placeHolder.EnsureControlHasId(autoGenerate: false);
                if (placeHolder.GetAllAncestors().Intersect(placeHolders).Any())
                {
                    throw new Exception(string.Format("The ContentPlaceHolder with ID '{0}' cannot be nested in another ContentPlaceHolder!", placeHolder.ID)); // TODO: exception handling
                }
            }
            return placeHolders;
        }

        /// <summary>
        /// Checks that the content page does not contain invalid content.
        /// </summary>
        private List<Content> GetChildPageContents(RedwoodView childPage, List<ContentPlaceHolder> parentPlaceHolders)
        {
            // make sure that the body contains only whitespace and Content controls
            if (!childPage.Children.All(c => (c is Literal && ((Literal)c).HasWhiteSpaceContentOnly()) || (c is Content)))
            {
                throw new Exception("If the page contains @masterpage directive, it can only contain white space and <rw:Content /> controls!");    // TODO: exception handling
            }

            // make sure that the Content controls are not nested in other elements
            var contents = childPage.GetAllDescendants().OfType<Content>().ToList();
            if (contents.Any(c => c.Parent != childPage))
            {
                throw new Exception("The control <rw:Content /> cannot be placed inside any control!");    // TODO: exception handling
            }

            return contents;
        }


    }
}