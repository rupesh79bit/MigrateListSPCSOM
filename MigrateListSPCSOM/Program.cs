using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SharePoint.Client;
using OfficeDevPnP.Core;
using System.Configuration;
using System.Runtime.InteropServices;
using System.Net;

namespace MigrateListSPCSOM
{
	class Program
	{
		static void Main(string[] args)
		{
			string srUrl = GetConfigValues("sourceSiteUrl");
			string listName = GetConfigValues("listName");
			string destSiteUrl = GetConfigValues("destinationSiteUrl");
			MigrateList(listName, getClientContext(srUrl), destSiteUrl);
			//var sitesUri = "http://client1.rmsuat.evalueserve.com/sites/dynamotesting/";
			//string userName = "ramesh.pandey@rmsdomain.com";
			//string pass = "Evs1234$";
			//var ctx = new ClientContext(sitesUri);
			//ctx.Credentials = new NetworkCredential(userName, pass);
			//ctx.Load(ctx.Web);
			//ctx.ExecuteQuery();
			//Console.WriteLine(ctx.Web.Title);
		}
		private static void CopyListItemsSP(List<ListItem> itemsToMigrate, ClientContext destContext,string listName)
		{
			List destinationList = destContext.Web.Lists.GetByTitle(listName);
			destContext.Load(destinationList.Fields);
			destContext.ExecuteQuery();
			int cnt = 0;
			//Migrating data.
			foreach (ListItem item in itemsToMigrate)
			{
				ListItemCreationInformation itemInfo = new ListItemCreationInformation();
				ListItem itemToCreate = destinationList.AddItem(itemInfo);
				foreach (Field field in destinationList.Fields)
				{
					//Console.WriteLine(field.SchemaXml);
					if (!field.ReadOnlyField && !field.Hidden &&
						 field.InternalName != "Attachments")
					{
						try
						{
							itemToCreate[field.InternalName] = item[field.InternalName];
						}
						catch (Exception ex)
						{
							Console.WriteLine(ex.Message);
						}
					}
				}
				itemToCreate.Update();
				if (++cnt % 4000 == 0)
				{
					destContext.ExecuteQuery();
				}
			}
			destContext.ExecuteQuery();
		}
		private static void CreateListSP(ClientContext ctx,string listName, [Optional] List sourceList)
		{
			ListCreationInformation creationInfo = new ListCreationInformation();
			if (sourceList!=null)
			{
				creationInfo.Title = sourceList.Title;
				creationInfo.Description = sourceList.Description;
				creationInfo.TemplateType = sourceList.BaseTemplate;
			}
			else
			{
				creationInfo.Title = listName;
				creationInfo.Description = "new list created " + listName;
				creationInfo.TemplateType = (int)ListTemplateType.GenericList;
			}

			List newList = ctx.Web.Lists.Add(creationInfo);
			ctx.Load(newList);
			ctx.ExecuteQuery();
		}
		private static bool listExists(string listName,ClientContext ctx)
		{
			return ctx.Web.ListExists(listName);
		}
		private static ClientContext getClientContext(string siteUrl)
		{
			var authManager = new OfficeDevPnP.Core.AuthenticationManager();
			// This method calls a pop up window with the login page and it also prompts  
			// for the multi factor authentication code.  
			return authManager.GetWebLoginClientContext(siteUrl);
		}
		private static void MigrateList(string listName,ClientContext sourceContext,string destSiteUrl)
		{
			List sourceList = null;
			ListItemCollection sourceListItemCollection = null;
			List<ListItem> itemsTomigrate = null;
			
			try
			{
				if (listExists(listName,sourceContext))
				{
					sourceList = sourceContext.Web.Lists.GetByTitle(listName);
					sourceContext.Load(sourceList.Fields);
					sourceContext.Load(sourceList);
					sourceContext.ExecuteQuery();
					if (sourceList.ItemCount <= 5000)
					{
					    sourceListItemCollection = sourceList.GetItems(CamlQuery.CreateAllItemsQuery());
						sourceContext.Load(sourceListItemCollection);
						sourceContext.ExecuteQuery();
						itemsTomigrate = sourceListItemCollection.ToList();
					}
					else
					{
						itemsTomigrate = GetAllItemSP.GetListItems(sourceContext, listName);
					}
					//!field.ReadOnlyField && !field.Hidden && field.InternalName != "Attachments" && field.Group != "_Hidden" && field.InternalName != "Title"
					List<Field> validFieldCol = sourceList.Fields.Where(x => !x.ReadOnlyField && !x.Hidden && x.InternalName != "Attachments" && x.Group != "_Hidden" && x.InternalName != "Title").ToList();


					using (ClientContext destContext = getClientContext(destSiteUrl))
					{
						List destinationList = null;
						if (listExists(listName, destContext))
						{
							destinationList = destContext.Web.Lists.GetByTitle(listName);
							destContext.Load(destinationList);
							destContext.ExecuteQuery();
							Console.WriteLine("List exist at destination site");
							if (sourceList.ItemCount != destinationList.ItemCount)
							{
								Console.WriteLine("Items at destination list has item less than source");
								destinationList.DeleteObject();
								destContext.ExecuteQueryRetry();
								Console.WriteLine("List Deleted at destination site");
								CreateListSP(destContext, listName, sourceList);
								Console.WriteLine(listName + " List created successfully");
								CreateFieldsSP.CopyFields(validFieldCol, destContext, listName);
								Console.WriteLine("Field created @ destination url.....");
								Console.WriteLine("Copy items Starting.....");
								CopyListItemsSP(itemsTomigrate, destContext, listName);
								Console.WriteLine("Copied list items completed");
							}
							
						}
						else
						{
							Console.WriteLine("List at destination does not exist");
							CreateListSP(destContext, listName);
							Console.WriteLine(listName+" List created successfully");
							destinationList = destContext.Web.Lists.GetByTitle(listName);
							CreateFieldsSP.CopyFields(validFieldCol, destContext, listName);
							Console.WriteLine("Field created @ destination url.....");
							Console.WriteLine("Copy items Starting.....");
							CopyListItemsSP(itemsTomigrate, destContext, listName);
							Console.WriteLine("List Items Copied success");
						}
					}
				}
				else
				{
					Console.WriteLine("list at source does not exist");
				}
				
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.StackTrace+"   "+ex.Message);
			}

		}
		private static string GetConfigValues(string key)
		{
			return ConfigurationManager.AppSettings[key];
		}

	}
}
