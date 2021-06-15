using ShoppingCart.Model;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using Xamarin.Forms;
using Newtonsoft.Json;
using Rg.Plugins.Popup.Extensions;

namespace ShoppingCart
{
    public partial class MainPage : ContentPage
    {
        private ObservableCollection<MainListItem> allListsSource;

        public MainPage()
        {
            InitializeComponent();

            allListsSource = new ObservableCollection<MainListItem>();

            if (Application.Current.Properties.ContainsKey(App.Current.Resources["AllListsSource"].ToString()))
                allListsSource = JsonConvert.DeserializeObject<ObservableCollection<MainListItem>>(Application.Current.Properties[App.Current.Resources["AllListsSource"].ToString()].ToString());

            lstAllLists.ItemsSource = allListsSource;
        }

        private async void txtNewList_Completed(object sender, EventArgs e)
        {
            if (((Entry)sender).Text != null && ((Entry)sender).Text.Trim().Length > 0)
            {
                int allListsSourceMaxID = 0;
                if (allListsSource.Count > 0)
                    allListsSourceMaxID = allListsSource.Max(x => x.ID);

                allListsSource.Add(new MainListItem(allListsSourceMaxID + 1, ((Entry)sender).Text, new ObservableCollection<ProductListItem>()));

                ((Entry)sender).Text = "";

                Application.Current.Properties[App.Current.Resources["AllListsSource"].ToString()] = JsonConvert.SerializeObject(allListsSource);
                await Application.Current.SavePropertiesAsync();
            }
        }

        private async void Delete_Tapped(object sender, EventArgs e)
        {
            MainListItem selectedItem = allListsSource.First<MainListItem>(x => x.ID == (int)((TappedEventArgs)e).Parameter);

            if (selectedItem != null && selectedItem.ID > 0 && await DisplayAlert("Delete", "Are you sure you want to delete " + selectedItem.DisplayName, "OK", "CANCEL"))
            {
                allListsSource.Remove(selectedItem);

                Application.Current.Properties[App.Current.Resources["AllListsSource"].ToString()] = JsonConvert.SerializeObject(allListsSource);
                await Application.Current.SavePropertiesAsync();
            }
        }

        private async void Edit_Tapped(object sender, EventArgs e)
        {
            int index = -1;
            try { index = allListsSource.IndexOf(allListsSource.First<MainListItem>(x => x.ID == (int)((TappedEventArgs)e).Parameter)); } catch { }

            if (index > -1)
            {
                IsEnabled = false;
                await Navigation.PushPopupAsync(new EditModalPage(allListsSource, index, -1), true);
                IsEnabled = true;
            }
        }

        private async void listItem_Tapped(object sender, EventArgs e)
        {
            int index = -1;
            try { index = allListsSource.IndexOf(allListsSource.First<MainListItem>(x => x.ID == (int)((TappedEventArgs)e).Parameter)); } catch { }

            if (index > -1)
            {
                IsEnabled = false;
                await Navigation.PushAsync(new ProductItemPage(allListsSource, index), true);
                IsEnabled = true;
            }
        }
    }
}
