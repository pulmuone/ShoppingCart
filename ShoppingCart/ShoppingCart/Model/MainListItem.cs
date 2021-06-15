using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace ShoppingCart.Model
{
    public class MainListItem
    {
        public MainListItem(int id, string displayName, ObservableCollection<ProductListItem> productsList)
        {
            this.ID = id;
            this.DisplayName = displayName;
            this.ProductsList = productsList;
        }

        public int ID { get; set; }
        public string DisplayName { get; set; }

        public ObservableCollection<ProductListItem> ProductsList { get; set; }
    }
}
