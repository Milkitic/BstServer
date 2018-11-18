using BstServer.Areas.Admin.Models;
using BstServer.Models;
using Microsoft.AspNetCore.Mvc;
using Milkitic.ApplicationHost;
using Milkitic.FileExplorer;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace BstServer.Controllers
{
    [Route("front")]
    public class HomeController : Controller
    {
        private readonly L4D2AppHost _l4D2AppHost;

        public HomeController(L4D2AppHost l4D2AppHost)
        {
            _l4D2AppHost = l4D2AppHost;
        }

        [Route("~/")]
        [Route("")]
        public IActionResult Index()
        {
            return View();
        }

        [Route("statistics")]
        public IActionResult Statistics()
        {
            return View();
        }


        private static (string name, string pic) GetDisplayInfo(string source)
        {
            string picName = "";
            string dispName = source;
            switch (source)
            {
                case "Magnum":
                    picName = "weapon_magnum";
                    break;
                case "Grenade Launcher":
                    picName = "weapon_grenadelauncher";
                    break;
                case "Chrome Shotgun":
                    picName = "weapon_shotgun_chrome";
                    break;
                case "M60":
                    picName = "weapon_rifle_m60";
                    break;
                case "Pistol":
                    picName = "weapon_pistols";
                    break;
                case "AK-47":
                    picName = "weapon_ak47";
                    break;
                case "Fire Axe":
                    picName = "weapon_melee_axe";
                    break;
                case "XM1014":
                    picName = "weapon_shotgun_auto";
                    dispName = "Tactical Shotgun";
                    break;
                case "Mac-10":
                    picName = "weapon_silencedsmg";
                    dispName = "Silenced SMG";
                    break;
                case "SPAS-12":
                    picName = "weapon_shotgun_tactical";
                    dispName = "Combat Shotgun";
                    break;
                case "M16A1":
                    picName = "weapon_m16";
                    dispName = "Assault Rifle";
                    break;
                case "Minigun":
                    picName = "weapon_50cal";
                    break;
                case "Pump Shotgun":
                    picName = "weapon_shotgun_pump";
                    break;
                case "Uzi":
                    picName = "weapon_smg";
                    break;
                case "Katana":
                    picName = "weapon_melee_sword";
                    break;
                case "Chainsaw":
                    picName = "weapon_melee_chainsaw";
                    break;
                case "Cricket Bat":
                    picName = "weapon_melee_cricketbat";
                    break;
            }

            return (dispName, picName);
        }

        [Route("privacy")]
        public IActionResult Privacy()
        {
            return View();
        }

        [HttpPost]
        [Route("/api/get_statistics")]
        public IActionResult GetStatistics(int y, int m, int d, bool dmg = true)
        {
            List<SteamUser> users = _l4D2AppHost.UserConfig.SteamUsers.ToList();
            List<WeaponInfo> dic = new List<WeaponInfo>();
            foreach (var u in users)
            {
                var info = u.GetDayInfo(y, m, d)?.WeaponInfos;
                if (info == null) continue;
                foreach (var cell in info)
                {
                    var dispInfo = GetDisplayInfo(cell.WeaponName);
                    var weapon = dispInfo.name;
                    var pic = dispInfo.pic;
                    int times = cell.DamageTimes;
                    var damage = cell.Damage;
                    var username = u.CurrentName;
                    if (damage == 0 && times == 0) continue;
                    if (dic.All(k => k.Weapon != weapon))
                    {
                        dic.Add(new WeaponInfo(weapon, pic));
                    }

                    var sb = dic.First(k => k.Weapon == weapon);
                    sb.AddDetail(new WeaponDetail(username, damage, times));
                }
            }

            var max = dic.Count == 0 ? 1 : dic.Max(k => k.Detail.Count);
            foreach (var weaponInfo in dic)
            {
                weaponInfo.Detail.Sort(new FfComparer());
            }

            List<WeaponInfo> dicSort =
                dic.OrderByDescending(objDic => objDic.Detail.Sum(k => k.Damage)).ToList();
            return Json(new JsonModelBase(200, "success", "success", new { column = max, dataList = dicSort }));
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        [Route("~/error")]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
