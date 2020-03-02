using System;
using Microsoft.AspNetCore.Mvc;

namespace FlexFlow.Api.Controllers
{
    /// <summary>
    /// Base class for FlexFlow API controllers. Adds the [ApiController] attribute so that model validation is
    /// automatic, resulting in less boilerplate.
    /// </summary>
    [ApiController]
    public class FlexFlowControllerBase : ControllerBase
    {
    }
}
