using Microsoft.SharePoint.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MigrateListSPCSOM
{
	class deleteAllItemSP
	{
        public static void deleteAllFromList(ClientContext cc, List myList)
        {
            int queryLimit = 4000;
            int batchLimit = 100;
            bool moreItems = true;

            string viewXml = string.Format(@"
                            <View>
                                <Query><Where></Where></Query>
                                <ViewFields>
                                    <FieldRef Name='ID' />
                                </ViewFields>
                                <RowLimit>{0}</RowLimit>
                            </View>", queryLimit);
            var camlQuery = new CamlQuery();
            camlQuery.ViewXml = viewXml;

            while (moreItems)
            {
                ListItemCollection listItems = myList.GetItems(camlQuery); // CamlQuery.CreateAllItemsQuery());
                cc.Load(listItems,
                    eachItem => eachItem.Include(
                        item => item,
                        item => item["ID"]));
                cc.ExecuteQuery();

                var totalListItems = listItems.Count;
                if (totalListItems > 0)
                {
                    Console.WriteLine("Deleting {0} items ", totalListItems);
                    for (var i = totalListItems - 1; i > -1; i--)
                    {
                        listItems[i].DeleteObject();
                        if (i % batchLimit == 0)
                            cc.ExecuteQuery();
                    }
                    cc.ExecuteQuery();
                }
                else
                {
                    moreItems = false;
                }
            }
            Console.WriteLine("Deletion complete.");
        }
    }
}
