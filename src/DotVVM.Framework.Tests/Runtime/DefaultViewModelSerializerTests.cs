using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using Microsoft.Owin;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using DotVVM.Framework.Configuration;
using DotVVM.Framework.Controls.Infrastructure;
using DotVVM.Framework.Hosting;
using DotVVM.Framework.ResourceManagement;
using DotVVM.Framework.Runtime;
using DotVVM.Framework.Security;
using DotVVM.Framework.ViewModel;

namespace DotVVM.Framework.Tests.Runtime
{
    [TestClass]
    public class DefaultViewModelSerializerTests
    {
        private DotvvmConfiguration configuration;
        private DefaultViewModelSerializer serializer;
        private DotvvmRequestContext context;
        
        [TestInitialize]
        public void TestInit()
        {
            configuration = DotvvmConfiguration.CreateDefault();
            configuration.Security.SigningKey = Convert.FromBase64String("Uiq1FXs016lC6QaWIREB7H2P/sn4WrxkvFkqaIKpB27E7RPuMipsORgSgnT+zJmUu8zXNSJ4BdL73JEMRDiF6A1ScRNwGyDxDAVL3nkpNlGrSoLNM1xHnVzSbocLFDrdEiZD2e3uKujguycvWSNxYzjgMjXNsaqvCtMu/qRaEGc=");
            configuration.Security.EncryptionKey = Convert.FromBase64String("jNS9I3ZcxzsUSPYJSwzCOm/DEyKFNlBmDGo9wQ6nxKg=");

            serializer = new DefaultViewModelSerializer(new DefaultViewModelProtector());
            context = new DotvvmRequestContext()
            {
                Configuration = configuration,
                OwinContext = new Microsoft.Owin.Fakes.StubIOwinContext()
                {
                    RequestGet = () => new Microsoft.Owin.Fakes.StubIOwinRequest()
                    {
                        UriGet = () => new Uri("http://localhost:8628/Sample1"),
                        UserGet = () => new WindowsPrincipal(WindowsIdentity.GetAnonymous()),
                        HeadersGet = () => new HeaderDictionary(new Dictionary<string, string[]>())
                    }
                },
                ResourceManager = new ResourceManager(configuration),
                Presenter = configuration.RouteTable.GetDefaultPresenter()
            };
        }



        [TestMethod]
        public void DefaultViewModelSerializer_NoEncryptedValues()
        {
            var oldViewModel = new TestViewModel()
            {
                Property1 = "a",
                Property2 = 4,
                Property3 = new DateTime(2000, 1, 1),
                Property4 = new List<TestViewModel2>()
                {
                    new TestViewModel2() { PropertyA = "t", PropertyB = 15 },
                    new TestViewModel2() { PropertyA = "xxx", PropertyB = 16 }
                },
                Property5 = null
            };
            context.ViewModel = oldViewModel;
            serializer.BuildViewModel(context, new DotvvmView());
            var result = context.GetSerializedViewModel();
            result = UnwrapSerializedViewModel(result);
            result = WrapSerializedViewModel(result);

            var newViewModel = new TestViewModel();
            context.ViewModel = newViewModel;
            serializer.PopulateViewModel(context, new DotvvmView(), result);

            Assert.AreEqual(oldViewModel.Property1, newViewModel.Property1);
            Assert.AreEqual(oldViewModel.Property2, newViewModel.Property2);
            Assert.AreEqual(oldViewModel.Property3, newViewModel.Property3);
            Assert.AreEqual(oldViewModel.Property4[0].PropertyA, newViewModel.Property4[0].PropertyA);
            Assert.AreEqual(oldViewModel.Property4[0].PropertyB, newViewModel.Property4[0].PropertyB);
            Assert.AreEqual(oldViewModel.Property4[1].PropertyA, newViewModel.Property4[1].PropertyA);
            Assert.AreEqual(oldViewModel.Property4[1].PropertyB, newViewModel.Property4[1].PropertyB);
            Assert.AreEqual(oldViewModel.Property5, newViewModel.Property5);
        }


        public class TestViewModel
        {
            public string Property1 { get; set; }
            public int Property2 { get; set; }
            public DateTime Property3 { get; set; }
            public List<TestViewModel2> Property4 { get; set; }
            public string Property5 { get; set; }
        }
        public class TestViewModel2
        {
            public string PropertyA { get; set; }
            public int PropertyB { get; set; }
        }



        [TestMethod]
        public void DefaultViewModelSerializer_SignedAndEncryptedValue()
        {
            var oldViewModel = new TestViewModel3()
            {
                Property1 = "a",
                Property2 = 4,
                Property3 = new DateTime(2000, 1, 1),
                Property4 = new List<TestViewModel4>()
                {
                    new TestViewModel4() { PropertyA = "t", PropertyB = 15 },
                    new TestViewModel4() { PropertyA = "xxx", PropertyB = 16 }
                }
            };
            context.ViewModel = oldViewModel;

            serializer.BuildViewModel(context, new DotvvmView());
            var result = context.GetSerializedViewModel();
            result = UnwrapSerializedViewModel(result);
            result = WrapSerializedViewModel(result);

            var newViewModel = new TestViewModel3();
            context.ViewModel = newViewModel;
            serializer.PopulateViewModel(context, new DotvvmView(), result);

            Assert.AreEqual(oldViewModel.Property1, newViewModel.Property1);
            Assert.AreEqual(oldViewModel.Property2, newViewModel.Property2);
            Assert.AreEqual(oldViewModel.Property3, newViewModel.Property3);
            Assert.AreEqual(oldViewModel.Property4[0].PropertyA, newViewModel.Property4[0].PropertyA);
            Assert.AreEqual(oldViewModel.Property4[0].PropertyB, newViewModel.Property4[0].PropertyB);
            Assert.AreEqual(oldViewModel.Property4[1].PropertyA, newViewModel.Property4[1].PropertyA);
            Assert.AreEqual(oldViewModel.Property4[1].PropertyB, newViewModel.Property4[1].PropertyB);
        }


        public class TestViewModel3
        {
            public string Property1 { get; set; }

            [ViewModelProtection(ViewModelProtectionSettings.SignData)]
            public int Property2 { get; set; }

            [ViewModelProtection(ViewModelProtectionSettings.EnryptData)]
            public DateTime Property3 { get; set; }
            public List<TestViewModel4> Property4 { get; set; }
        }
        public class TestViewModel4
        {
            [ViewModelProtection(ViewModelProtectionSettings.SignData)]
            public string PropertyA { get; set; }

            [ViewModelProtection(ViewModelProtectionSettings.EnryptData)]
            public int PropertyB { get; set; }
        }



        [TestMethod]
        public void DefaultViewModelSerializer_Enum()
        {
            var oldViewModel = new EnumTestViewModel()
            {
                Property1 = TestEnum.Second
            };
            context.ViewModel = oldViewModel;
            serializer.BuildViewModel(context, new DotvvmView());
            var result = context.GetSerializedViewModel();
            result = UnwrapSerializedViewModel(result);
            result = WrapSerializedViewModel(result);

            var newViewModel = new EnumTestViewModel();
            context.ViewModel = newViewModel;
            serializer.PopulateViewModel(context, new DotvvmView(), result);

            Assert.IsFalse(result.Contains(typeof (TestEnum).FullName));
            Assert.AreEqual(oldViewModel.Property1, newViewModel.Property1);
        }

        public class EnumTestViewModel
        {
            public TestEnum Property1 { get; set; }
        }

        public enum TestEnum
        {
            First,
            Second,
            Third
        }

        /// <summary>
        /// Wraps the serialized view model to an object that comes from the client.
        /// </summary>
        private static string WrapSerializedViewModel(string result)
        {
            return string.Format("{{'currentPath':[],'command':'','controlUniqueId':'','viewModel':{0},'validationTargetPath':'','updatedControls':{{}}}}".Replace("'", "\""), result);
        }

        /// <summary>
        /// Unwraps the object that goes to the client to the serialized view model.
        /// </summary>
        private static string UnwrapSerializedViewModel(string result)
        {
            return JObject.Parse(result)["viewModel"].ToString();
        }

    }
}
