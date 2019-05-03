using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace SwishTestWebAppCore.Controllers.API
{
    [Route("api/[controller]")]
    [Produces("application/json")]
    [ApiController]
    public class SwishAPIController : ControllerBase
    {
        private Swish.SwishClient _client;
        private readonly IMemoryCache _memoryCache;

        public SwishAPIController(Swish.SwishClient client, IMemoryCache memoryCache)
        {
            _client = client;
            _memoryCache = memoryCache;
        }

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult> Post([FromBody] Swish.PaymentStatusModel value)
        {
            var status = await _client.GetPaymentStatus(value.Id);
            _memoryCache.Set(status.Id, status);
            return Ok();
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [HttpGet("{id}")]
        public async Task<ActionResult<Swish.PaymentStatusModel>> Get([FromRoute] string id)
        {
            Swish.PaymentStatusModel status;
            while (!_memoryCache.TryGetValue(id, out status))
            {
                await Task.Delay(500);
            }
            return Ok(status);
        }
    }
}
