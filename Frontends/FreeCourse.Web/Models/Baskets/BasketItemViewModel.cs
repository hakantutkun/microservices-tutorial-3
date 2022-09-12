namespace FreeCourse.Web.Models.Baskets
{
    public class BasketItemViewModel
    {
        public int Quantity { get; set; } = 1;
        public string CourseId { get; set; }
        public string CourseName { get; set; }
        public decimal Price { get; set; }

        private decimal? DiscountAppliedPrice { get; set; }
    
        public void AppliedDiscount(decimal disCountPrice)
        {
            DiscountAppliedPrice = disCountPrice;
        }

        public decimal GetCurrentPrice
        {
            get => DiscountAppliedPrice != null ? DiscountAppliedPrice.Value : Price;  
        }
    }
}
