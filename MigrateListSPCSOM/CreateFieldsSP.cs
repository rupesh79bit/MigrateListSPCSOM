using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SharePoint.Client;

namespace MigrateListSPCSOM
{
	class CreateFieldsSP
	{
		public static void CopyFields(List<Field> srFieldCol,ClientContext targetContext,string targetListName)
		{
			List destinationList = targetContext.Web.Lists.GetByTitle(targetListName);
			int fieldCount = 0;
			try
			{
				foreach (var field in srFieldCol)
				{
					if (!field.ReadOnlyField && !field.Hidden && field.InternalName != "Attachments" && field.Group != "_Hidden" && field.InternalName != "Title")
					{
						Field simpleTextField = destinationList.Fields.AddFieldAsXml(field.SchemaXml, true, AddFieldOptions.AddFieldInternalNameHint);
						fieldCount++;
					//	Field simpleTextField = destination.Fields.Add(field);
						//FieldText textField = context.CastTo<FieldText>(field);
					}
				}
				targetContext.ExecuteQuery();
				Console.WriteLine("No of Fields created "+fieldCount);
			}
			catch (Exception exception)
			{
				Console.WriteLine(exception.Message);
			}
			
		}
	}
}
