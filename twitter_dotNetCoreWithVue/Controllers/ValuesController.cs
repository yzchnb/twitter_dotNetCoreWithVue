﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace twitter_dotNetCoreWithVue.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    
    
    public class ValuesController : ControllerBase
    {
        // GET api/values
        /// <summary>
        /// 得到值
        /// </summary>
        /// <returns>string</returns>
        [HttpGet]
        public IActionResult Get()
        {
            return Redirect("/api/values/5");
        }

        // GET api/values/5
        [HttpGet("{id}", Name = "GetTodo")]
        public ActionResult<string> Get(int id)
        {
            return Ok(id);
        }

        // POST api/values
        [HttpPost]
        public void Post([FromBody] string value)
        {
        }

        // PUT api/values/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] string value)
        {
        }

        // DELETE api/values/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
