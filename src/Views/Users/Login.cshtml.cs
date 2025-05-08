using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace taskloom.Views.Users
{
    public class AuthorizationModel : PageModel
    {
        private readonly ILogger<AuthorizationModel> _logger;

        public AuthorizationModel(ILogger<AuthorizationModel> logger)
        {
            _logger = logger;
        }

        public void OnGet()
        {

        }
    }
}
