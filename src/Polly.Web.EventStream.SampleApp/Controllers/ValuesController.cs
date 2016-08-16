using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Polly.Configuration;

namespace Polly.Web.EventStream.SampleApp.Controllers
{
    [Route("api/[controller]")]
    public class ValuesController : Controller
    {
        private readonly IPolicyService _policyService;

        public ValuesController(IPolicyService policyService)
        {
            _policyService = policyService;
        }

        // GET api/values
        /// <summary>
        /// Gets this instance.
        /// </summary>
        /// <returns>IEnumerable&lt;System.String&gt;.</returns>
        [HttpGet]
        public IEnumerable<string> Get()
        {
            var policy = _policyService.GetPolicy("/api/values");
            return policy.Execute<IEnumerable<string>>(
                    () => new string[] { "value1", "value2" }
                );
        }

        // GET api/values/5
        [HttpGet("{id}")]
        public async Task<string> Get(int id)
        {
            var policy = _policyService.GetPolicy("/api/values");
            return await policy.ExecuteAsync<string>(
                    async (cancellationToken) => {
                        if (id == 101) throw new ArgumentNullException();
                        if (id == 102) await Task.Delay(8000, cancellationToken);
                        await Task.Delay(id * 100, cancellationToken);
                        return await Task.FromResult($"value{id}");
                    }
                );
        }

        // POST api/values
        [HttpPost]
        public void Post([FromBody]string value)
        {
        }

        // PUT api/values/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody]string value)
        {
        }

        // DELETE api/values/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
