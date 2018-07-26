using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Xamarin.Android.Lite.Sample.Models;
using Xamarin.Essentials;

[assembly: Xamarin.Forms.Dependency (typeof (Xamarin.Android.Lite.Sample.Services.MockDataStore))]
namespace Xamarin.Android.Lite.Sample.Services
{
	public class MockDataStore : IDataStore<Item>
	{
		const string Key = "AllItems";
		static XmlSerializer serializer = new XmlSerializer (typeof (List<Item>));
		static List<Item> items = new List<Item> ();

		public Task<bool> AddItemAsync (Item item)
		{
			items.Add (item);
			return Save ();
		}

		public Task<bool> UpdateItemAsync (Item item)
		{
			var _item = items.Where ((Item arg) => arg.Id == item.Id).FirstOrDefault ();
			items.Remove (_item);
			items.Add (item);
			return Save ();
		}

		public Task<bool> DeleteItemAsync (string id)
		{
			var _item = items.Where ((Item arg) => arg.Id == id).FirstOrDefault ();
			items.Remove (_item);
			return Save ();
		}

		public Task<Item> GetItemAsync (string id)
		{
			return Task.FromResult (items.FirstOrDefault (s => s.Id == id));
		}

		public Task<IEnumerable<Item>> GetItemsAsync (bool forceRefresh = false)
		{
			return Task.Run (() => {
				string value = Preferences.Get (Key, null);
				Console.WriteLine ($"Read {Key} = {value}");
				if (string.IsNullOrEmpty (value)) {
					items = new List<Item> {
						new Item { Text = "First item", Description="This is an item description." },
						new Item { Text = "Second item", Description="This is an item description." },
						new Item { Text = "Third item", Description="This is an item description." },
						new Item { Text = "Fourth item", Description="This is an item description." },
						new Item { Text = "Fifth item", Description="This is an item description." },
						new Item { Text = "Sixth item", Description="This is an item description." },
					};
					Save ();
				} else {
					using (var reader = new StringReader (value)) {
						items = (List<Item>)serializer.Deserialize (reader);
					}
				}
				return items as IEnumerable<Item>;
			});
		}

		public Task<bool> Save ()
		{
			return Task.Run (() => {
				var builder = new StringBuilder ();
				using (var writer = new StringWriter (builder)) {
					serializer.Serialize (writer, items);
				}
				string value = builder.ToString ();
				Preferences.Set (Key, value);
				Console.WriteLine ($"Wrote {Key} = {value}");
				return true;
			});
		}
	}
}
