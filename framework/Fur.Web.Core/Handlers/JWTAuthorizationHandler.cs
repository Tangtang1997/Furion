﻿using Fur.Authorization;
using Fur.Core;
using Fur.DataEncryption;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Fur.Web.Core
{
    /// <summary>
    /// JWT 授权自定义处理程序
    /// </summary>
    public class JWTAuthorizationHandler : AppAuthorizeHandler
    {
        /// <summary>
        /// 请求管道
        /// </summary>
        /// <param name="context"></param>
        /// <param name="httpContext"></param>
        /// <returns></returns>
        public override bool Pipeline(AuthorizationHandlerContext context, DefaultHttpContext httpContext)
        {
            // 判断请求报文头中是否有 "Authorization" 报文头
            var bearerToken = httpContext.HttpContext.Request.Headers["Authorization"].ToString();
            if (string.IsNullOrEmpty(bearerToken)) return false;

            // 获取 token
            var accessToken = bearerToken[7..];

            // 验证token
            var (IsValid, Token) = JWTEncryption.Validate(accessToken, App.GetOptions<JWTSettingsOptions>());
            if (!IsValid) return false;

            // 检查权限
            return CheckAuthorzie(httpContext, Token);
        }

        /// <summary>
        /// 检查权限
        /// </summary>
        /// <param name="httpContext"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        private bool CheckAuthorzie(DefaultHttpContext httpContext, JsonWebToken token)
        {
            // 获取权限特性
            var securityDefineAttribute = httpContext.GetEndpoint().Metadata.GetMetadata<SecurityDefineAttribute>();
            if (securityDefineAttribute == null) return true;

            return App.GetService<IAuthorizationManager>().CheckSecurity(securityDefineAttribute.ResourceId);
        }
    }
}