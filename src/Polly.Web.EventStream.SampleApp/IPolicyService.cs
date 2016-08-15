using Microsoft.Extensions.Configuration;
using Polly.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Polly.Web.EventStream.SampleApp
{
    public interface IPolicyService
    {
        Policy GetPolicy(string name);
    }

    /// <summary>
    /// Class PolicyService.
    /// </summary>
    public class PolicyService : IPolicyService
    {
        private readonly IConfigurationRoot _config;

        /// <summary>
        /// Initializes a new instance of the <see cref="PolicyService"/> class.
        /// </summary>
        /// <param name="config">The configuration.</param>
        public PolicyService(IConfigurationRoot config)
        {
            _config = config;
        }

        /// <summary>
        /// Gets the policy.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>Policy.</returns>
        public Policy GetPolicy(string name)
        {
            return PolicyRegistry.Resolve(name, _config);
        }
    }
}
