﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Newtonsoft.Json;
using StackExchange.Profiling;
using System.Linq;
using System.Net.Mime;

namespace Fur.DataValidation
{
    /// <summary>
    /// 数据验证控制器
    /// </summary>
    public sealed class DataValidationFilter : IActionFilter, IOrderedFilter
    {
        /// <summary>
        /// 过滤器排序
        /// </summary>
        internal const int FilterOrder = -2000;

        /// <summary>
        /// 排序
        /// </summary>
        public int Order => FilterOrder;

        /// <summary>
        /// 是否时可重复使用的
        /// </summary>
        public bool IsReusable => true;

        /// <summary>
        /// 动作方法执行之前操作
        /// </summary>
        /// <param name="context"></param>
        public void OnActionExecuting(ActionExecutingContext context)
        {
            // 获取动作方法描述器
            var actionDescriptor = context.ActionDescriptor as ControllerActionDescriptor;
            var method = actionDescriptor.MethodInfo;
            var modelState = context.ModelState;

            // 跳过验证类型
            var nonValidateAttributeType = typeof(NonVaildateAttribute);

            // 如果贴了 [NonVaildate] 特性 或 所在类型贴了 [NonVaildate] 特性，则跳过验证
            if (method.IsDefined(nonValidateAttributeType, true) ||
                method.DeclaringType.IsDefined(nonValidateAttributeType, true))
            {
                // 打印验证跳过消息
                MiniProfiler.Current.CustomTiming("validation", "Validation Skipped", "Skipped !");
                return;
            }

            // 如果动作方法参数为 0 或 验证通过，则跳过，
            if (method.GetParameters().Length == 0 || modelState.IsValid)
            {
                // 打印验证成功消息
                MiniProfiler.Current.CustomTiming("validation", "Validation Successed", "Successed");
                return;
            }

            // 返回验证失败结果
            if (context.Result == null && !modelState.IsValid)
            {
                string validationResults = SetValidateFailedResult(context, modelState);

                // 打印验证失败信息
                MiniProfiler.Current.CustomTiming("validation", $"Validation Failed:\r\n{validationResults}", "Failed").Errored = true;
            }
        }

        /// <summary>
        /// 设置验证失败结果
        /// </summary>
        /// <param name="context">动作方法执行上下文</param>
        /// <param name="modelState">模型验证状态</param>
        /// <returns></returns>
        private static string SetValidateFailedResult(ActionExecutingContext context, ModelStateDictionary modelState)
        {
            // 返回 400 错误
            var result = new BadRequestObjectResult(modelState);

            // 设置返回的响应类型
            result.ContentTypes.Add(MediaTypeNames.Application.Json);
            result.ContentTypes.Add(MediaTypeNames.Application.Xml);

            context.Result = result;

            // 将验证错误信息转换成字典并序列化成 Json
            var validationResults = JsonConvert.SerializeObject(
                modelState.ToDictionary(u => u.Key, u => modelState[u.Key].Errors.Select(c => c.ErrorMessage))
                , Formatting.Indented);
            return validationResults;
        }

        /// <summary>
        /// 动作方法执行完成操作
        /// </summary>
        /// <param name="context"></param>
        public void OnActionExecuted(ActionExecutedContext context)
        {
        }
    }
}