using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using LPT.CLS.Model;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using CLSDashBoard.ViewModels;
using Newtonsoft.Json;

namespace CLSDashBoard.Controllers
{
    public class HomeController : Controller
    {
        SlaveContext _slaveContext;

        public HomeController(SlaveContext slaveContext, IHostingEnvironment env)
        {
            _slaveContext = slaveContext;
        }
        [HttpGet]
        public IActionResult Login()
        {
            string AreaName = HttpContext.Session.GetString("AreaName");
            if (AreaName != null)
            {
                return RedirectToAction("Index");
            }
            List<Area> ListAreas = _slaveContext.Areas.ToList();

            //获取到区域名称装进List集合
            List<string> AreaNames = new List<string>();
            foreach (var area in ListAreas)
            {
                AreaNames.Add(area.Name);
            }

            ViewBag.AreaNames = AreaNames;
            return View();
        }
        [HttpPost]
        public IActionResult Login(string area, string password)
        {
            if (password == null)
            {
                TempData["tips"] = "请输入密码";
            } else if (area.Equals("null"))
            {
                TempData["tips"] = "请选择区域";
            }
            else if (password.Equals("123456")) //这里与数据库密码核对，暂时不连数据库
            {
                HttpContext.Session.SetString("AreaName", area);
                TempData["AreaName"] = area;
                return RedirectToAction("Index");
            }
            else
            {
                TempData["tips"] = "密码错误";
            }
            return RedirectToAction("Login");
        }
        public IActionResult Index()
        {
            string AreaName = HttpContext.Session.GetString("AreaName");
            if (AreaName == null)
            {
                TempData["tips"] = "您还没有登录，请先登录";
                return RedirectToAction("Login");
            } else
            {
                List<AreaSortingRecords> IndexViews = GetIndexViews(AreaName);
                
                //序列化IndexViews并存到Session，存储这个区域下的所有分拣信息，供GetTable函数使用
                HttpContext.Session.SetString("IndexViews", JsonConvert.SerializeObject(IndexViews));
                ViewBag.AreaName = AreaName;
                ViewBag.ItemCount = IndexViews.Count();
                return View();
            }
        }
        [HttpPost]
        public IActionResult GetTable(int page, int full)
        {
            //反序列化IndexViews取Session
            var value = HttpContext.Session.GetString("IndexViews");
            List<AreaSortingRecords> IndexViews = (value==null?default(List<AreaSortingRecords>):JsonConvert.DeserializeObject<List<AreaSortingRecords>>(value));

            AreaSortingRecords[] IndexViewsArray = IndexViews.ToArray();
            int k = 0;
            int TEST = IndexViewsArray.Length;
            int End;
            if (full == 1)
            {
                End = (page * 10) + 10;
            }else
            {
                End = IndexViewsArray.Length;
            }
            AreaSortingRecords[] tempViewArray = new AreaSortingRecords[End - page*10];
            for (int i = page * 10; i < End; i++)
            {
                tempViewArray[k] = IndexViewsArray[i];
                k++;
             }
            return Json(tempViewArray.ToList());
        }
        [HttpPost]
        public IActionResult GetChartData(string AreaName)
        {
            //反序列化SortingRecords取Session
            var value = HttpContext.Session.GetString("SortingRecords");
            List<SortingRecord> AreaSortingRecords = (value == null ? default(List<SortingRecord>) : JsonConvert.DeserializeObject<List<SortingRecord>>(value));
            //根据区域下的分拣记录分组
            var GroupByName = AreaSortingRecords.GroupBy(m => m.GarbageTypeName);
            int kindNum = GroupByName.Count();
            ChartModel[] chartModels = new ChartModel[kindNum];
            for(int i=0; i<chartModels.Length; i++)
            {
                chartModels[i] = new ChartModel();
            }
            int k = 0;
            foreach (var item in GroupByName)
            {
                int flag = 1;
                foreach(var tinyItem in item)
                {
                    if(flag == 1)
                    {
                        chartModels[k].Name = tinyItem.GarbageTypeName;
                        flag = 0;
                    }
                    if (tinyItem.Weight.HasValue)
                    {
                        chartModels[k].Value = chartModels[k].Value + (int)tinyItem.Weight;
                    }
                }
                int temp = chartModels[k].Value;
                k++;
            }
            return Json(chartModels.ToList());
        }
        [HttpPost]
        public IActionResult GetChartData2(string AreaName)
        {
            //反序列化SortingRecords取Session
            var value = HttpContext.Session.GetString("SortingRecords");
            List<SortingRecord> AreaSortingRecords = (value == null ? default(List<SortingRecord>) : JsonConvert.DeserializeObject<List<SortingRecord>>(value));
            var GroupByUser = AreaSortingRecords.GroupBy(m => m.UserID);
            ChartModel[] chartModels = new ChartModel[GroupByUser.Count()];
            for(int i=0; i<chartModels.Length; i++)
            {
                chartModels[i] = new ChartModel();
            }
            int k = 0;
            foreach(var item in GroupByUser)
            {
                foreach(var tinyItem in item)
                {
                    chartModels[k].Name  = _slaveContext.Users.Single(m => m.ID == tinyItem.UserID).UserNo + "";
                    break;
                }
                chartModels[k].Value = item.Count();
                k++;
            }
            return Json(chartModels.OrderByDescending(m => m.Value).Take(5).ToList());
        }
        public IActionResult Error()
        {
            return View();
        }
        public List<AreaSortingRecords> GetIndexViews(string AreaName)
        {
            List<SortingRecord> AreaSortingRecords = new List<SortingRecord>();
            List<AreaSortingRecords> IndexViews = new List<AreaSortingRecords>();
            //根据区域名称查询区域ID
            Guid AreaID = _slaveContext.Areas.Single(m => m.Name == AreaName).ID;
            //查询该区域下所有用户
            List<User> AreaUsers = _slaveContext.Users.Where(m => m.AreaID == AreaID).ToList();
            List<Guid> UserIDs = new List<Guid>();
            foreach (var user in AreaUsers)
            {
                UserIDs.Add(user.ID);
            }
            AreaSortingRecords = new List<SortingRecord>();
            AreaSortingRecords = _slaveContext.SortingRecords.Where(m => UserIDs.Contains(m.UserID)).OrderByDescending(m => m.Time).ToList();
            foreach (var item in AreaSortingRecords)
            {
                int userNo = _slaveContext.Users.Single(m => m.ID == item.UserID).UserNo;
                DateTime time = (DateTime)item.Time;
                string garbageTypeName = item.GarbageTypeName;
                float weight = item.Weight == null ? 0F : (float)item.Weight; //有的Weight为NULL
                int bp = item.BP;
                AreaSortingRecords indexView = new AreaSortingRecords()
                {
                    UserNo = userNo,
                    Time = time,
                    GarbageTypeName = garbageTypeName,
                    Weight = weight,
                    BP = bp
                };
                IndexViews.Add(indexView);
            }
            //序列化AreaSortingRecords并存到Session，存储分拣信息，供GetChartData()函数使用
            HttpContext.Session.SetString("SortingRecords", JsonConvert.SerializeObject(AreaSortingRecords));
            return IndexViews;
        }
    }

    public class ChartModel
    {
        public int Value;
        public string Name;
    }
}
