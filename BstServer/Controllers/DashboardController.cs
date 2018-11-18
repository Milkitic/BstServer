using BstServer.Areas.Admin.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BstServer.Controllers
{
    [Area("admin")]
    [Route("admin/dashboard")]
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly TerminalAppHost _terminalAppHost;

        public DashboardController(TerminalAppHost terminalAppHost)
        {
            _terminalAppHost = terminalAppHost;
        }

        [Route("")]
        public ActionResult Index()
        {
            return View();
        }

        [Route("~/api/get_chart")]
        [HttpPost]
        public ActionResult GetAllCharts()
        {
            lock (_terminalAppHost.LockObj)
            {
                List<TerminalAppHost.SysInfo> dataList = _terminalAppHost.SysInfos
                    /*.Where(k =>k.UnixTime > DateTimeOffset.Now.AddMinutes(-2).ToUnixTimeMilliseconds()).ToList()*/;
                long maxTime = dataList.Max(k => k.UnixTime);
                return Json(new JsonModelBase(200, "success", "success", new { dataList, maxTime }));
            }
        }

        [Route("~/api/append_chart")]
        [HttpPost]
        public ActionResult GetChartByTime(long unixTime)
        {
            lock (_terminalAppHost.LockObj)
            {
                var newList = _terminalAppHost.SysInfos.Where(k => k.UnixTime > unixTime).ToList();
                long maxTime = newList.Count == 0 ? unixTime : newList.Max(k => k.UnixTime);
                return Json(new JsonModelBase(200, "success", "success", new { newList, maxTime }));
            }
        }
    }
}
