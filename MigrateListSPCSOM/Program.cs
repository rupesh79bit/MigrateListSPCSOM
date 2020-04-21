using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SharePoint.Client;
using OfficeDevPnP.Core;
using System.Configuration;


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
		}

		private static void CopyListItemsSP(ListItemCollection itemsToMigrate, ClientContext destContext,string listName)
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
		private static void CreateListSP(ClientContext clientContext,string listName)
		{
			ListCreationInformation creationInfo = new ListCreationInformation();
			creationInfo.Title = listName;
			creationInfo.Description = "new list created " + listName;
			creationInfo.TemplateType = (int)ListTemplateType.GenericList;
			// Create a new custom list    

			List newList = clientContext.Web.Lists.Add(creationInfo);
			// Retrieve the custom list properties    
			clientContext.Load(newList);
			// Execute the query to the server.    
			clientContext.ExecuteQuery();
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
						List destinationList = destContext.Web.Lists.GetByTitle(listName);
						if (listExists(listName, destContext))
						{
							Console.WriteLine("List exist at destination site");
							ListItemCollection destinationListItems = destinationList.GetItems(CamlQuery.CreateAllItemsQuery());
							sourceContext.Load(destinationListItems);
							sourceContext.ExecuteQuery();
							if (destinationListItems.Count < itemsToMigrate.Count)
							{
								Console.WriteLine("List exist at destination site");
								CopyListItemsSP(itemsToMigrate, destContext, listName);
							}
							else
								Console.WriteLine("List Created Successfully");
						}
						else
						{
							Console.WriteLine("List at destination does not exist");
							CreateListSP(destContext, listName);
							Console.WriteLine(listName+" List created successfully");
							
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
							CopyListItemsSP(itemsToMigrate, destContext, listName);
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

				throw;
			}

		}
		private static string GetConfigValues(string key)
		{
			return ConfigurationManager.AppSettings[key];
		}
	}
}
