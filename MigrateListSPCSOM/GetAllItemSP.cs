using Microsoft.SharePoint.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace MigrateListSPCSOM
{
	class GetAllItemSP
	{
		public static List<ListItem> GetListItems(ClientContext ctx,string listName,[Optional] string listUrl,[Optional] CamlQuery camlQuery)
		{
			List<ListItem> items = new List<ListItem>();
			List list = ctx.Web.Lists.GetByTitle(listName);
			int rowLimit = 5000;
			string viewXml = string.Format(@"
                            <View Scope = 'RecursiveAll'><RowLimit> {0} </RowLimit></View >",
                            rowLimit);

            var query = new CamlQuery();
            if (camlQuery==null)
            {
                query.ViewXml = viewXml;
            }
            else
            {
                query = camlQuery;
            }
            try
            {
                do
                {
                    ListItemCollection listItemCollection = list.GetItems(query);
                    ctx.Load(listItemCollection);
                    ctx.ExecuteQuery();

                    //Adding the current set of ListItems in our single buffer
                    items.AddRange(listItemCollection);
                    //Reset the current pagination info
                    query.ListItemCollectionPosition = listItemCollection.ListItemCollectionPosition;

                }
                while (query.ListItemCollectionPosition != null);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error Occured get all items "+ex.StackTrace+"/n"+ex.Message);
            }
            
            return items;
		}
	}
}
