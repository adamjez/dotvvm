﻿using Redwood.Framework.ViewModel;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Redwood.Samples.BasicSamples.ViewModels
{
    public class Sample18ViewModel : RedwoodViewModelBase
    {

        public Sample18ChildViewModel Child { get; set; }

        public Sample18ViewModel()
        {
            Child = new Sample18ChildViewModel();
        }

        public void Test()
        {

        }
    }

    public class Sample18ChildViewModel
    {
        [Required]
        public string Text { get; set; }
    }
}