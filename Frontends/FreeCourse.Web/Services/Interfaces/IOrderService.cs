using FreeCourse.Web.Models.Orders;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FreeCourse.Web.Services.Interfaces
{
    public interface IOrderService
    {

        /// <summary>
        /// Synch
        /// Direk order mikroservisine istek yapilacak.
        /// </summary>
        /// <param name="checkoutInfoInput"></param>
        /// <returns></returns>
        Task<OrderCreatedViewModel> CreateOrder(CheckoutInfoInput checkoutInfoInput);

        /// <summary>
        /// Async
        /// Siparis bilgileri rabbitmq'ya gonderilecek...
        /// </summary>
        /// <param name="checkoutInfoInput"></param>
        /// <returns></returns>
        Task SuspendOrder(CheckoutInfoInput checkoutInfoInput);

        Task<List<OrderViewModel>> GetOrder();
    }
}
