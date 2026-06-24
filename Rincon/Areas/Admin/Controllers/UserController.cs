using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Rincon.DataAccess.Data;
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
        private readonly ApplicationDbContext _db;

        public UsersController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext db)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _db = db;
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
            SetPasswordProtection(vm, user, userRoles);

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
                    AddIdentityErrors(result);

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

                var oldRoles = await _userManager.GetRolesAsync(user);
                SetPasswordProtection(vm, user, oldRoles);

                if (!vm.CanChangePassword && !string.IsNullOrWhiteSpace(vm.NewPassword))
                {
                    ModelState.AddModelError("NewPassword", vm.PasswordProtectionMessage ?? "No podés cambiar la contraseña de este usuario.");
                    return View(vm);
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
                    AddIdentityErrors(result);

                    return View(vm);
                }

                await _userManager.RemoveFromRolesAsync(user, oldRoles);
                await _userManager.AddToRoleAsync(user, vm.Role);

                if (!string.IsNullOrWhiteSpace(vm.NewPassword))
                {
                    var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                    var passwordResult = await _userManager.ResetPasswordAsync(user, token, vm.NewPassword);

                    if (!passwordResult.Succeeded)
                    {
                        AddIdentityErrors(passwordResult);

                        return View(vm);
                    }
                }

                TempData["success"] = "Usuario actualizado correctamente";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpGet]
        public IActionResult GetAll()
        {
            var draw = GetDataTablesInt("draw");
            var start = GetDataTablesInt("start");
            var length = GetDataTablesInt("length", 5);
            var searchValue = Request.Query["search[value]"].ToString()?.Trim();
            var orderColumn = GetDataTablesInt("order[0][column]");
            var orderDirection = Request.Query["order[0][dir]"].ToString();
            var currentUserId = _userManager.GetUserId(User);

            var query =
                from user in _db.Users.AsNoTracking()
                join userRole in _db.UserRoles.AsNoTracking()
                    on user.Id equals userRole.UserId into userRoles
                from userRole in userRoles.DefaultIfEmpty()
                join role in _db.Roles.AsNoTracking()
                    on userRole.RoleId equals role.Id into roles
                from role in roles.DefaultIfEmpty()
                select new
                {
                    id = user.Id,
                    fullName = user.FullName,
                    email = user.Email,
                    dni = user.DNI,
                    phoneNumber = user.PhoneNumber,
                    role = role != null && role.Name != null ? role.Name : "Sin rol",
                    isActive = user.IsActive,
                    canToggleStatus = user.Id != currentUserId
                        && (role == null || role.Name != SD.Role_Admin),
                    statusProtectionReason = user.Id == currentUserId
                        ? "No podés bloquear tu propio usuario"
                        : role != null && role.Name == SD.Role_Admin
                            ? "Los administradores no se bloquean desde el sistema"
                            : string.Empty
                };

            var recordsTotal = query.Count();

            if (!string.IsNullOrWhiteSpace(searchValue))
            {
                query = query.Where(u =>
                    u.fullName.Contains(searchValue) ||
                    (u.email != null && u.email.Contains(searchValue)) ||
                    u.dni.Contains(searchValue) ||
                    (u.phoneNumber != null && u.phoneNumber.Contains(searchValue)) ||
                    u.role.Contains(searchValue));
            }

            var recordsFiltered = query.Count();

            query = orderColumn switch
            {
                0 => orderDirection == "asc" ? query.OrderBy(u => u.fullName) : query.OrderByDescending(u => u.fullName),
                1 => orderDirection == "asc" ? query.OrderBy(u => u.email) : query.OrderByDescending(u => u.email),
                2 => orderDirection == "asc" ? query.OrderBy(u => u.dni) : query.OrderByDescending(u => u.dni),
                3 => orderDirection == "asc" ? query.OrderBy(u => u.phoneNumber) : query.OrderByDescending(u => u.phoneNumber),
                4 => orderDirection == "asc" ? query.OrderBy(u => u.role) : query.OrderByDescending(u => u.role),
                5 => orderDirection == "asc" ? query.OrderBy(u => u.isActive) : query.OrderByDescending(u => u.isActive),
                _ => query.OrderBy(u => u.fullName)
            };

            var users = query
                .Skip(start)
                .Take(length)
                .ToList();

            return Json(new
            {
                draw,
                recordsTotal,
                recordsFiltered,
                data = users
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(string id)
        {
            var user = await _userManager.FindByIdAsync(id);

            if (user == null)
            {
                return Json(new { success = false, message = "Usuario no encontrado" });
            }

            if (user.Id == _userManager.GetUserId(User))
            {
                return Json(new
                {
                    success = false,
                    message = "No podés bloquear tu propio usuario administrador."
                });
            }

            if (await _userManager.IsInRoleAsync(user, SD.Role_Admin))
            {
                return Json(new
                {
                    success = false,
                    message = "No se puede bloquear a otro administrador desde el sistema. Para dar de baja un administrador, comunicate con el dueño del sistema."
                });
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

        private void SetPasswordProtection(UserVM vm, ApplicationUser user, IEnumerable<string> userRoles)
        {
            var currentUserId = _userManager.GetUserId(User);
            var isAnotherAdmin = user.Id != currentUserId && userRoles.Contains(SD.Role_Admin);

            vm.CanChangePassword = !isAnotherAdmin;
            vm.PasswordProtectionMessage = isAnotherAdmin
                ? "No podés cambiar la contraseña de otro administrador. Si un administrador debe darse de baja o recuperar acceso, comunicate con el dueño del sistema."
                : null;
        }

        private void AddIdentityErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", GetIdentityErrorMessage(error));
            }
        }

        private static string GetIdentityErrorMessage(IdentityError error)
        {
            return error.Code switch
            {
                nameof(IdentityErrorDescriber.PasswordRequiresNonAlphanumeric) => "La contraseña debe tener al menos un carácter especial, por ejemplo !, @ o #.",
                nameof(IdentityErrorDescriber.PasswordRequiresLower) => "La contraseña debe tener al menos una letra minúscula.",
                nameof(IdentityErrorDescriber.PasswordRequiresUpper) => "La contraseña debe tener al menos una letra mayúscula.",
                nameof(IdentityErrorDescriber.PasswordRequiresDigit) => "La contraseña debe tener al menos un número.",
                nameof(IdentityErrorDescriber.PasswordTooShort) => "La contraseña es demasiado corta.",
                nameof(IdentityErrorDescriber.PasswordRequiresUniqueChars) => "La contraseña debe tener más caracteres diferentes.",
                nameof(IdentityErrorDescriber.DuplicateUserName) => "Ya existe un usuario con ese email.",
                nameof(IdentityErrorDescriber.DuplicateEmail) => "Ya existe un usuario con ese email.",
                nameof(IdentityErrorDescriber.InvalidEmail) => "El email ingresado no es válido.",
                nameof(IdentityErrorDescriber.InvalidUserName) => "El email ingresado no es válido como nombre de usuario.",
                _ => error.Description
            };
        }

        private int GetDataTablesInt(string key, int defaultValue = 0)
        {
            return int.TryParse(Request.Query[key], out var value) ? value : defaultValue;
        }
    }
    
}
