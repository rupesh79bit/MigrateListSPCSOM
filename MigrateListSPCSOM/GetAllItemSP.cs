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
		public List<ListItem> GetListItems(ClientContext ctx,string listName,[Optional] string listUrl,[Optional] CamlQuery camlQuery)
		{
			List<ListItem> items = new List<ListItem>();
			List list = ctx.Web.Lists.GetByTitle(listName);
			int rowLimit = 100;
			ListItemCollectionPosition position = null;
			string viewXml = string.Format(@"
                            <View>
                                <Query><Where></Where></Query>
                                <ViewFields>
                                    <FieldRef Name='ID' />
                                </ViewFields>
                                <RowLimit>{0}</RowLimit>
                            </View>", rowLimit);

            var query = new CamlQuery();
            if (camlQuery==null)
            {
                query.ViewXml = viewXml;
            }
            else
            {
                query = camlQuery;
            }
			

            do
            {
                ListItemCollection listItems = null;
                if (listItems != null && listItems.ListItemCollectionPosition != null)
                {
                    query.ListItemCollectionPosition = listItems.ListItemCollectionPosition;
                }
                listItems = list.GetItems(CamlQuery.CreateAllItemsQuery());
                ctx.Load(listItems);
                position = listItems.ListItemCollectionPosition;
                items.AddRange(listItems.ToList());
            }
            while (position != null);
            return items;
		}
	}
}
