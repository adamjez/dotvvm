﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DotVVM.Testing.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium;


namespace DotVVM.Samples.Tests.Feature
{
    [TestClass]
    public class MasterPageTests : AppSeleniumTest
    {
        [TestMethod]
        [SampleReference(nameof(SamplesRouteUrls.FeatureSamples_NestedMasterPages_Content))]
        public void Feature_NestedMasterPages_Content_TwoNestedMasterPages()
        {
            RunInAllBrowsers(browser =>
            {
                browser.NavigateToUrl(SamplesRouteUrls.FeatureSamples_NestedMasterPages_Content);
                browser.First("h1"); // root masterpage
                browser.First("h2"); // nested masterpage
                browser.First("h3"); // nested page
            });
        }

    }
}
