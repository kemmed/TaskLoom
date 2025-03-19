using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace diplom.Views.Users
{
    public class PasswordRecoveryModel : PageModel
    {
        private readonly ILogger<PasswordRecoveryModel> _logger;

        public PasswordRecoveryModel(ILogger<PasswordRecoveryModel> logger)
        {
            _logger = logger;
        }

        public void OnGet()
        {

        }
    }
}
