﻿// Copyright © 2015-2019 Alex Kukhtin. All rights reserved.

using System;
using System.Dynamic;
using System.Threading.Tasks;
using System.Web;
using A2v10.Infrastructure;
using A2v10.Request.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace A2v10.Request
{
	public partial class BaseController
	{
		public async Task ApplicationCommand(String command, Action<ExpandoObject> setParams /*Int32 tenantId, Int64 userId*/, String json, HttpResponseBase response)
		{
			switch (command.ToLowerInvariant())
			{
				case "setperiod":
					ExpandoObject dataToSet = JsonConvert.DeserializeObject<ExpandoObject>(json, new ExpandoObjectConverter());
					setParams?.Invoke(dataToSet);
					await _dbContext.ExecuteExpandoAsync(null, "a2user_state.SetGlobalPeriod", dataToSet);
					break;
				case "switchtocompany":
					if (!_host.IsMultiCompany)
						throw new InvalidOperationException("switchtocompany");
					ExpandoObject dataToExec = JsonConvert.DeserializeObject<ExpandoObject>(json, new ExpandoObjectConverter());
					setParams?.Invoke(dataToExec);
					await SwitchToCompany(dataToExec);
					break;
				default:
					throw new RequestModelException($"Invalid application command '{command}'");
			}
		}

		public async Task SwitchToCompany(ExpandoObject data)
		{
			if (!_host.IsMultiCompany)
				throw new InvalidOperationException(nameof(SwitchToCompany));
			Int64 CompanyId = data.Get<Int64>("company");
			Int64 UserId = data.Get<Int64>("UserId");
			Int32 TenantId = 1;
			if (_host.IsMultiTenant)
				TenantId = data.Get<Int32>("TenantId");
			var saveModel = new SwitchToCompanySaveModel()
			{
				UserId = UserId,
				TenantId = TenantId,
				CompanyId = CompanyId
			};
			if (CompanyId == 0)
				throw new InvalidOperationException("Unable to switch to company with id='0'");
			await _dbContext.ExecuteAsync<SwitchToCompanySaveModel>(null, "a2security_tenant.SwitchToCompany", saveModel);
			_userStateManager.SetUserCompanyId(saveModel.CompanyId);
		}

	}
}
