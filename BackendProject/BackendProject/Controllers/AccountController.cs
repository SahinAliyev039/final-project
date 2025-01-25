﻿using BackendProject.Utils;
using BackendProject.Utils.Enums;
using Microsoft.AspNetCore.Identity;

namespace BackendProject.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public AccountController(UserManager<AppUser> userManager, SignInManager<AppUser> signInManager,
            RoleManager<IdentityRole> roleManager, IWebHostEnvironment webHostEnvironment)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _webHostEnvironment = webHostEnvironment;

        }

        public IActionResult Register()
        {
            if (User.Identity.IsAuthenticated)
                return BadRequest();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel registerViewModel)
        {
            if (User.Identity.IsAuthenticated)
                return BadRequest();

            if (!ModelState.IsValid)
                return View();

            AppUser newUser = new()
            {
                Fullname = registerViewModel.Fullname,
                Email = registerViewModel.Email,
                UserName = registerViewModel.Username,
                IsActive = true,
            };
            var identityResult = await _userManager.CreateAsync(newUser, registerViewModel.Password);
            if (!identityResult.Succeeded)
            {
                foreach (var error in identityResult.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
                return View();
            }
            await _userManager.AddToRoleAsync(newUser, RoleType.Member.ToString());

            return RedirectToAction(nameof(Login));

        }

        public IActionResult Login()
        {
            if (User.Identity.IsAuthenticated)
                return BadRequest();

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel loginViewModel)
        {
            if (User.Identity.IsAuthenticated)
                return BadRequest();

            if (!ModelState.IsValid)
                return View();

            var user = await _userManager.FindByNameAsync(loginViewModel.Username);
            if (user is null)
            {
                ModelState.AddModelError("", "Username or password invalid");
                return View();
            }

            var signInResult = await _signInManager.PasswordSignInAsync(user, loginViewModel.Password, loginViewModel.RememberMe, true);
            if (signInResult.IsLockedOut)
            {
                ModelState.AddModelError("", "Your account is blocked temporary");
                return View();
            }
            if (!signInResult.Succeeded)
            {
                ModelState.AddModelError("", "Username or password invalid");
                return View();
            }

            return RedirectToAction("Index", "Home");
        }

        public async Task<IActionResult> Logout()
        {
            if (!User.Identity.IsAuthenticated)
                return BadRequest();

            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(FogotPasswordViewModel fogotPasswordViewModel)
        {
            if (!ModelState.IsValid)
                return View();

            var user = await _userManager.FindByEmailAsync(fogotPasswordViewModel.Email);
            if (user is null)
            {
                ModelState.AddModelError("Email", $"User not found by email: {fogotPasswordViewModel.Email}");
                return View();
            }

            string token = await _userManager.GeneratePasswordResetTokenAsync(user);

            string? url = Url.Action("ResetPassword", "Account", new { userId = user.Id, token }, HttpContext.Request.Scheme);

            EmailHelper emailHelper = new EmailHelper();

            string body = await GetEmailTemplateAsync(url);

            MailRequestViewModel mailRequestViewModel = new()
            {
                ToEmail = user.Email,
                Subject = "Reset your password",
                Body = body
            };

            await emailHelper.SendEmailAsync(mailRequestViewModel);
            return RedirectToAction(nameof(Login));
        }

        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel resetPasswordViewModel)
        {
            if (string.IsNullOrWhiteSpace(resetPasswordViewModel.UserId) || string.IsNullOrWhiteSpace(resetPasswordViewModel.Token))
                return BadRequest();

            var user = await _userManager.FindByIdAsync(resetPasswordViewModel.UserId);
            if (user is null)
                return NotFound();

            ViewBag.UserName = user.UserName;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ChangePasswordViewModel changePasswordViewModel, string? userId, string? token)
        {
            if (!ModelState.IsValid)
                return View();

            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(token))
                return BadRequest();

            var user = await _userManager.FindByIdAsync(userId);
            if (user is null)
                return NotFound();

            var identityResult = await _userManager.ResetPasswordAsync(user, token, changePasswordViewModel.Password);
            if (!identityResult.Succeeded)
            {
                foreach (var error in identityResult.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
                return View();
            }

            return RedirectToAction(nameof(Login));
        }

        public async Task<IActionResult> CreateRoles()
        {
            foreach (var roleType in Enum.GetValues(typeof(RoleType)))
            {
                if (!await _roleManager.RoleExistsAsync(roleType.ToString()))
                    await _roleManager.CreateAsync(new IdentityRole { Name = roleType.ToString() });
            }

            return Json("Ok");
        }

        private async Task<string> GetEmailTemplateAsync(string url)
        {
            string path = Path.Combine(_webHostEnvironment.WebRootPath, "assets", "Password", "template.html");

            using StreamReader streamReader = new StreamReader(path);

            string result = await streamReader.ReadToEndAsync();

            result = result.Replace("[reset_password_url]", url);
            streamReader.Close();
            return result;
        }
    }
}
