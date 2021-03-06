﻿using System;
using System.Data;
using System.IO;
using System.Data.Common;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using Oracle.ManagedDataAccess.Client;
using twitter_dotNetCoreWithVue.Controllers.Utils;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace twitter_dotNetCoreWithVue.Controllers
{
    /// <summary>
    /// 定义有关推特信息的api
    /// 包括发送，转发，删除，查询
    /// 以及点赞，评论。
    /// 以及根据话题Topic来查询
    /// </summary>
    [Route("api/[controller]")]
    public class MessageController : Controller
    {
        //用于展示推特时的模型
        public class MessageForShow
        {
            [Display(Name = "推特ID")]
            public int message_id { get; set; }

            [Display(Name = "推特内容")]
            [Required]
            [StringLength(280)]
            public string message_content { get; set; }

            [Display(Name = "推特所含话题")]
            public TopicController.TopicInfos[] message_topics { get; set; }

            [Display(Name = "推特所含艾特")]
            public AtController.AtInfos[] message_ats { get; set; }

            [Display(Name = "推特发布时间")]
            [Required]
            public string message_create_time { get; set; }

            [Display(Name = "点赞量")]
            public int message_like_num { get; set; }

            [Display(Name = "转发量")]
            public int message_transpond_num { get; set; }

            [Display(Name = "评论量")]
            public int message_comment_num { get; set; }

            [Display(Name = "浏览量")]
            public int message_view_num { get; set; }

            [Display(Name = "推特是否带图")]
            [Required]
            public int message_has_image { get; set; }

            [Display(Name = "推特是否为转发")]
            [Required]
            public int message_is_transpond { get; set; }

            [Display(Name = "发布人ID")]
            public int message_sender_user_id { get; set; }

            [Display(Name = "推特热度")]
            public int message_heat { get; set; }

            [Display(Name = "转发来源推特ID")]
            public int message_transpond_message_id { get; set; }

            [Display(Name = "推特含图数量")]
            public int message_image_count { get; set; }

            [Display(Name = "图片url列表")]
            public string[] message_image_urls { get; set; }

        }
        //用于发送推特时的模型
        public class MessageForSender
        {

            [Display(Name = "推特内容")]
            [Required]
            [StringLength(280)]
            public string message_content { get; set; }

            [Display(Name = "推特是否带图")]
            [Required]
            public int message_has_image { get; set; }

            [Display(Name = "推特含图数量")]
            public int message_image_count { get; set; }

        }
        //用于转发推特时的模型
        public class MessageForTransponder
        {
            [Display(Name = "推特内容")]
            [Required]
            [StringLength(280)]
            public string message_content { get; set; }

            [Display(Name = "转发来源推特ID")]
            public int message_transpond_message_id { get; set; }

        }


        //推特中包含的艾特类
        public class AtInfos
        {
            public List<string> atList = new List<string>();
            public List<int> atIds = new List<int>();
        }



        /// <summary>
        /// 查看推特详情时调用的api
        /// 前端需要根据是否转发来改变视图的具体类型
        /// 为减轻前端工作量，我们把推特含有的图片列表同时传送过去
        /// </summary>
        /// <returns>The message.</returns>
        /// <param name="message_id">Message identifier.</param>
        [HttpPost("query")]
        public async Task<IActionResult> Query([Required]int message_id)
        {
            //获得推特的详细信息
            //无需验证登录态
            //除了基本的推特信息以外，我们需要根据这条推特是否含有图，来把MessageForShow的图片url列表填好

            return await Wrapper.wrap(async (OracleConnection conn) =>
            {
                //function FUNC_SHOW_MESSAGE_BY_ID(message_id in INTEGER, result out sys_refcursor)
                //return INTEGER
                string procedurename = "FUNC_SHOW_MESSAGE_BY_ID";
                OracleCommand cmd = new OracleCommand(procedurename, conn);
                cmd.CommandType = CommandType.StoredProcedure;

                //Add return value
                OracleParameter p1 = new OracleParameter();
                p1 = cmd.Parameters.Add("state", OracleDbType.Int32);
                p1.Direction = ParameterDirection.ReturnValue;

                //Add first parameter message_id
                OracleParameter p2 = new OracleParameter();
                p2 = cmd.Parameters.Add("message_id", OracleDbType.Int32);
                p2.Direction = ParameterDirection.Input;
                p2.Value = message_id;

                //Add second parameter search_result
                OracleParameter p3 = new OracleParameter();
                p3 = cmd.Parameters.Add("result", OracleDbType.RefCursor);
                p3.Direction = ParameterDirection.Output;

                //Get the result table
                OracleDataAdapter DataAdapter = new OracleDataAdapter(cmd);
                DataTable dt = new DataTable();
                await Task.FromResult(DataAdapter.Fill(dt));

                if (int.Parse(p1.Value.ToString()) == 0)
                {
                    throw new Exception("failed");
                }
                MessageForShow infos = new MessageForShow();
                infos.message_id = int.Parse(dt.Rows[0][0].ToString());
                infos.message_content = dt.Rows[0][1].ToString();
                infos.message_create_time = dt.Rows[0][2].ToString();
                infos.message_like_num = int.Parse(dt.Rows[0][3].ToString());
                infos.message_transpond_num = int.Parse(dt.Rows[0][4].ToString());
                infos.message_comment_num = int.Parse(dt.Rows[0][5].ToString());
                infos.message_view_num = int.Parse(dt.Rows[0][6].ToString());
                infos.message_has_image = int.Parse(dt.Rows[0][7].ToString());
                infos.message_sender_user_id = int.Parse(dt.Rows[0][8].ToString());
                infos.message_heat = int.Parse(dt.Rows[0][9].ToString());
                infos.message_image_count = int.Parse(dt.Rows[0][10].ToString() == "" ? "0" : dt.Rows[0][10].ToString());
                infos.message_transpond_message_id = int.Parse(dt.Rows[0][11].ToString() == "" ? "0" : dt.Rows[0][11].ToString());

                infos.message_topics = await TopicController.SearchTopicsInTwitter(infos.message_content);
                infos.message_ats = await AtController.SearchAtsInTwitter(infos.message_content);

                if (infos.message_has_image == 1)
                {
                    string path = @"wwwroot\Messages\" + infos.message_id.ToString() + @"\";
                    infos.message_image_urls = getMessageImageUrls(infos.message_id, infos.message_image_count);
                }

                RestfulResult.RestfulData<MessageForShow> rr = new RestfulResult.RestfulData<MessageForShow>();
                rr.Code = 200;
                rr.Message = "success";
                rr.Data = infos;
                return new JsonResult(rr);

            });


        }

        //内部调用的，根据ID查询返回MessageForShow类型的函数
        static public async Task<MessageForShow> InnerQuery(int message_id)
        {
            return await Wrapper.wrap(async (OracleConnection conn) =>
            {
                //function FUNC_SHOW_MESSAGE_BY_ID(message_id in INTEGER, result out sys_refcursor)
                //return INTEGER
                string procedurename = "FUNC_SHOW_MESSAGE_BY_ID";
                OracleCommand cmd = new OracleCommand(procedurename, conn);
                cmd.CommandType = CommandType.StoredProcedure;

                //Add return value
                OracleParameter p1 = new OracleParameter();
                p1 = cmd.Parameters.Add("state", OracleDbType.Int32);
                p1.Direction = ParameterDirection.ReturnValue;

                //Add first parameter message_id
                OracleParameter p2 = new OracleParameter();
                p2 = cmd.Parameters.Add("message_id", OracleDbType.Int32);
                p2.Direction = ParameterDirection.Input;
                p2.Value = message_id;

                //Add second parameter search_result
                OracleParameter p3 = new OracleParameter();
                p3 = cmd.Parameters.Add("result", OracleDbType.RefCursor);
                p3.Direction = ParameterDirection.Output;

                //Get the result table
                OracleDataAdapter DataAdapter = new OracleDataAdapter(cmd);
                DataTable dt = new DataTable();
                await Task.FromResult(DataAdapter.Fill(dt));

                if (int.Parse(p1.Value.ToString()) == 0)
                {
                    throw new Exception("failed");
                }
                MessageForShow infos = new MessageForShow();
                infos.message_id = int.Parse(dt.Rows[0][0].ToString());
                infos.message_content = dt.Rows[0][1].ToString();
                infos.message_create_time = dt.Rows[0][2].ToString();
                infos.message_like_num = int.Parse(dt.Rows[0][3].ToString());
                infos.message_transpond_num = int.Parse(dt.Rows[0][4].ToString());
                infos.message_comment_num = int.Parse(dt.Rows[0][5].ToString());
                infos.message_view_num = int.Parse(dt.Rows[0][6].ToString());
                infos.message_has_image = int.Parse(dt.Rows[0][7].ToString());
                infos.message_sender_user_id = int.Parse(dt.Rows[0][8].ToString());
                infos.message_heat = int.Parse(dt.Rows[0][9].ToString());
                infos.message_image_count = int.Parse(dt.Rows[0][10].ToString() == "" ? "0" : dt.Rows[0][10].ToString());
                infos.message_transpond_message_id = int.Parse(dt.Rows[0][11].ToString() == "" ? "0" : dt.Rows[0][11].ToString());

                infos.message_topics = await TopicController.SearchTopicsInTwitter(infos.message_content);
                infos.message_ats = await AtController.SearchAtsInTwitter(infos.message_content);
                infos.message_image_urls = new string[infos.message_image_count];
                for(int i = 0; i < infos.message_image_count; i++)
                {
                    infos.message_image_urls = getMessageImageUrls(infos.message_id, infos.message_image_count);
                }

                return infos;

            });
        }


        /// <summary>
        /// 此api用于首页，需要查找所有的显示在首页的推特时调用
        /// 根据range来返回前几条推荐的信息
        /// 实际上我们返回的是自己的以及关注者的推特
        /// 按照时间排序
        /// </summary>
        /// <returns>The messages for index.</returns>
        /// <param name="range">Range.</param>
        /// <param name="user_id">user_id</param>
        [HttpPost("queryMessage/{user_id}")]
        public async Task<IActionResult> QueryMessages([Required]int user_id, [Required][FromBody]Range range)
        {
            return await Wrapper.wrap(async (OracleConnection conn) =>
            {
                //function FUNC_SHOW_HOME_MESSAGE_BY_RANGE(user_id in INTEGER, rangeStart in INTEGER, rangeLimitation in INTEGER, search_result out sys_refcursor)
                //return INTEGER
                string procedurename = "FUNC_SHOW_MESSAGE_BY_RANGE";
                OracleCommand cmd = new OracleCommand(procedurename, conn);
                cmd.CommandType = CommandType.StoredProcedure;

                //Add return value
                OracleParameter p1 = new OracleParameter();
                p1 = cmd.Parameters.Add("state", OracleDbType.Int32);
                p1.Direction = ParameterDirection.ReturnValue;

                //Add first parameter user_id
                OracleParameter p2 = new OracleParameter();
                p2 = cmd.Parameters.Add("user_id", OracleDbType.Int32);
                p2.Direction = ParameterDirection.Input;
                p2.Value = user_id;

                //Add second parameter rangeStart
                OracleParameter p3 = new OracleParameter();
                p3 = cmd.Parameters.Add("rangeStart", OracleDbType.Int32);
                p3.Direction = ParameterDirection.Input;
                p3.Value = range.startFrom;

                //Add third parameter rangeLimitation
                OracleParameter p4 = new OracleParameter();
                p4 = cmd.Parameters.Add("rangeLimitation", OracleDbType.Int32);
                p4.Direction = ParameterDirection.Input;
                p4.Value = range.limitation;

                //Add fourth parameter search_result
                OracleParameter p5 = new OracleParameter();
                p5 = cmd.Parameters.Add("result", OracleDbType.RefCursor);
                p5.Direction = ParameterDirection.Output;

                //Get the result table
                OracleDataAdapter DataAdapter = new OracleDataAdapter(cmd);
                DataTable dt = new DataTable();
                await Task.FromResult(DataAdapter.Fill(dt));

                if (int.Parse(p1.Value.ToString()) == 0)
                {
                    throw new Exception("failed");
                }
                MessageForShow[] receivedTwitters = new MessageForShow[dt.Rows.Count];

                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    receivedTwitters[i] = new MessageForShow();
                    receivedTwitters[i].message_id = int.Parse(dt.Rows[i][0].ToString());
                    receivedTwitters[i].message_content = dt.Rows[i][1].ToString();
                    receivedTwitters[i].message_create_time = dt.Rows[i][2].ToString();
                    receivedTwitters[i].message_like_num = int.Parse(dt.Rows[i][3].ToString());
                    receivedTwitters[i].message_transpond_num = int.Parse(dt.Rows[i][4].ToString());
                    receivedTwitters[i].message_comment_num = int.Parse(dt.Rows[i][5].ToString());
                    receivedTwitters[i].message_view_num = int.Parse(dt.Rows[i][6].ToString());
                    receivedTwitters[i].message_has_image = int.Parse(dt.Rows[i][7].ToString());
                    receivedTwitters[i].message_sender_user_id = int.Parse(dt.Rows[i][8].ToString());
                    receivedTwitters[i].message_heat = int.Parse(dt.Rows[i][9].ToString());
                    receivedTwitters[i].message_image_count = int.Parse(dt.Rows[i][10].ToString() == "" ? "0" : dt.Rows[i][10].ToString());
                    receivedTwitters[i].message_transpond_message_id = int.Parse(dt.Rows[i][11].ToString() == "" ? "0" : dt.Rows[i][11].ToString());

                    receivedTwitters[i].message_topics = await TopicController.SearchTopicsInTwitter(receivedTwitters[i].message_content);
                    receivedTwitters[i].message_ats = await AtController.SearchAtsInTwitter(receivedTwitters[i].message_content);

                }
                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    if (receivedTwitters[i].message_has_image == 1)
                    {
                        string path = @"wwwroot\Messages\" + receivedTwitters[i].message_id.ToString() + @"\";
                        receivedTwitters[i].message_image_urls =
                        getMessageImageUrls(receivedTwitters[i].message_id, receivedTwitters[i].message_image_count);

                    }
                }

                RestfulResult.RestfulArray<MessageForShow> rr = new RestfulResult.RestfulArray<MessageForShow>();
                rr.Code = 200;
                rr.Message = "success";
                rr.Data = receivedTwitters;
                return new JsonResult(rr);
            });

        }

        /// <summary>
        /// 根据range来返回前几条推荐的信息
        /// 返回的是最新的range条消息
        /// 按照时间排序
        /// </summary>
        /// <returns>The messages for index.</returns>
        /// <param name="range">Range.</param>
        [HttpPost("queryNewestMessage")]
        public async Task<IActionResult> QueryNewest([Required][FromBody]Range range)
        {
            return await Wrapper.wrap(async (OracleConnection conn) =>
            {
                //function FUNC_SHOW_MESSAGE_BY_TIME(startFrom in INTEGER, limitation in INTEGER, search_result out sys_refcursor)
                //return INTEGER
                string procedurename = "FUNC_SHOW_MESSAGE_BY_TIME";
                OracleCommand cmd = new OracleCommand(procedurename, conn);
                cmd.CommandType = CommandType.StoredProcedure;

                //Add return value
                OracleParameter p1 = new OracleParameter();
                p1 = cmd.Parameters.Add("state", OracleDbType.Int32);
                p1.Direction = ParameterDirection.ReturnValue;

                //Add first parameter startFrom
                OracleParameter p2 = new OracleParameter();
                p2 = cmd.Parameters.Add("startFrom", OracleDbType.Int32);
                p2.Direction = ParameterDirection.Input;
                p2.Value = range.startFrom;

                //Add second parameter limitation
                OracleParameter p3 = new OracleParameter();
                p3 = cmd.Parameters.Add("limitation", OracleDbType.Int32);
                p3.Direction = ParameterDirection.Input;
                p3.Value = range.limitation;

                //Add third parameter search_result
                OracleParameter p4 = new OracleParameter();
                p4 = cmd.Parameters.Add("search_result", OracleDbType.RefCursor);
                p4.Direction = ParameterDirection.Output;

                //Get the result table
                OracleDataAdapter DataAdapter = new OracleDataAdapter(cmd);
                DataTable dt = new DataTable();
                await Task.FromResult(DataAdapter.Fill(dt));

                if (int.Parse(p1.Value.ToString()) == 0)
                {
                    throw new Exception("failed");
                }
                MessageForShow[] receivedTwitters = new MessageForShow[dt.Rows.Count];

                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    receivedTwitters[i] = new MessageForShow();
                    receivedTwitters[i].message_id = int.Parse(dt.Rows[i][0].ToString());
                    receivedTwitters[i].message_content = dt.Rows[i][1].ToString();
                    receivedTwitters[i].message_create_time = dt.Rows[i][2].ToString();
                    receivedTwitters[i].message_like_num = int.Parse(dt.Rows[i][3].ToString());
                    receivedTwitters[i].message_transpond_num = int.Parse(dt.Rows[i][4].ToString());
                    receivedTwitters[i].message_comment_num = int.Parse(dt.Rows[i][5].ToString());
                    receivedTwitters[i].message_view_num = int.Parse(dt.Rows[i][6].ToString());
                    receivedTwitters[i].message_has_image = int.Parse(dt.Rows[i][7].ToString());
                    receivedTwitters[i].message_sender_user_id = int.Parse(dt.Rows[i][8].ToString());
                    receivedTwitters[i].message_heat = int.Parse(dt.Rows[i][9].ToString());
                    receivedTwitters[i].message_image_count = int.Parse(dt.Rows[i][10].ToString() == "" ? "0" : dt.Rows[i][10].ToString());
                    receivedTwitters[i].message_transpond_message_id = int.Parse(dt.Rows[i][11].ToString() == "" ? "0" : dt.Rows[i][11].ToString());

                    receivedTwitters[i].message_topics = await TopicController.SearchTopicsInTwitter(receivedTwitters[i].message_content);
                    receivedTwitters[i].message_ats = await AtController.SearchAtsInTwitter(receivedTwitters[i].message_content);

                }
                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    if (receivedTwitters[i].message_has_image == 1)
                    {
                        string path = @"wwwroot\Messages\" + receivedTwitters[i].message_id.ToString() + @"\";
                        receivedTwitters[i].message_image_urls = 
                        getMessageImageUrls(receivedTwitters[i].message_id, receivedTwitters[i].message_image_count);

                    }
                }

                RestfulResult.RestfulArray<MessageForShow> rr = new RestfulResult.RestfulArray<MessageForShow>();
                rr.Code = 200;
                rr.Message = "success";
                rr.Data = receivedTwitters;
                return new JsonResult(rr);
            });

        }

        /// <summary>
        /// 根据range来返回前几条用户自身和用户关注的人的消息
        /// 返回的是最新的range条消息
        /// 按照时间排序
        /// </summary>
        /// <returns>The messages for index.</returns>
        /// <param name="range">Range.</param>
        [HttpPost("queryFollowMessage")]
        public async Task<IActionResult> QueryFollowMessage([Required][FromBody]Range range)
        {
            int userId;
            if (HttpContext.User.Identity.IsAuthenticated)
            {
                userId = int.Parse(HttpContext.User.Claims.First().Value);
            }
            else
            {
                RestfulResult.RestfulData rr = new RestfulResult.RestfulData();
                rr.Code = 200;
                rr.Message = "Need Authentication";
                return new JsonResult(rr);
            }

            return await Wrapper.wrap(async (OracleConnection conn) =>
            {
                //function FUNC_SHOW_FOLLOW_MESSAGE(startFrom in INTEGER, limitation in INTEGER, userid in INTEGER, search_result out sys_refcursor)
                //return INTEGER
                string procedurename = "FUNC_SHOW_FOLLOW_MESSAGE";
                OracleCommand cmd = new OracleCommand(procedurename, conn);
                cmd.CommandType = CommandType.StoredProcedure;

                //Add return value
                OracleParameter p1 = new OracleParameter();
                p1 = cmd.Parameters.Add("state", OracleDbType.Int32);
                p1.Direction = ParameterDirection.ReturnValue;

                //Add first parameter startFrom
                OracleParameter p2 = new OracleParameter();
                p2 = cmd.Parameters.Add("startFrom", OracleDbType.Int32);
                p2.Direction = ParameterDirection.Input;
                p2.Value = range.startFrom;

                //Add second parameter limitation
                OracleParameter p3 = new OracleParameter();
                p3 = cmd.Parameters.Add("limitation", OracleDbType.Int32);
                p3.Direction = ParameterDirection.Input;
                p3.Value = range.limitation;

                OracleParameter p4 = new OracleParameter();
                p4 = cmd.Parameters.Add("userid", OracleDbType.Int32);
                p4.Direction = ParameterDirection.Input;
                p4.Value = userId;

                //Add third parameter search_result
                OracleParameter p5 = new OracleParameter();
                p5 = cmd.Parameters.Add("search_result", OracleDbType.RefCursor);
                p5.Direction = ParameterDirection.Output;

                //Get the result table
                OracleDataAdapter DataAdapter = new OracleDataAdapter(cmd);
                DataTable dt = new DataTable();
                await Task.FromResult(DataAdapter.Fill(dt));

                if (int.Parse(p1.Value.ToString()) == 0)
                {
                    throw new Exception("failed");
                }
                MessageForShow[] receivedTwitters = new MessageForShow[dt.Rows.Count];

                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    receivedTwitters[i] = new MessageForShow();
                    receivedTwitters[i].message_id = int.Parse(dt.Rows[i][0].ToString());
                    receivedTwitters[i].message_content = dt.Rows[i][1].ToString();
                    receivedTwitters[i].message_create_time = dt.Rows[i][2].ToString();
                    receivedTwitters[i].message_like_num = int.Parse(dt.Rows[i][3].ToString());
                    receivedTwitters[i].message_transpond_num = int.Parse(dt.Rows[i][4].ToString());
                    receivedTwitters[i].message_comment_num = int.Parse(dt.Rows[i][5].ToString());
                    receivedTwitters[i].message_view_num = int.Parse(dt.Rows[i][6].ToString());
                    receivedTwitters[i].message_has_image = int.Parse(dt.Rows[i][7].ToString());
                    receivedTwitters[i].message_sender_user_id = int.Parse(dt.Rows[i][8].ToString());
                    receivedTwitters[i].message_heat = int.Parse(dt.Rows[i][9].ToString());
                    receivedTwitters[i].message_image_count = int.Parse(dt.Rows[i][10].ToString() == "" ? "0" : dt.Rows[i][10].ToString());
                    receivedTwitters[i].message_transpond_message_id = int.Parse(dt.Rows[i][11].ToString() == "" ? "0" : dt.Rows[i][11].ToString());

                    receivedTwitters[i].message_topics = await TopicController.SearchTopicsInTwitter(receivedTwitters[i].message_content);
                    receivedTwitters[i].message_ats = await AtController.SearchAtsInTwitter(receivedTwitters[i].message_content);

                }
                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    if (receivedTwitters[i].message_has_image == 1)
                    {
                        string path = @"wwwroot\Messages\" + receivedTwitters[i].message_id.ToString() + @"\";
                        receivedTwitters[i].message_image_urls =
                        getMessageImageUrls(receivedTwitters[i].message_id, receivedTwitters[i].message_image_count);

                    }
                }

                RestfulResult.RestfulArray<MessageForShow> rr = new RestfulResult.RestfulArray<MessageForShow>();
                rr.Code = 200;
                rr.Message = "success";
                rr.Data = receivedTwitters;
                return new JsonResult(rr);
            });

        }

        /// <summary>
        /// 调用api发送推特
        /// 若推特还含有图片，还需要另外调用图片上传的接口
        /// </summary>
        /// <returns>The message.</returns>
        /// <param name="message">Message.</param>
        [HttpPost("send")]
        public async Task<IActionResult> Send([Required][FromForm]MessageForSender message)
        {
            //TODO 需要验证身份
            //有很多参数都是有初始化的
            //!!!!!与Topic的联动
            //首先要检查message中是否有两个#号括起来的连续无空格字符串
            //若有，则去数据库中检索该Topic是否存在，若不存在则添加，若存在则将其热度提高

            int userId;
            List<string> topics = new List<string>();
            List<string> ats = new List<string>();
            System.Text.RegularExpressions.Regex topicRegex = new System.Text.RegularExpressions.Regex(@"#(\w+)#");
            System.Text.RegularExpressions.Regex atRegex = new System.Text.RegularExpressions.Regex(@"@(\w+)");

            if (HttpContext.User.Identity.IsAuthenticated)
            {
                userId = int.Parse(HttpContext.User.Claims.First().Value);

                //检查message_content里含有的话题，用两个#包含的内容作为话题。若出现两个连续的#，则忽略之。
                //所有的话题内容会被保存到topics列表内，并在调用第二个函数FUNC_ADD_TOPIC时，逐一对topic的内容进行处理（不存在则创建，存在则热度+1）
                //对艾特内容同理
                System.Text.RegularExpressions.MatchCollection topicCollection = topicRegex.Matches(message.message_content);
                System.Text.RegularExpressions.MatchCollection atCollection = atRegex.Matches(message.message_content);
                for (int i = 0; i < topicCollection.Count; i++) topics.Add(topicCollection[i].Groups[1].ToString());
                for (int i = 0; i < atCollection.Count; i++) ats.Add(atCollection[i].Groups[1].ToString());
            }
            else
            {
                RestfulResult.RestfulData rr = new RestfulResult.RestfulData();
                rr.Code = 200;
                rr.Message = "Need Authentication";
                return new JsonResult(rr);
            }

            using (OracleConnection conn = new OracleConnection(ConnStr.getConnStr()))
            {
                try
                {
                    conn.ConnectionString = ConnStr.getConnStr();
                    conn.Open();
                    //FUNC_SEND_MESSAGE(message_content in VARCHAR2, message_has_image in INTEGER, user_id in INTEGER, message_image_count in INTEGER, message_id out INTEGER)
                    //return INTEGER
                    string procedureName = "FUNC_SEND_MESSAGE";
                    OracleCommand cmd = new OracleCommand(procedureName, conn);
                    cmd.CommandType = CommandType.StoredProcedure;

                    //Add return value
                    OracleParameter p1 = new OracleParameter();
                    p1 = cmd.Parameters.Add("state", OracleDbType.Int32);
                    p1.Direction = ParameterDirection.ReturnValue;

                    //Add first parameter message_content
                    OracleParameter p2 = new OracleParameter();
                    p2 = cmd.Parameters.Add("message_content", OracleDbType.Varchar2);
                    p2.Direction = ParameterDirection.Input;
                    p2.Value = message.message_content;

                    //Add second parameter message_has_image
                    OracleParameter p3 = new OracleParameter();
                    p3 = cmd.Parameters.Add("message_has_image", OracleDbType.Int32);
                    p3.Direction = ParameterDirection.Input;
                    p3.Value = message.message_has_image;

                    //Add third parameter user_id
                    OracleParameter p4 = new OracleParameter();
                    p4 = cmd.Parameters.Add("user_id", OracleDbType.Int32);
                    p4.Direction = ParameterDirection.Input;
                    p4.Value = userId;

                    //Add fourth parameter message_image_count
                    OracleParameter p5 = new OracleParameter();
                    p5 = cmd.Parameters.Add("message_image_count", OracleDbType.Int32);
                    p5.Direction = ParameterDirection.Input;
                    p5.Value = message.message_image_count;

                    //Add fifth parameter message_id
                    OracleParameter p6 = new OracleParameter();
                    p6 = cmd.Parameters.Add("message_id", OracleDbType.Int32);
                    p6.Direction = ParameterDirection.Output;

                    await cmd.ExecuteReaderAsync();
                    if (int.Parse(p1.Value.ToString()) == 0)
                    {
                        throw new Exception("failed");
                    }

                    await TopicController.AddTopicsInTwitter(message.message_content, int.Parse(p6.Value.ToString()));
                    await AtController.AddAtsInTwitter(message.message_content, int.Parse(p6.Value.ToString()), userId);

                    //若推特含图，从POST体内获得图的内容并保存到服务器
                    if (message.message_has_image == 1)
                    {
                        var images = Request.Form.Files;
                        int img_num = 0;
                        Directory.CreateDirectory(@"wwwroot\Messages\" + p6.Value.ToString());
                        foreach (var imgfile in images)
                        {
                            if (imgfile.Length > 0)
                            {
                                string img_path;
                                if (imgfile.ContentType.Substring(0, 5) == "image")
                                {
                                    img_path = @"wwwroot\Messages\" + p6.Value.ToString() + @"\" + img_num.ToString() + ".jpg";
                                }
                                else {
                                    string videoFormat = imgfile.ContentType.Split("/")[1];
                                    img_path = @"wwwroot\Messages\" + p6.Value.ToString() + @"\" + img_num.ToString() + "." + videoFormat;
                                }
                                using (var stream = new FileStream(img_path, FileMode.Create))
                                {
                                    await imgfile.CopyToAsync(stream);
                                }
                                img_num++;
                            }
                        }
                    }

                    RestfulResult.RestfulData rr = new RestfulResult.RestfulData(200, "success");
                    conn.Close();
                    return new JsonResult(rr);
                }
                catch (Exception e)
                {
                    RestfulResult.RestfulData rr = new RestfulResult.RestfulData(500, "fail");
                    Console.Write(e.Message);
                    Console.Write(e.StackTrace);
                    conn.Close();
                    return new JsonResult(rr);
                }
            }



        }

        /// <summary>
        /// 转发消息时调用的api
        /// </summary>
        /// <returns>成功与否</returns>
        /// <param name="message_id">Message identifier.</param>
        /// <param name="message">Message.</param>
        [HttpPost("transpond")]
        public async Task<IActionResult> Transpond([Required][FromBody]MessageForTransponder message)
        {
            //需要验证身份
            //返回是否转发成功
            //同样存在与Topic的联动
            int userId;
            if (HttpContext.User.Identity.IsAuthenticated)
            {
                userId = int.Parse(HttpContext.User.Claims.First().Value);

            }
            else
            {
                RestfulResult.RestfulData rr = new RestfulResult.RestfulData();
                rr.Code = 200;
                rr.Message = "Need Authentication";
                return new JsonResult(rr);
            }

            return await Wrapper.wrap(async (OracleConnection conn) =>
            {
                //FUNC_TRANSPOND_MESSAGE(userID in INTEGER, message_content in VARCHAR2, transpondID in INTEGER, messageID out INTEGER)
                //return INTEGER
                string procedureName = "FUNC_TRANSPOND_MESSAGE";
                OracleCommand cmd = new OracleCommand(procedureName, conn);
                cmd.CommandType = CommandType.StoredProcedure;

                //Add return value
                OracleParameter p1 = new OracleParameter();
                p1 = cmd.Parameters.Add("state", OracleDbType.Int32);
                p1.Direction = ParameterDirection.ReturnValue;

                //Add first parameter userID
                OracleParameter p2 = new OracleParameter();
                p2 = cmd.Parameters.Add("userID", OracleDbType.Int32);
                p2.Direction = ParameterDirection.Input;
                p2.Value = userId;

                //Add second parameter message_content
                OracleParameter p3 = new OracleParameter();
                p3 = cmd.Parameters.Add("message_content", OracleDbType.Varchar2);
                p3.Direction = ParameterDirection.Input;
                p3.Value = message.message_content;

                //Add third parameter message_sender_user_id
                OracleParameter p4 = new OracleParameter();
                p4 = cmd.Parameters.Add("transpondID", OracleDbType.Int32);
                p4.Direction = ParameterDirection.Input;
                p4.Value = message.message_transpond_message_id;

                //Add fourth parameter message_id
                OracleParameter p5 = new OracleParameter();
                p5 = cmd.Parameters.Add("messageID", OracleDbType.Int32);
                p5.Direction = ParameterDirection.Output;

                await cmd.ExecuteReaderAsync();
                if (int.Parse(p1.Value.ToString()) == 0)
                {
                    throw new Exception("failed");
                }

                await TopicController.AddTopicsInTwitter(message.message_content, int.Parse(p5.Value.ToString()));
                await AtController.AddAtsInTwitter(message.message_content, int.Parse(p5.Value.ToString()), userId);

                RestfulResult.RestfulData rr = new RestfulResult.RestfulData(200, "success");
                return new JsonResult(rr);
            });
        }


        /// <summary>
        /// 删除推特时使用
        /// </summary>
        /// <returns>The message.</returns>
        /// <param name="message_id">Message identifier.</param>
        [HttpPost("delete")]
        public IActionResult Delete([Required]int message_id)
        {
            //需要验证登录态
            //返回成功与否

            int userId;
            if (HttpContext.User.Identity.IsAuthenticated)
            {
                userId = int.Parse(HttpContext.User.Claims.First().Value);
            }
            else
            {
                RestfulResult.RestfulData rr = new RestfulResult.RestfulData();
                rr.Code = 200;
                rr.Message = "Need Authentication";
                return new JsonResult(rr);
            }

            return Wrapper.wrap((OracleConnection conn) =>
            {
                //function FUNC_DELETE_MESSAGE(message_id in INTEGER, message_has_image out INTEGER)
                //return INTEGER
                string procedurename = "FUNC_DELETE_MESSAGE";
                OracleCommand cmd = new OracleCommand(procedurename, conn);
                cmd.CommandType = CommandType.StoredProcedure;

                //Add return value
                OracleParameter p1 = new OracleParameter();
                p1 = cmd.Parameters.Add("state", OracleDbType.Int32);
                p1.Direction = ParameterDirection.ReturnValue;

                //Add first parameter message_id
                OracleParameter p2 = new OracleParameter();
                p2 = cmd.Parameters.Add("message_id", OracleDbType.Int32);
                p2.Direction = ParameterDirection.Input;
                p2.Value = message_id;

                //Add second parameter search_result
                OracleParameter p3 = new OracleParameter();
                p3 = cmd.Parameters.Add("message_has_image", OracleDbType.RefCursor);
                p3.Direction = ParameterDirection.Output;

                cmd.ExecuteReader();
                if (int.Parse(p1.Value.ToString()) == 0)
                {
                    throw new Exception("failed");
                }

                //根据返回内容，表示推特是否有图片。如果推特有图片，则把这条推特ID所对应的图片下的文件夹删掉
                if (int.Parse(p3.Value.ToString()) == 1)
                {
                    string path = @"wwwroot\Messages\" + message_id.ToString() + ".jpg";
                    if (Directory.Exists(path))
                    {
                        Directory.Delete(path, true);
                    }
                }

                RestfulResult.RestfulData rr = new RestfulResult.RestfulData(200, "success");
                return new JsonResult(rr);

            });
        }



        public static string[] getMessageImageUrls(int message_id, int message_image_count)
        {
            string[] messageImageUrls = new string[message_image_count];
            string path = @"wwwroot\Messages\" + message_id.ToString() + @"\";
            if (!Directory.Exists(path))
            {
                return new string[0];
            }
            DirectoryInfo folder = new DirectoryInfo(path);
            FileInfo[] fileInfos = folder.GetFiles("*.*");
            for (int i = 0; i < message_image_count; i++)
            {
                Console.WriteLine(fileInfos[i].Name);
                messageImageUrls[i] = "http://localhost:12293/Messages/" + message_id.ToString() + "/" + fileInfos[i].Name;
            }
            return messageImageUrls;
        }
    }
}
