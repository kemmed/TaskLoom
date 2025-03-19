using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace diplom.Views.Users
{
    public class PasswordRecoveryMessageModel : PageModel
    {
        private readonly ILogger<PasswordRecoveryMessageModel> _logger;

        public PasswordRecoveryMessageModel(ILogger<PasswordRecoveryMessageModel> logger)
        {
            _logger = logger;
        }

        public void OnGet()
        {

        }
    }
}
