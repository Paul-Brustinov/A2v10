﻿// Copyright © 2015-2019 Alex Kukhtin. All rights reserved.

using System;

namespace A2v10.Request.Models
{
	public class TenantUserModel
	{
		public String Module { get; set; }

		public override String ToString()
		{
			return Module;
		}
	}
}
