﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace Redwood.Samples.BasicSamples.ViewModels
{
    public class Sample17ViewModel_A : Sample17ViewModel
    {


        public Sample17ViewModel_A()
        {
            HeaderText = "Sample 1";
        }

        public int Value { get; set; }

        public void IncreaseValue()
        {
            Value++;
        }

    }
}