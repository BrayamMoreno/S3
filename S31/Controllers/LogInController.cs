using Amazon.S3;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using S31.Models;
using System.Security.Claims;

namespace S31.Controllers
{
	public class LogInController : Controller
	{
		public IActionResult Index()
		{
			return View();
		}

		public IActionResult LogIn(Usuario model)
		{
			if (ModelState.IsValid)
			{
				var claims = new List<Claim> {
									new Claim("Nombre", model.Nombre ),
									new Claim("Contraseña", model.Contraseña ),
									new Claim("Cubeta", model.Cubeta),
									new Claim(ClaimTypes.Role, "Admin")
				};

				var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

				HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));

				return RedirectToAction("Dashboard", "S31");
			}
			else
			{
				TempData["ErrorCampos"] = "Algunos de los campos estan vacios.";
				return RedirectToAction("Index");
			}
		}

		public IActionResult LogOut(){

			HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

			return RedirectToAction("Index");
		}


	}
}
