using Root16.Sprout.Step;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Root16.Sprout.Sample
{
    internal class TestStep : IIntegrationStep
    {
        public string Name { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public void Run(IIntegrationRuntime runtime)
        {
            throw new NotImplementedException();
        }
    }
}
