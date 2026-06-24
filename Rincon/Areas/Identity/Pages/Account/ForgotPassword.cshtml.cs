// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Rincon.Models;
namespace Rincon.Areas.Identity.Pages.Account;

public class ForgotPasswordModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IEmailSender _emailSender;

    public ForgotPasswordModel(UserManager<ApplicationUser> userManager, IEmailSender emailSender)
    {
        _userManager = userManager;
        _emailSender = emailSender;
    }

    /// <summary>
    ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
    ///     directly from your code. This API may change or be removed in future releases.
    /// </summary>
    [BindProperty]
    public InputModel Input { get; set; } = default!;

    /// <summary>
    ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
    ///     directly from your code. This API may change or be removed in future releases.
    /// </summary>
    public class InputModel
    {
        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [Required]
        [EmailAddress]
        public string Email { get; set; } = default!;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await Task.CompletedTask;
        TempData["modalTitle"] = "Restablecer contraseña";
        TempData["modalText"] = "Por seguridad, comunicate con tu superior o administrador para que te genere una nueva contraseña.";
        TempData["modalIcon"] = "info";
        TempData["modalConfirmText"] = "Entendido";
        return RedirectToPage("./Login");
    }

    public async Task<IActionResult> OnGetAsync()
    {
        await Task.CompletedTask;
        TempData["modalTitle"] = "Restablecer contraseña";
        TempData["modalText"] = "Por seguridad, comunicate con tu superior o administrador para que te genere una nueva contraseña.";
        TempData["modalIcon"] = "info";
        TempData["modalConfirmText"] = "Entendido";
        return RedirectToPage("./Login");
    }
}
