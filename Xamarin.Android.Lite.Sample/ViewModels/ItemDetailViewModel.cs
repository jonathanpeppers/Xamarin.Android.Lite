using System;

using Xamarin.Android.Lite.Sample.Models;

namespace Xamarin.Android.Lite.Sample.ViewModels
{
    public class ItemDetailViewModel : BaseViewModel
    {
        public Item Item { get; set; }
        public ItemDetailViewModel(Item item = null)
        {
            Title = item?.Text;
            Item = item;
        }
    }
}
