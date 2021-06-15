using Newtonsoft.Json;
using Rg.Plugins.Popup.Pages;
using Rg.Plugins.Popup.Services;
using ShoppingCart.Model;
using System;
using System.Collections.ObjectModel;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace ShoppingCart
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class EditModalPage : PopupPage
    {
        private ObservableCollection<MainListItem> listToEdit;
        private int indexMainList;
        private int indexProductList;

        public EditModalPage(ObservableCollection<MainListItem> _listToEdit, int _mainListIndex, int _productListIndex)
        {
            InitializeComponent();

            listToEdit = _listToEdit;
            indexMainList = _mainListIndex;
            indexProductList = _productListIndex;

            if (indexProductList == -1)
            {
                lblMessage.Text = "Are you sure that you want to edit name for " + listToEdit[indexMainList].DisplayName + " ?";
                txtNewName.Text = listToEdit[indexMainList].DisplayName;
            }
            else
            {
                if (listToEdit[indexMainList].ProductsList[indexProductList].DisplayName.Length > 0)
                {
                    lblMessage.Text = "Are you sure that you want to edit name for " + listToEdit[indexMainList].ProductsList[indexProductList].DisplayName + " ?";
                    txtNewName.Text = listToEdit[indexMainList].ProductsList[indexProductList].DisplayName;
                }
                else
                    lblMessage.Text = "Add product name";
            }
        }

        private async void OnOkTap(object sender, EventArgs e)
        {
            if (txtNewName.Text != null && txtNewName.Text.Trim().Length > 0)
            {
                if (indexProductList == -1)
                    listToEdit[indexMainList] = new MainListItem(listToEdit[indexMainList].ID, txtNewName.Text, listToEdit[indexMainList].ProductsList);
                else
                    listToEdit[indexMainList].ProductsList[indexProductList] = new ProductListItem(listToEdit[indexMainList].ProductsList[indexProductList].ID, txtNewName.Text, listToEdit[indexMainList].ProductsList[indexProductList].Type, listToEdit[indexMainList].ProductsList[indexProductList].Image);

                Application.Current.Properties[App.Current.Resources["AllListsSource"].ToString()] = JsonConvert.SerializeObject(listToEdit);
                await Application.Current.SavePropertiesAsync();

                await PopupNavigation.Instance.PopAsync();
            }
        }

        private void OnCancelTap(object sender, EventArgs e)
        {
            PopupNavigation.Instance.PopAsync();
        }
    }
}
