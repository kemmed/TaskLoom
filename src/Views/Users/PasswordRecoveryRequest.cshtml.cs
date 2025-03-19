using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace diplom.Views.Users
{
    public class PasswordRecoveryRequestModel : PageModel
    {
        private readonly ILogger<PasswordRecoveryRequestModel> _logger;

        public PasswordRecoveryRequestModel(ILogger<PasswordRecoveryRequestModel> logger)
        {
            _logger = logger;
        }

        public void OnGet()
        {

        }
    }
}
