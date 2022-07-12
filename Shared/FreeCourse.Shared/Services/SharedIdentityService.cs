using Microsoft.AspNetCore.Http;
using System.Linq;

namespace FreeCourse.Shared.Services
{
    public class SharedIdentityService : ISharedIdentityService
    {
        private IHttpContextAccessor _httpContextAccessor;

        public string GetUserId
        {
            get { return _httpContextAccessor.HttpContext.User.FindFirst("sub").Value; }
        }

        public SharedIdentityService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor=httpContextAccessor;
        }

    }
}
