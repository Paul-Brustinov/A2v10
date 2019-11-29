﻿// Copyright © 2015-2018 Alex Kukhtin. All rights reserved.


using System;
using System.Web.Mvc;
using System.Threading.Tasks;
using System.IO;
using System.Dynamic;
using System.Web;
using System.Xml;
using System.Xml.Linq;
using System.Text;

using Microsoft.AspNet.Identity;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

using A2v10.Infrastructure;
using A2v10.Request;
using A2v10.Interop;
using A2v10.Web.Identity;

namespace A2v10.Web.Mvc.Controllers
{
	[AllowAnonymous]
	public class ApiController : Controller
	{
		A2v10.Request.BaseController _baseController = new BaseController();
		ILogger _logger;

		public ApiController()
		{
			_logger = ServiceLocator.Current.GetService<ILogger>();
			_baseController.Host.StartApplication(false);
		}

		public Int64? UserId
		{
			get
			{
				if (User.Identity == null)
					return null;
				return User.Identity.GetUserId<Int64>();
			}
		}

		public Int32 TenantId => User.Identity.GetUserTenantId();

		Boolean ValidAllowAddress(RequestCommand ac)
		{
			if (String.IsNullOrEmpty(ac.AllowAddressForCheck))
				return true;
			if (!ac.AllowAddressForCheck.Contains(Request.UserHostAddress))
			{
				Response.StatusCode = 403; // forbidden
				return false;
			}
			return true;
		}

		[HttpGet]
		[ActionName("Default")]
		public async Task DefaultGET(String pathInfo)
		{
			Guid apiGuid = Guid.NewGuid();
			try
			{
				_logger.LogApi($"get: {pathInfo}", Request.UserHostAddress, apiGuid);
				var rm = await RequestModel.CreateFromApiUrl(_baseController.Host, "_api/" + pathInfo);
				var ac = rm.CurrentCommand;

				if (ac.AllowOriginForCheck == null)
					throw new RequestModelException($"'allowOrigin' is required for '{ac.command}' command");

				if (!ValidAllowAddress(ac))
					return;

				Response.AddHeader("Access-Control-Allow-Origin", ac.AllowOriginForCheck);

				switch (ac.type)
				{
					case CommandType.file:
						if (ac.file == null)
							throw new RequestModelException($"'file' is required for '{ac.command}' command");
						GetFile(ac);
						break;
					case CommandType.clr:
						await ExecuteClrCommand(ac, GetDataToInvokeGet(ac.wrapper, apiGuid), apiGuid);
						break;
					default:
						throw new NotImplementedException();
				}
			} catch (Exception ex)
			{
				if (ex.InnerException != null)
					ex = ex.InnerException;
				Response.ContentType = "text/plain";
				Response.Output.Write(ex.Message);
			}
		}

		void SetIdentityParams(ExpandoObject prms)
		{
			Int64? userId = UserId;
			if (userId != null)
				prms.Set("UserId", userId.Value);
			Int32 tenantId = TenantId;
			if (tenantId != 0)
				prms.Set("TenantId", tenantId);
		}

		ExpandoObject GetDataToInvokeGet(String wrapper, Guid apiGuid)
		{
			var dataToInvoke = new ExpandoObject();
			SetIdentityParams(dataToInvoke);
			var qs = Request.QueryString;
			if (qs.HasKeys())
			{
				foreach (var k in qs.AllKeys)
					dataToInvoke.Set(k, qs[k]);
			}
			if (!String.IsNullOrEmpty(wrapper))
			{
				ExpandoObject wrap = new ExpandoObject();
				wrap.Set(wrapper, dataToInvoke);
				dataToInvoke = wrap;
			}
			_logger.LogApi($"getdata: {JsonConvert.SerializeObject(dataToInvoke)}", Request.UserHostAddress, apiGuid);
			return dataToInvoke;
		}

		void GetFile(RequestCommand cmd)
		{
			var appReader = _baseController.Host.ApplicationReader;
			String fullPath = appReader.MakeFullPath(cmd.Path, cmd.file);
			String htmlPath = fullPath + ".html";
			if (!appReader.FileExists(htmlPath))
				throw new FileNotFoundException($"File not found '{fullPath}'");
			Response.ContentType = MimeMapping.GetMimeMapping(htmlPath);
			StringBuilder sb = new StringBuilder(appReader.FileReadAllText(htmlPath));
			String serverUrl = Request.Url.GetLeftPart(UriPartial.Authority);
			sb.Replace("$(ServerUrl)", serverUrl);
			Response.Write(sb.ToString());
		}

		//runAllManagedModulesForAllRequests="true" is required!
		[HttpOptions]
		[ActionName("Default")]
		public async Task DefaultOptions(String pathInfo)
		{
			try
			{
				var rm = await RequestModel.CreateFromApiUrl(_baseController.Host, "_api/" + pathInfo);
				var ac = rm.CurrentCommand;

				if (!ValidAllowAddress(ac))
					return;

				Response.AddHeader("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE");
				Response.AddHeader("Access-Control-Allow-Headers", "Content-Type, Accept, X-Requested-With");
				Response.AddHeader("Access-Control-Allow‌​-Credentials", "true");
				Response.AddHeader("Access-Control-Max-Age", "60");
				Response.AddHeader("Access-Control-Allow-Origin", ac.AllowOriginForCheck);
			}
			catch (Exception ex)
			{
				if (ex.InnerException != null)
					ex = ex.InnerException;
				_logger.LogApiError(ex.Message, Request.UserHostAddress, Guid.NewGuid());

			}
		}

		[HttpPost]
		[ActionName("Default")]
		public async Task DefaultPOST(String pathInfo)
		{
			Guid apiGuid = Guid.NewGuid();
			try
			{
				_logger.LogApi($"post: {pathInfo}", Request.UserHostAddress, apiGuid);
				var rm = await RequestModel.CreateFromApiUrl(_baseController.Host, "_api/" + pathInfo);
				var ac = rm.CurrentCommand;

				if (!ValidAllowAddress(ac))
					return;

				Response.ContentType = "application/json";
				Response.AddHeader("Access-Control-Allow-Origin", ac.AllowOriginForCheck);

				String json = null;
				Request.InputStream.Seek(0, SeekOrigin.Begin); // ensure
				using (var tr = new StreamReader(Request.InputStream))
				{
					json = tr.ReadToEnd();
					_logger.LogApi($"request: {json}", Request.UserHostAddress, apiGuid);
				}
				ExpandoObject dataToInvoke = JsonConvert.DeserializeObject<ExpandoObject>(json, new ExpandoObjectConverter());
				if (dataToInvoke == null)
					dataToInvoke = new ExpandoObject();
				if (!String.IsNullOrEmpty(ac.wrapper))
				{
					ExpandoObject wrap = new ExpandoObject();
					wrap.Set(ac.wrapper, dataToInvoke);
					dataToInvoke = wrap;
				}
				SetIdentityParams(dataToInvoke);
				await ExecuteCommand(ac, dataToInvoke, apiGuid);
			}
			catch (Exception ex)
			{
				if (ex.InnerException != null)
					ex = ex.InnerException;
				_logger.LogApiError(ex.Message, Request.UserHostAddress, apiGuid);
				_baseController.WriteExceptionStatus(ex, Response);
			}
		}

		async Task ExecuteCommand(RequestCommand cmd, ExpandoObject dataToInvoke, Guid apiGuid)
		{
			switch (cmd.type)
			{
				case CommandType.clr:
					await ExecuteClrCommand(cmd, dataToInvoke, apiGuid);
					break;
			}
		}

		async Task ExecuteClrCommand(RequestCommand cmd, ExpandoObject dataToInvoke, Guid apiGuid)
		{
			TextWriter writer = Response.Output;
			if (String.IsNullOrEmpty(cmd.clrType))
				throw new RequestModelException($"clrType must be specified for command '{cmd.command}'");
			var invoker = new ClrInvoker();
			Object result = null;
			if (cmd.async)
				result = await invoker.InvokeAsync(cmd.clrType, dataToInvoke);
			else
				result = invoker.Invoke(cmd.clrType, dataToInvoke);
			if (result == null)
				return;
			if (result is String)
			{
				_logger.LogApi($"response: {result.ToString()}", Request.UserHostAddress, apiGuid);
				writer.Write(result.ToString());
			}
			else if (result is XDocument xDoc)
			{
				Response.ContentType = "text/xml";
				using (var xmlWriter = XmlWriter.Create(writer))
				{
					xDoc.WriteTo(xmlWriter);
				}
			}
			else
			{
				String json = JsonConvert.SerializeObject(result, JsonHelpers.StandardSerializerSettings);
				_logger.LogApi($"response: {json}", Request.UserHostAddress, apiGuid);
				writer.Write(json);
			}
		}
	}
}
