using Microsoft.Crm.Sdk.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Root16.Sprout.DependencyInjection;

public class StepRegistrationDependencyList : List<Type>
{
    public StepRegistrationDependencyList(params Type[] steps) : base(steps)
    {
    }
}
