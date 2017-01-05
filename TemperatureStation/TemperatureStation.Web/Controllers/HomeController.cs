﻿using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TemperatureStation.Web.Data;
using TemperatureStation.Web.Extensions;
using TemperatureStation.Web.Models;
using System.Linq;
using System.Reflection;

namespace TemperatureStation.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _dataContext;
        private readonly ICalculatorProvider _calcProvider;

        public HomeController(ApplicationDbContext dataContext, ICalculatorProvider calcProvider)
        {
            _dataContext = dataContext;
            _calcProvider = calcProvider;
        }

        public async Task<IActionResult> Index()
        {
            _dataContext.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
            
            var model = new HomeViewModel();
            model.Measurement = await _dataContext.Measurements
                                                  .Include(m => m.SensorRoles)
                                                  .Include(m => m.Calculators)
                                                  .SingleOrDefaultAsync(m => m.IsActive);
            if(model.Measurement == null)
            {
                return View("IndexEmpty");
            }

            //var readings1 = await _dataContext.Readings
            //                            .Include(sr => sr.Measurement)
            //                            .Include(r => ((SensorReading)r).SensorRole)
            //                            .Where(r => r.Measurement.Id == model.Measurement.Id)
            //                            .OrderByDescending(r => r.ReadingTime)                                        
            //                            .Take(48)                                        
            //                            .ToListAsync();

            //var readings2 = await _dataContext.Readings
            //                            .Include(sr => sr.Measurement)
            //                            .Include(r => ((CalculatorReading)r).Calculator)
            //                            .Where(r => r.Measurement.Id == model.Measurement.Id)
            //                            .OrderByDescending(r => r.ReadingTime)
            //                            .Take(48)
            //                            .ToListAsync();

            //readings1.AddRange(readings2);
            //var readings = Mapper.Map(readings1, new List<ReadingViewModel>());
            //model.Readings = readings.OrderBy(r => r.ReadingTime)
            //                         .GroupBy(r => r.ReadingTime);

            model.Readings = _dataContext.GetReadings(model.Measurement.Id, null, 10);
            var showOnChart = _calcProvider.GetTypes()
                                            .Where(t => t.GetTypeInfo().GetCustomAttribute<CalculatorAttribute>() != null)
                                            .Where(t => t.GetTypeInfo().GetCustomAttribute<CalculatorAttribute>().ShowOnChart)
                                            .Select(t => t.GetTypeInfo().GetCustomAttribute<CalculatorAttribute>().Name)
                                            .ToList();
            showOnChart.AddRange(model.Measurement.SensorRoles.Select(r => r.RoleName));
            model.CalculatorsOnChart = showOnChart.ToArray();
            return View(model); 
        }

        public IActionResult Error()
        {
            return View();
        }
    }
}