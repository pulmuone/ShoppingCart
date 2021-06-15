using System;
using System.Collections.Generic;
using System.Text;

namespace ShoppingCart.Model
{
    public class ProductListItem
    {
        public ProductListItem(int id, string name, string type, byte[] image)
        {
            this.ID = id;
            this.DisplayName = name;
            this.Type = type;
            this.Image = image;
        }

        public int ID { get; set; }
        public string DisplayName { get; set; }
        public string Type { get; set; }
        public byte[] Image { get; set; }
        public string DisplayType { get { return "Barcode: " + Type; } }
    }
}
