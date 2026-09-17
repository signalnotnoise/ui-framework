using System;
using System.Collections.Generic;
using System.Text;

namespace UI_Framework.Checks
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public sealed class FrameworkTests
    {
        [TestMethod]
        public void ComponentChecks()
        {
            StaTestRunner.Run(() => global::ComponentChecks.Run());
        }

        [TestMethod]
        public void BindingChecks()
        {
            StaTestRunner.Run(() => global::BindingChecks.Run());
        }

        [TestMethod]
        public void OptimizationChecks()
        {
            StaTestRunner.Run(() => global::OptimizationChecks.Run());
        }
    }
}
