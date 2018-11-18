using BstServer.Areas.Admin.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Milkitic.ApplicationHost;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BstServer.Controllers
{
    [Area("admin")]
    [Route("admin/console")]
    [Authorize]
    public class ConsoleController : Controller
    {
        private readonly L4D2AppHost _l4D2AppHost;

        public ConsoleController(L4D2AppHost l4D2AppHost)
        {
            _l4D2AppHost = l4D2AppHost;
        }

        [Route("")]
        public IActionResult Index()
        {
            ConsoleViewModel viewModel = new ConsoleViewModel(_l4D2AppHost);
            return View(viewModel);
        }

        [Route("~/api/start_proc")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RunProcess(ConsoleViewModel viewModel)
        {
            _l4D2AppHost.HostSettings.RedirectStandardInput = viewModel.HostSettings.RedirectStandardInput;
            _l4D2AppHost.HostSettings.ShowWindow = viewModel.HostSettings.ShowWindow;
            _l4D2AppHost.Run();
            return RedirectToAction("Index");
        }

        [Route("~/api/send_command")]
        [HttpPost]
        public IActionResult SendCommand(string command)
        {
            try
            {
                _l4D2AppHost.SendMessage(command);
                return Json(new JsonModelBase(200, "success", "success", null));

            }
            catch (Exception e)
            {
                return Json(new JsonModelBase(500, "failed", e.Message, null));
            }
        }

        [Route("~/api/console_opt")]
        [HttpGet]
        public async Task<IActionResult> ConsoleOperate(int type)
        {
            switch (type)
            {
                case 0:
                    await _l4D2AppHost.StopAsync();
                    _l4D2AppHost.Run();
                    break;
                default:
                    await _l4D2AppHost.StopAsync();
                    break;
            }

            return RedirectToAction("Index");
        }
    }
}