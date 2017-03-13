using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
namespace HashProof.Core.Services
{
    public static class DependencyResolver
    {
        public static IServiceProvider Provider { get; set; }
    }
}
