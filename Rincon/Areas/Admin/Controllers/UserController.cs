using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Rincon.Models;
using Rincon.Models.ViewModels;
using Rincon.Utilities;


namespace Rincon.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class UsersController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public UsersController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Upsert(string? id)
        {
            var roles = GetRoleList();

            var vm = new UserVM
            {
                RoleList = roles
            };

            if (string.IsNullOrEmpty(id))
            {
                return View(vm);
            }

            var user = _userManager.Users.FirstOrDefault(u => u.Id == id);

            if (user == null)
            {
                return NotFound();
            }

            var userRoles = _userManager.GetRolesAsync(user).GetAwaiter().GetResult();

            vm.Id = user.Id;
            vm.FullName = user.FullName;
            vm.Email = user.Email;
            vm.DNI = user.DNI;
            vm.PhoneNumber = user.PhoneNumber;
            vm.Address = user.Address;
            vm.Role = userRoles.FirstOrDefault() ?? SD.Role_Employee;
            vm.RoleList = roles;

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upsert(UserVM vm)
        {
            vm.RoleList = GetRoleList();

            if (!ModelState.IsValid)
            {
                return View(vm);
            }

            if (string.IsNullOrEmpty(vm.Id))
            {
                var user = new ApplicationUser
                {
                    UserName = vm.Email,
                    Email = vm.Email,
                    FullName = vm.FullName,
                    DNI = vm.DNI,
                    PhoneNumber = vm.PhoneNumber,
                    Address = vm.Address,
                    Date = DateTime.Now,
                    IsActive = true,
                    EmailConfirmed = true
                };

                string defaultPassword = $"{vm.DNI}Aa!";

                var result = await _userManager.CreateAsync(user, defaultPassword);

                if (!result.Succeeded)
                {
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError("", error.Description);
                    }

                    return View(vm);
                }

                await _userManager.AddToRoleAsync(user, vm.Role);

                TempData["success"] = $"Usuario creado correctamente. Contraseña inicial: {defaultPassword}";
                return RedirectToAction(nameof(Index));
            }
            else
            {
                var user = await _userManager.FindByIdAsync(vm.Id);

                if (user == null)
                {
                    return NotFound();
                }

                user.UserName = vm.Email;
                user.Email = vm.Email;
                user.FullName = vm.FullName;
                user.DNI = vm.DNI;
                user.PhoneNumber = vm.PhoneNumber;
                user.Address = vm.Address;

                var result = await _userManager.UpdateAsync(user);

                if (!result.Succeeded)
                {
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError("", error.Description);
                    }

                    return View(vm);
                }

                var oldRoles = await _userManager.GetRolesAsync(user);
                await _userManager.RemoveFromRolesAsync(user, oldRoles);
                await _userManager.AddToRoleAsync(user, vm.Role);

                TempData["success"] = "Usuario actualizado correctamente";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpGet]
        public IActionResult GetAll()
        {
            var users = _userManager.Users
                .Select(u => new
                {
                    id = u.Id,
                    fullName = u.FullName,
                    email = u.Email,
                    dni = u.DNI,
                    phoneNumber = u.PhoneNumber,
                    isActive = u.IsActive
                })
                .ToList();

            var result = new List<object>();

            foreach (var user in users)
            {
                var appUser = _userManager.Users.FirstOrDefault(u => u.Id == user.id);
                var roles = appUser != null
                    ? _userManager.GetRolesAsync(appUser).GetAwaiter().GetResult()
                    : new List<string>();

                result.Add(new
                {
                    user.id,
                    user.fullName,
                    user.email,
                    user.dni,
                    user.phoneNumber,
                    role = roles.FirstOrDefault() ?? "Sin rol",
                    user.isActive
                });
            }

            return Json(new { data = result });
        }

        [HttpPost]
        public async Task<IActionResult> ToggleStatus(string id)
        {
            var user = await _userManager.FindByIdAsync(id);

            if (user == null)
            {
                return Json(new { success = false, message = "Usuario no encontrado" });
            }

            user.IsActive = !user.IsActive;

            if (user.IsActive)
            {
                user.LockoutEnd = null;
            }
            else
            {
                user.LockoutEnd = DateTimeOffset.MaxValue;
            }

            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                return Json(new { success = false, message = "No se pudo actualizar el estado del usuario" });
            }

            return Json(new
            {
                success = true,
                message = user.IsActive ? "Usuario activado correctamente" : "Usuario bloqueado correctamente"
            });
        }

        private IEnumerable<SelectListItem> GetRoleList()
        {
            return _roleManager.Roles
                .Select(r => r.Name)
                .AsEnumerable()
                .Where(roleName => !string.IsNullOrWhiteSpace(roleName))
                .Select(roleName => new SelectListItem
                {
                    Text = GetRoleDisplayName(roleName),
                    Value = roleName
                })
                .ToList();
        }

        private string GetRoleDisplayName(string? roleName)
        {
            return roleName switch
            {
                SD.Role_Admin => "Administrador",
                SD.Role_Employee => "Empleado",
                _ => roleName ?? "Sin rol"
            };
        }
    }
    
}
