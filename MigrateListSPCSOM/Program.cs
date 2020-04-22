using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SharePoint.Client;
using OfficeDevPnP.Core;
using System.Configuration;
using System.Runtime.InteropServices;

namespace MigrateListSPCSOM
{
	public enum SharepointAuth
	{
		Sharepointonline,
		Sharepointonpremise
	}
	class Program
	{
		static void Main(string[] args)
		{
			string siteUrl = "https://devbit2k11.sharepoint.com/sites/MyCompany";
			string listName = "Employee";
			string destSite = "https://devbit2k11.sharepoint.com/sites/MineTest";
			ClientContext ctx = getClientContext(siteUrl);
			MigrateList(listName, ctx, destSite);
			Console.ReadLine();
		}
		private static void CopyListItemsSP(List<ListItem> itemsToMigrate, ClientContext destContext,string listName)
		{
			List destinationList = destContext.Web.Lists.GetByTitle(listName);
			destContext.Load(destinationList.Fields);
			destContext.ExecuteQuery();
			//Migrating data.
			foreach (ListItem item in itemsToMigrate)
			{
				
				ListItemCreationInformation itemInfo = new ListItemCreationInformation();
				ListItem itemToCreate = destinationList.AddItem(itemInfo);

				foreach (Field field in destinationList.Fields)
				{
					Console.WriteLine(field.SchemaXml);
					if (!field.ReadOnlyField && !field.Hidden &&
						 field.InternalName != "Attachments")
					{
						try
						{
							itemToCreate[field.InternalName] = item[field.InternalName];
						}
						catch (Exception ex)
						{
							//Log exception
						}
					}
				}
				itemToCreate.Update();
				destContext.ExecuteQuery();
			}
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
			var authManager = new AuthenticationManager();
			// This method calls a pop up window with the login page and it also prompts  
			// for the multi factor authentication code.  
			return authManager.GetWebLoginClientContext(siteUrl);
		}
		private static void MigrateList(string listName,ClientContext sourceContext,string destSite)
		{
			List sourceList = null;
			ListItemCollection itemsToMigrate = null;

			try
			{
				if (listExists(listName,sourceContext))
				{
					sourceList = sourceContext.Web.Lists.GetByTitle(listName);
					sourceContext.Load(sourceList.Fields);
					itemsToMigrate = sourceList.GetItems(CamlQuery.CreateAllItemsQuery());
					sourceContext.Load(itemsToMigrate);
					sourceContext.ExecuteQuery();
					
				
					
					using (ClientContext destContext = getClientContext(destSite))
					{
						
						if (listExists(listName, destContext))
						{
							List destinationList = destContext.Web.Lists.GetByTitle(listName);
							Console.WriteLine("List exist at destination site");
							destinationList.DeleteObject();
							destContext.ExecuteQueryRetry();
							Console.WriteLine("List Deleted at destination site");
							CreateListSP(destContext, listName, sourceList);
							Console.WriteLine(listName + " List created successfully");
							int count = 0;
							foreach (var field in sourceList.Fields)
							{
								if (!field.ReadOnlyField && !field.Hidden && field.InternalName != "Attachments" && field.Group != "_Hidden" && field.InternalName != "Title")
								{
									count++;
									Field simpleTextField = destinationList.Fields.AddFieldAsXml(field.SchemaXml, true, AddFieldOptions.AddFieldInternalNameHint);
								}
							}
							Console.WriteLine(count+ ": Fields Created");
							destContext.ExecuteQuery();
							Console.WriteLine("Field created @ destination url.....");
							Console.WriteLine("Copy items Starting.....");
							CopyListItemsSP(itemsToMigrate.ToList(), destContext, listName);
						}
						else
						{
							
							Console.WriteLine("List at destination does not exist");
							CreateListSP(destContext, listName);
							Console.WriteLine(listName+" List created successfully");
							List destinationList = destContext.Web.Lists.GetByTitle(listName);
							int count = 0;
							foreach (var field in sourceList.Fields)
							{
								if (!field.ReadOnlyField && !field.Hidden && field.InternalName != "Attachments" && field.Group !="_Hidden" && field.InternalName!="Title")
								{
									count++;
									Field simpleTextField = destinationList.Fields.AddFieldAsXml(field.SchemaXml, true, AddFieldOptions.AddFieldInternalNameHint);
								}
							}
							Console.WriteLine(count);
							destContext.ExecuteQuery();
							Console.WriteLine("Field created @ destination url.....");
							Console.WriteLine("Copy items Starting.....");
							CopyListItemsSP(itemsToMigrate.ToList(), destContext, listName);
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
