//---------------------------------------------------------------------
//  This file is part of the Microsoft .NET Framework SDK Code Samples.
// 
//  Copyright (C) Microsoft Corporation.  All rights reserved.
// 
//This source code is intended only as a supplement to Microsoft
//Development Tools and/or on-line documentation.  See these other
//materials for detailed information regarding Microsoft code samples.
// 
//THIS CODE AND INFORMATION ARE PROVIDED AS IS WITHOUT WARRANTY OF ANY
//KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
//IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
//PARTICULAR PURPOSE.
//---------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Windows.Forms;
using System.Collections.Specialized;
using Microsoft.Win32;

// NOTE: This provider uses Assembly metadata such as ProductName, etc. 
// to determine a workable registry path in which to store settings. 
// Note that these are NOT secure metadata elements, however they are 
// reasonably safe from collision but not at all safe from malicious tampering.  
// A robust implementation of the provider would include a better pathing algorithm.

namespace LazerTagHostUI
{
	class RegistrySettingsProvider : SettingsProvider
	{
		public override string ApplicationName
		{
			get { return Application.ProductName; }
			set { }
		}

		public override void Initialize(string name, NameValueCollection col)
		{
			base.Initialize(ApplicationName, col);
		}

		// SetPropertyValue is invoked when ApplicationSettingsBase.Save is called
		// ASB makes sure to pass each provider only the values marked for that provider -
		// though in this sample, since the entire settings class was marked with a SettingsProvider
		// attribute, all settings in that class map to this provider
		public override void SetPropertyValues(SettingsContext context, SettingsPropertyValueCollection propvals)
		{
			// Iterate through the settings to be stored
			// Only IsDirty=true properties should be included in propvals
			foreach (SettingsPropertyValue propval in propvals)
			{
				// NOTE: this provider allows setting to both user- and application-scoped
				// settings. The default provider for ApplicationSettingsBase - 
				// LocalFileSettingsProvider - is read-only for application-scoped setting. This 
				// is an example of a policy that a provider may need to enforce for implementation,
				// security or other reasons.
				GetRegKey(propval.Property).SetValue(propval.Name, propval.SerializedValue);
			}
		}

		public override SettingsPropertyValueCollection GetPropertyValues(SettingsContext context, SettingsPropertyCollection props)
		{
			// Create new collection of values
			var values = new SettingsPropertyValueCollection();

			// Iterate through the settings to be retrieved
			foreach (SettingsProperty setting in props)
			{
				var value = new SettingsPropertyValue(setting)
					{
						IsDirty = false,
						SerializedValue = GetRegKey(setting).GetValue(setting.Name, setting.DefaultValue)
					};
				values.Add(value);
			}
			return values;
		}

		// Helper method: fetches correct registry subkey.
		// HKLM is used for settings marked as application-scoped.
		// HKLU is used for settings marked as user-scoped.
		private static RegistryKey GetRegKey(SettingsProperty prop)
		{
			var regKey = IsUserScoped(prop) ? Registry.CurrentUser : Registry.LocalMachine;
			regKey = regKey.CreateSubKey(GetSubKeyPath());
			return regKey;
		}

		// Helper method: walks the "attribute bag" for a given property
		// to determine if it is user-scoped or not.
		// Note that this provider does not enforce other rules, such as 
		//   - unknown attributes
		//   - improper attribute combinations (e.g. both user and app - this implementation
		//     would say true for user-scoped regardless of existence of app-scoped)
		private static bool IsUserScoped(SettingsProperty prop)
		{
			return (from DictionaryEntry d in prop.Attributes select (Attribute) d.Value).Any(a => a is UserScopedSettingAttribute);
		}

		// Builds a key path based on the CompanyName, ProductName, and ProductVersion attributes in 
		// the AssemblyInfo file (editable directly or within the Project Properties UI)
		private static string GetSubKeyPath()
		{
			var keys = new List<string> {"Software"};
			if (!string.IsNullOrWhiteSpace(Application.CompanyName)) keys.Add(Application.CompanyName);
			if (!string.IsNullOrWhiteSpace(Application.ProductName) &&
				Application.ProductName != Application.CompanyName) keys.Add(Application.ProductName);
			return string.Join("\\",keys);
		}
	}
}
