﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Oracle.ManagedDataAccess.Client;
using twitter_dotNetCoreWithVue.Controllers.Utils;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace twitter_dotNetCoreWithVue.Controllers
{
    /// <summary>
    /// 有关评论推特的api
    /// </summary>
    [Route("api/[controller]")]
    public class CommentController : Controller
    {
        //用于评论推特时的模型
        public class CommentForSender
        {
            [Display(Name = "评论内容")]
            [Required]
            [StringLength(280)]
            public string comment_content { set; get; }

        }

        /// <summary>
        /// 给某个推特添加评论时调用
        /// </summary>
        /// <returns>是否成功</returns>
        /// <param name="message_id">Message identifier.</param>
        /// <param name="comment">Comment.</param>
        [HttpPost("add/{message_id}")]
        public IActionResult Add([Required]int message_id, [Required][FromBody]CommentForSender comment)
        {
            //TODO 需要身份验证
            //返回是否成功
            int my_user_id = -1;
            if (HttpContext.User.Identity.IsAuthenticated)
            {
                my_user_id = int.Parse(HttpContext.User.Claims.ElementAt(0).Value);
            }
            else
            {
                //进入到这部分意味着用户登录态已经失效，需要返回给客户端信息，即需要登录。
                RestfulResult.RestfulData rr = new RestfulResult.RestfulData();
                rr.Code = 200;
                rr.Message = "Need Authentication";
                return new JsonResult(rr);
            }

            return Wrapper.wrap((OracleConnection conn)=> {
                //FUNC_ADD_COMMENT(user_id in INTEGER, message_id in INTEGER, content in VARCHAR2(255))
                //return INTEGER
                string procudureName = "FUNC_ADD_COMMENT";
                OracleCommand cmd = new OracleCommand(procudureName, conn);
                cmd.CommandType = CommandType.StoredProcedure;

                //Add return value
                OracleParameter p1 = new OracleParameter();
                p1 = cmd.Parameters.Add("state", OracleDbType.Int32);
                p1.Direction = ParameterDirection.ReturnValue;
                //Add input parameter user_id
                OracleParameter p2 = new OracleParameter();
                p2 = cmd.Parameters.Add("user_id", OracleDbType.Int32);
                p2.Direction = ParameterDirection.Input;
                p2.Value = my_user_id;
                OracleParameter p3 = new OracleParameter();
                //Add input parameter message_id
                p3 = cmd.Parameters.Add("message_id", OracleDbType.Int32);
                p3.Value = message_id;
                p3.Direction = ParameterDirection.Input;
                OracleParameter p4 = new OracleParameter();
                //Add input parameter content
                p4 = cmd.Parameters.Add("content", OracleDbType.Varchar2);
                p4.Value = comment.comment_content;
                p4.Direction = ParameterDirection.Input;

                cmd.ExecuteReader();
                Console.WriteLine(p1.Value);

                if (int.Parse(p1.Value.ToString()) != 1)
                {
                    throw new Exception("failed");
                }

                RestfulResult.RestfulData rr = new RestfulResult.RestfulData(200, "success");
                return new JsonResult(rr);
            });
        }

    }
}
