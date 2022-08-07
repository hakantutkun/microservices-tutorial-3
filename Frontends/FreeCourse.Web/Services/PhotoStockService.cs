using FreeCourse.Web.Models.Catalogs.PhotoStocks;
using FreeCourse.Web.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace FreeCourse.Web.Services
{
    public class PhotoStockService : IPhotoStockService
    {


        public Task<bool> DeletePhoto(string photoUrl)
        {
        }

        public Task<PhotoViewModel> UploadPhoto(IFormFile photo)
        {
        }
    }
}
