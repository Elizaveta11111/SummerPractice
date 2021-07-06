using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Serilog;

namespace WebApplication.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class MessageBrokerController : ControllerBase
    {
        private readonly Serilog.ILogger _logger;

        public MessageBrokerController(Serilog.ILogger logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public string SendMessage()
        {
            return EmitLogTopic.Send(_logger);
        }
    }
}